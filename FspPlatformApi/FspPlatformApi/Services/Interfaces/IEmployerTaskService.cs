using FspPlatformApi.DTOs.Tasks;

namespace FspPlatformApi.Services.Interfaces;

public interface IEmployerTaskService
{
    // ---------- Работодатель ----------
    Task<Guid> CreateTaskAsync(Guid ownerUserId, EmployerTaskUpsertDto dto);
    Task AssignAsync(Guid ownerUserId, Guid taskId, AssignTaskDto dto);
    Task<int> AssignManyAsync(Guid ownerUserId, Guid taskId, AssignTaskBulkDto dto);
    Task ArchiveAsync(Guid ownerUserId, Guid taskId);
    Task RestoreAsync(Guid ownerUserId, Guid taskId);
    Task<List<EmployerTaskListItemDto>> GetCompanyTasksAsync(Guid ownerUserId);
    Task<List<TaskCompletionDto>> GetCompletionsAsync(Guid ownerUserId);
    Task ScoreAsync(Guid ownerUserId, Guid candidateTaskId, TaskScoreDto dto);
    /// <summary>Отзыв назначения до начала работы кандидатом (статус "отменено", не удаление).</summary>
    Task UnassignAsync(Guid ownerUserId, Guid candidateTaskId);

    // ---------- Кандидат ----------
    Task<List<CandidateTaskDto>> GetMyTasksAsync(Guid candidateId);
    Task SubmitAnswerAsync(Guid candidateId, Guid candidateTaskId, TaskAnswerDto dto);
}