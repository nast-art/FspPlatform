using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Employers;

public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _db;
    public CompanyService(ApplicationDbContext db) => _db = db;

    public async Task<CompanyDto> GetMyCompanyAsync(Guid ownerUserId)
    {
        var c = await _db.Companies
            .FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId && x.Status == "активен")
            ?? throw new InvalidOperationException("Компания не найдена.");
        return Map(c);
    }

    public async Task<CompanyDto> CreateAsync(Guid ownerUserId, CompanyUpsertDto dto)
    {
        if (await _db.Companies.AnyAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен"))
            throw new InvalidOperationException("Компания уже создана.");
        var c = new Company
        {
            OwnerUserId = ownerUserId, Name = dto.Name, Industry = dto.Industry,
            Description = dto.Description, Website = dto.Website,
            ContactEmail = dto.ContactEmail, ContactPhone = dto.ContactPhone
        };
        _db.Companies.Add(c);
        await _db.SaveChangesAsync();
        return Map(c);
    }

    public async Task<CompanyDto> UpdateAsync(Guid ownerUserId, Guid companyId, CompanyUpsertDto dto)
    {
        var c = await _db.Companies
            .FirstOrDefaultAsync(x => x.Id == companyId && x.OwnerUserId == ownerUserId)
            ?? throw new UnauthorizedAccessException();
        c.Name = dto.Name; c.Industry = dto.Industry; c.Description = dto.Description;
        c.Website = dto.Website; c.ContactEmail = dto.ContactEmail; c.ContactPhone = dto.ContactPhone;
        await _db.SaveChangesAsync();
        return Map(c);
    }

    private static CompanyDto Map(Company c) => new()
    {
        Id = c.Id, Name = c.Name, Industry = c.Industry, Description = c.Description,
        Website = c.Website, ContactEmail = c.ContactEmail, ContactPhone = c.ContactPhone,
        VerificationLevel = c.VerificationLevel
    };
}