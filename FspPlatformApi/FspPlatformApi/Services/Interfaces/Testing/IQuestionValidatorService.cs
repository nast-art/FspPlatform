namespace FspPlatformApi.Services.Interfaces.Testing;

/// <summary>
/// Валидация сгенерированного банка ПЕРЕД вставкой в БД.
/// Порт tools/validate-questions.mjs: эталон quiz, рубрики open, max_score,
/// и прогон эталонных решений code-задач по тест-кейсам через ICodeRunnerService (Docker).
/// </summary>
public interface IQuestionValidatorService
{
    Task<ValidationReport> ValidateAsync(GeneratedQuestionBatch batch, CancellationToken ct = default);
}

public class ValidationReport
{
    public int Checked { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool IsValid => Errors.Count == 0;
}