using FspPlatformApi.DTOs.Tests;

namespace FspPlatformApi.Services.Interfaces.Testing;

public interface ITestService
{
    Task<TestSessionDto> StartTestAsync(Guid candidateId, StartTestDto dto);
    Task<TestSessionDto> GetSessionAsync(Guid candidateId, Guid sessionId);
    Task SubmitAnswersAsync(Guid candidateId, Guid sessionId, SubmitAnswersDto dto);
    Task<TestResultDto> FinishTestAsync(Guid candidateId, Guid sessionId);
    Task<List<TestHistoryItemDto>> GetHistoryAsync(Guid candidateId);
}