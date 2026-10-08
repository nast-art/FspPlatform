using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Employers;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public ApplicationService(ApplicationDbContext db, INotificationService notify)
        => (_db, _notify) = (db, notify);

    /// <summary>Компания текущего работодателя. Без неё — понятная ошибка, а не "Sequence contains no elements".</summary>
    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен")
        ?? throw new InvalidOperationException("Сначала создайте профиль компании.");

    public async Task ApplyAsync(Guid candidateId, Guid vacancyId, ApplyDto dto)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId && v.Status == "активен")
            ?? throw new KeyNotFoundException("Вакансия не найдена или снята с публикации.");

        if (await _db.Applications.AnyAsync(a => a.VacancyId == vacancyId && a.CandidateId == candidateId))
            throw new InvalidOperationException("Вы уже откликались на эту вакансию.");

        // ФИО кандидата — для текста уведомления работодателю.
        // Профиль может быть не заполнен: ?? "" и Trim гарантируют, что не упадём и не шлём "null".
        var candidate = await _db.CandidateProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == candidateId);

        var candidateName = $"{candidate?.LastName ?? ""} {candidate?.FirstName ?? ""}".Trim();

        var a = new Application
        {
            VacancyId = vacancyId,
            CandidateId = candidateId,
            CoverLetter = dto.CoverLetter,
            Status = "отправлен" // ТЗ 2.2 п.3
        };
        _db.Applications.Add(a);
        await _db.SaveChangesAsync();

        // ВАЖНО: ключ payload должен называться "vacancy" — фронт читает p.vacancy.
        await _notify.CreateAsync(vacancy.Company.OwnerUserId, "новый_отклик",
            new
            {
                applicationId = a.Id,
                vacancyId,
                vacancy = vacancy.Title,
                candidate = string.IsNullOrWhiteSpace(candidateName) ? "Кандидат" : candidateName
            });
    }

    public async Task<List<ApplicationDto>> GetMyApplicationsAsync(Guid candidateId) =>
        await _db.Applications
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                VacancyId = a.VacancyId,
                VacancyTitle = a.Vacancy.Title,
                CompanyName = a.Vacancy.Company.Name,
                SalaryFrom = a.Vacancy.SalaryFrom,
                SalaryTo = a.Vacancy.SalaryTo,
                Status = a.Status,
                SubmittedAt = a.SubmittedAt,
                // Список самого кандидата: контакты ему не нужны (они его собственные),
                // но CandidateId/CandidateName заполняем — для симметрии ответа.
                CandidateId = a.CandidateId,
                CandidateName = (a.Candidate.LastName ?? "") + " " + (a.Candidate.FirstName ?? "")
            })
            .ToListAsync();

    /// <summary>
    /// Все отклики по всем вакансиям компании — один запрос вместо N+1 со стороны фронта.
    /// ТЗ 2.2 п.2: отклик сам по себе раскрывает работодателю контакты кандидата —
    /// поэтому Email/Phone/Telegram здесь заполнены.
    /// </summary>
    public async Task<List<ApplicationDto>> GetCompanyApplicationsAsync(Guid ownerUserId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        return await _db.Applications
            .Where(a => a.Vacancy.CompanyId == company.Id)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                VacancyId = a.VacancyId,
                VacancyTitle = a.Vacancy.Title,
                CompanyName = a.Vacancy.Company.Name,
                SalaryFrom = a.Vacancy.SalaryFrom,
                SalaryTo = a.Vacancy.SalaryTo,
                Status = a.Status,
                SubmittedAt = a.SubmittedAt,
                CandidateId = a.CandidateId,
                CandidateName = (a.Candidate.LastName ?? "") + " " + (a.Candidate.FirstName ?? ""),
                CoverLetter = a.CoverLetter,
                Email = a.Candidate.User.Email,
                Phone = a.Candidate.Phone,
                Telegram = a.Candidate.Telegram
            })
            .ToListAsync();
    }

    public async Task<List<ApplicationDto>> GetVacancyApplicationsAsync(Guid ownerUserId, Guid vacancyId)
    {
        var company = await MyCompanyAsync(ownerUserId);

        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId && v.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Вакансия не найдена или не принадлежит вашей компании.");

        return await _db.Applications
            .Where(a => a.VacancyId == vacancyId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                VacancyId = a.VacancyId,
                VacancyTitle = vacancy.Title,
                CompanyName = vacancy.Company.Name,
                SalaryFrom = vacancy.SalaryFrom,
                SalaryTo = vacancy.SalaryTo,
                Status = a.Status,
                SubmittedAt = a.SubmittedAt,
                // см. комментарий к GetCompanyApplicationsAsync — контакты после отклика (ТЗ 2.2 п.2)
                CandidateId = a.CandidateId,
                CandidateName = (a.Candidate.LastName ?? "") + " " + (a.Candidate.FirstName ?? ""),
                CoverLetter = a.CoverLetter,
                Email = a.Candidate.User.Email,
                Phone = a.Candidate.Phone,
                Telegram = a.Candidate.Telegram
            })
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(Guid ownerUserId, Guid applicationId, ApplicationStatusDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);

        var a = await _db.Applications
            .Include(x => x.Vacancy)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.Vacancy.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Отклик не найден или не принадлежит вашей компании.");

        if (a.Status is "принят" or "отклонен" or "отозван")
            throw new InvalidOperationException("Отклик уже закрыт.");

        a.Status = dto.Status;
        await _db.SaveChangesAsync();

        await _notify.CreateAsync(a.CandidateId, "смена_статуса_отклика",
            new
            {
                applicationId = a.Id,
                vacancy = a.Vacancy.Title,
                company = a.Vacancy.Company.Name,
                status = dto.Status
            });
    }

    public async Task WithdrawAsync(Guid candidateId, Guid applicationId)
    {
        var a = await _db.Applications
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("Отклик не найден.");
        a.Status = "отозван";
        await _db.SaveChangesAsync();
    }
}