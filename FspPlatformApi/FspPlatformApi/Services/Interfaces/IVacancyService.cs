using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.DTOs.Search;

namespace FspPlatformApi.Services.Interfaces;

public interface IVacancyService
{
    Task<VacancyDto> CreateAsync(Guid ownerUserId, VacancyUpsertDto dto);
    Task<VacancyDto> UpdateAsync(Guid ownerUserId, Guid vacancyId, VacancyUpsertDto dto);
    Task PublishAsync(Guid ownerUserId, Guid vacancyId);
    Task CloseAsync(Guid ownerUserId, Guid vacancyId);
    Task<VacancyDto> GetAsync(Guid vacancyId);
    Task<List<VacancyDto>> GetPublishedAsync(SearchVacancyQuery query);
}