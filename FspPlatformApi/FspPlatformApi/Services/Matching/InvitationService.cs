using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Invitations;
using FspPlatformApi.Services.Interfaces;
using FspPlatformApi.Services.Interfaces.Matching;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Matching;

/// <summary>
/// Основная механика платформы (ТЗ критерий 1): инициатива у работодателя —
/// он просматривает категоризированную базу и направляет приглашение конкретному человеку
/// (модель LHH). Привязка к вакансии необязательна, ЗП обязательна и видна кандидату до общения.
/// </summary>
public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public InvitationService(ApplicationDbContext db, INotificationService notify)
        => (_db, _notify) = (db, notify);

    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен")
        ?? throw new InvalidOperationException("Сначала создайте профиль компании.");

    public async Task<InvitationDto> SendAsync(Guid ownerUserId, SendInvitationDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);

        var candidateExists = await _db.CandidateProfiles
            .AnyAsync(p => p.UserId == dto.CandidateId && p.IsProfilePublic);
        if (!candidateExists)
            throw new InvalidOperationException("Кандидат не найден или скрыл профиль.");

        var inv = new Invitation
        {
            CompanyId = company.Id,
            CandidateId = dto.CandidateId,
            VacancyId = dto.VacancyId,
            Title = dto.Title,
            Message = dto.Message,
            SalaryFrom = dto.SalaryFrom,
            SalaryTo = dto.SalaryTo,
            ContactMethod = dto.ContactMethod,
            Status = "отправлено",
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };
        _db.Invitations.Add(inv);
        await _db.SaveChangesAsync();

        await _notify.CreateAsync(dto.CandidateId, "получено_приглашение",
            new
            {
                invitationId = inv.Id,
                company = company.Name,
                title = inv.Title,
                salary = $"{inv.SalaryFrom}–{inv.SalaryTo}"
            });

        return await MapAsync(inv.Id);
    }

    /// <summary>
    /// ВАЖНО: Include(Company) и Include(Candidate) обязательны — без них навигации
    /// будут null внутри Map и запрос упадёт с NullReferenceException,
    /// как только в таблице появится хотя бы одно приглашение.
    ///
    /// Отозванные приглашения из списка исключаем: это завершённое действие самого
    /// работодателя, оно не требует реакции и уже скрыто на стороне кандидата
    /// (симметрия). Строка в БД остаётся — история и аудит целы.
    /// Статус «истекло» остаётся видимым — это сигнал «не дождались ответа».
    /// </summary>
    public async Task<List<InvitationDto>> GetCompanyInvitationsAsync(Guid ownerUserId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        return await _db.Invitations
            .AsNoTracking()
            .Include(i => i.Company)
            .Include(i => i.Candidate)
            .Where(i => i.CompanyId == company.Id && i.Status != "отозвано")
            .OrderByDescending(i => i.SentAt)
            .Select(i => Map(i))
            .ToListAsync();
    }

    public async Task<List<InvitationDto>> GetMyInvitationsAsync(Guid candidateId) =>
        await _db.Invitations
            .AsNoTracking()
            .Include(i => i.Company)
            .Include(i => i.Candidate)
            .Where(i => i.CandidateId == candidateId && i.Status != "отозвано")
            .OrderByDescending(i => i.SentAt)
            .Select(i => Map(i))
            .ToListAsync();

    public async Task MarkViewedAsync(Guid candidateId, Guid invitationId)
    {
        var inv = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.CandidateId == candidateId)
            ?? throw new UnauthorizedAccessException("Приглашение не найдено.");
        if (inv.Status == "отправлено")
        {
            inv.Status = "просмотрено";
            inv.ViewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RespondAsync(Guid candidateId, Guid invitationId, InvitationResponseDto dto)
    {
        // Include(Candidate) обязателен: ФИО нужно для уведомления работодателю.
        var inv = await _db.Invitations
            .Include(i => i.Company)
            .Include(i => i.Candidate)
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.CandidateId == candidateId)
            ?? throw new UnauthorizedAccessException("Приглашение не найдено.");

        if (inv.Status is not ("отправлено" or "просмотрено"))
            throw new InvalidOperationException("Приглашение уже обработано.");

        if (inv.ExpiresAt is not null && inv.ExpiresAt < DateTime.UtcNow)
        {
            inv.Status = "истекло";
            await _db.SaveChangesAsync();
            throw new InvalidOperationException("Срок действия приглашения истёк.");
        }

        inv.Status = dto.Action == "accept" ? "принято" : "отклонено";
        inv.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // ВАЖНО: ключи candidate и title должны совпадать с тем, что читает фронт.
        // Раньше payload был { invitationId, status, candidateId } — работодатель
        // видел голое «Кандидат принял приглашение» без имени и контекста.
        var candidateName = $"{inv.Candidate?.LastName ?? ""} {inv.Candidate?.FirstName ?? ""}".Trim();

        await _notify.CreateAsync(inv.Company.OwnerUserId, "ответ_на_приглашение",
            new
            {
                invitationId = inv.Id,
                status = inv.Status,
                candidateId,
                candidate = string.IsNullOrWhiteSpace(candidateName) ? "Кандидат" : candidateName,
                title = inv.Title
            });
    }

    public async Task RevokeAsync(Guid ownerUserId, Guid invitationId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var inv = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.CompanyId == company.Id)
            ?? throw new UnauthorizedAccessException("Приглашение не найдено.");
        if (inv.Status is "принято" or "отклонено")
            throw new InvalidOperationException("Приглашение уже имеет ответ.");
        inv.Status = "отозвано";
        await _db.SaveChangesAsync();
    }

    private async Task<InvitationDto> MapAsync(Guid id)
    {
        var i = await _db.Invitations
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Candidate)
            .FirstAsync(x => x.Id == id);
        return Map(i);
    }

    /// <summary>
    /// Защита от null: ФИО кандидата и название компании подставляются через ?? —
    /// если профиль кандидата ещё не заполнен (LastName/FirstName = null), сервис
    /// не падает, а показывает заглушку.
    /// </summary>
    private static InvitationDto Map(Invitation i) => new()
    {
        Id = i.Id,
        CandidateId = i.CandidateId,
        CandidateName =
            (i.Candidate?.LastName ?? "") + " " + (i.Candidate?.FirstName ?? ""),
        CategoryName = null,
        VacancyId = i.VacancyId,
        Title = i.Title,
        Message = i.Message,
        SalaryFrom = i.SalaryFrom,
        SalaryTo = i.SalaryTo,
        Currency = i.Currency,
        CompanyName = i.Company?.Name ?? "Компания",
        ContactMethod = i.ContactMethod,
        Status = i.Status,
        SentAt = i.SentAt,
        ViewedAt = i.ViewedAt,
        RespondedAt = i.RespondedAt,
        ExpiresAt = i.ExpiresAt
    };
}