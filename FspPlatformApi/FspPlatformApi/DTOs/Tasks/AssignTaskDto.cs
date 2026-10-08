namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Назначение задачи конкретному кандидату.</summary>
public record AssignTaskDto
{
    /// <summary>ID кандидата.</summary>
    public required Guid CandidateId { get; init; }
}