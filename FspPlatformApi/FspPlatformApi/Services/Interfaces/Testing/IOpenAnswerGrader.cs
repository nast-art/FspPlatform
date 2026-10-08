namespace FspPlatformApi.Services.Interfaces.Testing;

public record OpenGradeResult(decimal Score, string Feedback, string GradedBy); // "ai" | "heuristic"

/// <summary>
/// Первичная проверка open-вопросов строго по зафиксированной рубрике (correct_payload.rubric).
/// Реализация: ИИ (OpenAI-совместимый API), при отсутствии ключа — эвристический грейдер по ключевым
/// критериям рубрики. В любом случае auto_graded=false и ответ попадает в выборочную экспертную перепроверку.
/// </summary>
public interface IOpenAnswerGrader
{
    Task<OpenGradeResult> GradeAsync(string prompt, string rubric, string answer, decimal maxScore,
        CancellationToken ct = default);
}