namespace FspPlatformApi.DTOs.Tasks;

/// <summary>
/// Ответ кандидата на задачу, как его видит работодатель (для вкладки «Ответы кандидатов»).
/// Свежие ответы — «свежий сигнал» о кандидате (ТЗ п.2).
/// </summary>
public record TaskCompletionDto
{
    /// <summary>ID назначения (candidate_tasks.id) — нужен для оценки.</summary>
    public required Guid CandidateTaskId { get; init; }

    /// <summary>ID задачи.</summary>
    public required Guid TaskId { get; init; }

    /// <summary>Название задачи.</summary>
    public required string TaskTitle { get; init; }

    /// <summary>ID кандидата.</summary>
    public required Guid CandidateId { get; init; }

    /// <summary>ФИО кандидата.</summary>
    public required string CandidateName { get; init; }

    /// <summary>Категория кандидата (основная специализация + грейд), если известна.</summary>
    public string? CategoryName { get; init; }

    /// <summary>Статус для отображения («Назначено» / «Выполнено» / ...).</summary>
    public required string Status { get; init; }

    /// <summary>Сырой код статуса («назначено» / «выполнено» / ...) — для логики на фронте.</summary>
    public required string StatusCode { get; init; }

    /// <summary>Текст ответа кандидата (решение или предложенный подход).</summary>
    public string? AnswerText { get; init; }

    /// <summary>Дата назначения.</summary>
    public required DateTime AssignedAt { get; init; }

    /// <summary>Дата выполнения (null — ещё не выполнено).</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Оценка работодателя (null — не оценено).</summary>
    public decimal? Score { get; init; }

    /// <summary>Максимальный балл задачи.</summary>
    public required decimal MaxScore { get; init; }
}