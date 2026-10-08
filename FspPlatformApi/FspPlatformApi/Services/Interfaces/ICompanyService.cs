using FspPlatformApi.DTOs.Employers;

namespace FspPlatformApi.Services.Interfaces;

public interface ICompanyService
{
    Task<CompanyDto> GetMyCompanyAsync(Guid ownerUserId);
    Task<CompanyDto> CreateAsync(Guid ownerUserId, CompanyUpsertDto dto);
    Task<CompanyDto> UpdateAsync(Guid ownerUserId, Guid companyId, CompanyUpsertDto dto);
}