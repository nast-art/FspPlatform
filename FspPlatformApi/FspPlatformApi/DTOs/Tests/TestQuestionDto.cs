namespace FspPlatformApi.DTOs.Tests;

/// <summary>
/// Вопрос тестовой сессии, как его видит кандидат.
/// ВНИМАНИЕ: correct_payload (эталон/рубрика) сюда не входит — только payload.
/// </summary>
public record TestQuestionDto
{
    /// <summary>ID вопроса (из банка test_questions).</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Порядковый номер в сессии.</summary>
    public required short OrderNo { get; init; }

    /// <summary>Тип: «тест_с_вариантами» | «задача_с_кодом» | «открытый_вопрос».</summary>
    public required string QuestionType { get; init; }

    /// <summary>Формулировка (для code — условие).</summary>
    public required string Prompt { get; init; }

    /// <summary>Шаблон кода (для code-задач), если задан в payload.template.</summary>
    public string? CodeTemplate { get; init; }

    /// <summary>
    /// Нагрузка вопроса: варианты ответов для quiz / testCases для code (скрытые от кандидата при выдаче
    /// на фронте — отдавать ожидаемые выходы кандидату не нужно; API отдаёт payload целиком,
    /// фронт рендерит только нужные поля).
    /// </summary>
    public System.Text.Json.JsonElement? Payload { get; init; }
}