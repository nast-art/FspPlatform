using FspPlatformApi.DTOs.Employers;

namespace FspPlatformApi.Services.Interfaces;

public interface IApplicationService
{
    Task ApplyAsync(Guid candidateId, Guid vacancyId, ApplyDto dto);
    Task<List<ApplicationDto>> GetMyApplicationsAsync(Guid candidateId);
    Task<List<ApplicationDto>> GetCompanyApplicationsAsync(Guid ownerUserId);
    Task<List<ApplicationDto>> GetVacancyApplicationsAsync(Guid ownerUserId, Guid vacancyId);
    Task UpdateStatusAsync(Guid ownerUserId, Guid applicationId, ApplicationStatusDto dto);
    Task WithdrawAsync(Guid candidateId, Guid applicationId);
}