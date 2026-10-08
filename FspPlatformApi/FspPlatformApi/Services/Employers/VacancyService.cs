using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.DTOs.Search;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Employers;

public class VacancyService : IVacancyService
{
    private readonly ApplicationDbContext _db;
    public VacancyService(ApplicationDbContext db) => _db = db;

    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен")
        ?? throw new InvalidOperationException("Сначала создайте профиль компании.");

    public async Task<VacancyDto> CreateAsync(Guid ownerUserId, VacancyUpsertDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var v = new Vacancy
        {
            Id = Guid.NewGuid(),  
            CompanyId = company.Id, Title = dto.Title, Description = dto.Description,
            SpecializationId = (short)dto.SpecializationId,
            GradeFromId = dto.GradeFromId, GradeToId = dto.GradeToId,
            SalaryFrom = dto.SalaryFrom, SalaryTo = dto.SalaryTo, // ТЗ 2.2 п.1: ЗП обязательна
            EmploymentType = dto.EmploymentType, Location = dto.Location, Status = "черновик"
        };
        _db.Vacancies.Add(v);
        await SaveSkillsAsync(v.Id, dto.SkillIds);
        await _db.SaveChangesAsync();
        return await MapAsync(v.Id);
    }

    public async Task<VacancyDto> UpdateAsync(Guid ownerUserId, Guid vacancyId, VacancyUpsertDto dto)
    {
        var v = await OwnedVacancyAsync(ownerUserId, vacancyId);
        v.Title = dto.Title; v.Description = dto.Description;
        v.SpecializationId = (short)dto.SpecializationId;
        v.GradeFromId = dto.GradeFromId; v.GradeToId = dto.GradeToId;
        v.SalaryFrom = dto.SalaryFrom; v.SalaryTo = dto.SalaryTo;
        v.EmploymentType = dto.EmploymentType; v.Location = dto.Location;
        await SaveSkillsAsync(v.Id, dto.SkillIds);
        await _db.SaveChangesAsync();
        return await MapAsync(v.Id);
    }

    public async Task PublishAsync(Guid ownerUserId, Guid vacancyId)
    {
        var v = await OwnedVacancyAsync(ownerUserId, vacancyId);
        v.Status = "активен"; v.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task CloseAsync(Guid ownerUserId, Guid vacancyId)
    {
        var v = await OwnedVacancyAsync(ownerUserId, vacancyId);
        v.Status = "в_архиве";
        await _db.SaveChangesAsync();
    }

    public async Task<VacancyDto> GetAsync(Guid vacancyId) => await MapAsync(vacancyId);

    public async Task<List<VacancyDto>> GetPublishedAsync(SearchVacancyQuery query)
    {
        var q = _db.Vacancies.Where(v => v.Status == "активен");
        if (query.SpecializationId is not null) q = q.Where(v => v.SpecializationId == query.SpecializationId);
        if (!string.IsNullOrWhiteSpace(query.Query))
            q = q.Where(v => EF.Functions.ILike(v.Title + " " + v.Description, $"%{query.Query}%"));
        return await q.OrderByDescending(v => v.PublishedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(v => new VacancyDto
            {
                Id = v.Id, Title = v.Title, Description = v.Description, CompanyName = v.Company.Name,
                SpecializationName = v.Specialization.Name,
                GradeRange = v.GradeFrom.Name + " – " + v.GradeTo.Name,
                SalaryFrom = v.SalaryFrom, SalaryTo = v.SalaryTo, Currency = v.Currency,
                EmploymentType = v.EmploymentType, Location = v.Location,
                Status = v.Status, PublishedAt = v.PublishedAt,
                Skills = v.Skills.Select(k => k.Name).ToList()
            }).ToListAsync();
    }

    private async Task<Vacancy> OwnedVacancyAsync(Guid ownerUserId, Guid vacancyId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        return await _db.Vacancies
            .FirstOrDefaultAsync(v => v.Id == vacancyId && v.CompanyId == company.Id)
            ?? throw new InvalidOperationException("Вакансия не найдена или не принадлежит вашей компании.");
    }

    private async Task SaveSkillsAsync(Guid vacancyId, List<short>? skillIds)
    {
        var set = _db.Set<Dictionary<string, object>>("VacancySkill");
        set.RemoveRange(set.Where(s => (Guid)s["VacancyId"] == vacancyId));
        if (skillIds is not null)
            foreach (var id in skillIds.Distinct())
                set.Add(new Dictionary<string, object> { ["VacancyId"] = vacancyId, ["SkillId"] = id });
    }

    private async Task<VacancyDto> MapAsync(Guid id)
    {
        var v = await _db.Vacancies.Include(x => x.Company).Include(x => x.Specialization)
            .Include(x => x.GradeFrom).Include(x => x.GradeTo).Include(x => x.Skills)
            .FirstAsync(x => x.Id == id);
        return new VacancyDto
        {
            Id = v.Id, Title = v.Title, Description = v.Description, CompanyName = v.Company.Name,
            SpecializationName = v.Specialization.Name,
            GradeRange = v.GradeFrom.Name + " – " + v.GradeTo.Name,
            SalaryFrom = v.SalaryFrom, SalaryTo = v.SalaryTo, Currency = v.Currency,
            EmploymentType = v.EmploymentType, Location = v.Location,
            Status = v.Status, PublishedAt = v.PublishedAt,
            Skills = v.Skills.Select(k => k.Name).ToList()
        };
    }
}