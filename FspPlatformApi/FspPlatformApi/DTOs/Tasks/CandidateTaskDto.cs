namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Назначенное кандидату задание с его ответом и оценкой.</summary>
public record CandidateTaskDto
{
    /// <summary>ID назначения (candidate_tasks.id).</summary>
    public required Guid Id { get; init; }

    /// <summary>Название задачи.</summary>
    public required string Title { get; init; }

    /// <summary>Описание.</summary>
    public required string Description { get; init; }

    /// <summary>Компания-автор.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Статус («назначено» / «в_работе» / «выполнено» / «пропущено» / «срок_истек»).</summary>
    public required string Status { get; init; }

    /// <summary>Дата назначения.</summary>
    public required DateTime AssignedAt { get; init; }

    /// <summary>Дата выполнения.</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Оценка работодателя.</summary>
    public decimal? Score { get; init; }

    /// <summary>Текст ответа кандидата (null, пока не отправлен) — чтобы кандидат мог перечитать своё решение.</summary>
    public string? AnswerText { get; init; }
}