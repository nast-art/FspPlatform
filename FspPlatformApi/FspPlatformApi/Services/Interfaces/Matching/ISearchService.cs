using FspPlatformApi.DTOs.Search;

namespace FspPlatformApi.Services.Interfaces.Matching;

public interface ISearchService
{
    Task<SearchResponseDto> SearchAsync(Guid ownerUserId, SearchCandidatesDto dto);
    Task<List<SearchResponseDto>> GetHistoryAsync(Guid ownerUserId, int page, int pageSize);
    Task<SearchResponseDto?> GetSavedResultAsync(Guid ownerUserId, Guid requestId);
}