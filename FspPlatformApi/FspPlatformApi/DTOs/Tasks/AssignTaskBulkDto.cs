namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Массовое назначение задачи нескольким кандидатам из подборки.</summary>
public record AssignTaskBulkDto
{
    /// <summary>ID кандидатов (users.id), выбранные в интерфейсе поиска.</summary>
    public required List<Guid> CandidateIds { get; init; }
}