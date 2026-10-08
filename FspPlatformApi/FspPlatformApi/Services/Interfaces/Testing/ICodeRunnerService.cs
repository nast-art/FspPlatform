namespace FspPlatformApi.Services.Interfaces.Testing;

public record CodeTestCase(string Input, string Expected);

public record CodeRunResult(int Passed, int Total, List<string> ActualOutputs, string? Error)
{
    public decimal RelativeScore => Total == 0 ? 0m : (decimal)Passed / Total;
}

/// <summary>
/// Детерминированный раннер code-задач: код кандидата прогоняется по скрытым тест-кейсам
/// (вход → выход) в изолированном Docker-контейнере. ИИ не участвует — ошибиться не может.
/// </summary>
public interface ICodeRunnerService
{
    Task<CodeRunResult> RunAsync(string language, string code, IReadOnlyList<CodeTestCase> testCases,
        CancellationToken ct = default);
}