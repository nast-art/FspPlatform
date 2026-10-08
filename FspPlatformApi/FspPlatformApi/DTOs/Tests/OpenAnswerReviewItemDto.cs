namespace FspPlatformApi.DTOs.Tests;

/// <summary>
/// Элемент очереди экспертной перепроверки open-ответов (10–20% выборочно).
/// Экспертный скор перезаписывает первичный ИИ-скор.
/// </summary>
public record OpenAnswerReviewItemDto
{
    /// <summary>ID строки ответа (test_session_answers.id).</summary>
    public required long AnswerId { get; init; }

    /// <summary>ID тестовой сессии.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>ID кандидата.</summary>
    public required Guid CandidateId { get; init; }

    /// <summary>Имя кандидата.</summary>
    public required string CandidateName { get; init; }

    /// <summary>Формулировка вопроса.</summary>
    public required string QuestionPrompt { get; init; }

    /// <summary>Рубрика оценивания (из correct_payload.rubric).</summary>
    public required string Rubric { get; init; }

    /// <summary>Ответ кандидата.</summary>
    public string? AnswerText { get; init; }

    /// <summary>Первичный ИИ-скор.</summary>
    public decimal? AiScore { get; init; }

    /// <summary>Обратная связь ИИ (если сохранялась).</summary>
    public string? AiFeedback { get; init; }

    /// <summary>Экспертный скор (null — ещё не проверен).</summary>
    public decimal? ExpertScore { get; init; }
}