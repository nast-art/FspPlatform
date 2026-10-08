using System.Diagnostics;
using FspPlatformApi.Services.Interfaces.Testing;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// Docker-песочница для code-задач:
/// - каждый язык описан LanguageSpec: образ, имя файла, команда компиляции (опционально), команда запуска;
/// - компилируемые языки собираются ОДИН раз на прогон, дальше каждый тест-кейс
///   исполняет готовый бинарник (иначе javac/dotnet на каждый кейс = минуты);
/// - сравнение вывода с эталоном построчное, без участия ИИ.
///
/// Перед первым запуском подтяжите образы:
///   docker pull python:3.11-slim node:20-alpine mono:6 eclipse-temurin:17-jdk golang:1.22-alpine gcc:13
/// </summary>
public class CodeRunnerService : ICodeRunnerService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<CodeRunnerService> _logger;
    private readonly string _workRoot;
    private readonly string? _sharedVolume;

    private record LanguageSpec(
        string Image,
        string FileName,
        string? CompileCmd,   // null — интерпретируемый язык, компиляция не нужна
        string RunCmd);

    private static readonly Dictionary<string, LanguageSpec> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["python"]     = new LanguageSpec("python:3.11-slim",      "solution.py",   null,                                              "python3 solution.py"),
        ["javascript"] = new LanguageSpec("node:20-alpine",        "solution.js",   null,                                              "node solution.js"),
        ["js"]         = new LanguageSpec("node:20-alpine",        "solution.js",   null,                                              "node solution.js"),
        ["csharp"]     = new LanguageSpec("mono:6",                "Solution.cs",   "mcs -out:solution.exe Solution.cs",               "mono solution.exe"),
        ["c#"]         = new LanguageSpec("mono:6",                "Solution.cs",   "mcs -out:solution.exe Solution.cs",               "mono solution.exe"),
        ["java"]       = new LanguageSpec("eclipse-temurin:17-jdk","Solution.java", "javac Solution.java",                             "java Solution"),
        ["go"]         = new LanguageSpec("golang:1.22-alpine",    "solution.go",   "go build -o solution solution.go",                "./solution"),
        ["golang"]     = new LanguageSpec("golang:1.22-alpine",    "solution.go",   "go build -o solution solution.go",                "./solution"),
        ["cpp"]        = new LanguageSpec("gcc:13",                "solution.cpp",  "g++ -O2 -std=c++17 -o solution solution.cpp",     "./solution"),
        ["c++"]        = new LanguageSpec("gcc:13",                "solution.cpp",  "g++ -O2 -std=c++17 -o solution solution.cpp",     "./solution"),
    };

    public CodeRunnerService(IConfiguration cfg, ILogger<CodeRunnerService> logger, IWebHostEnvironment env)
    {
        _cfg = cfg; _logger = logger;
        // Каталог для файлов запуска. Переопределяется через "CodeRunner:WorkRoot"
        // (например, на точку монтирования общего named volume — тогда пути совпадают
        // и внутри контейнера API, и для демона Docker при вложенном docker run).
        _workRoot = cfg["CodeRunner:WorkRoot"]
            ?? Path.Combine(env.ContentRootPath, "Data", "CodeRuns");
        Directory.CreateDirectory(_workRoot);

        // Если задан "CodeRunner:Volume", код передаётся раннеру через named volume
        // (единственный способ, работающий в Docker Desktop на Windows, где bind-mount
        // вложенного docker run резолвится на хосте, а не в этом контейнере);
        // иначе — классический bind-mount каталога WorkRoot (когда API запущен на хосте).
        _sharedVolume = cfg["CodeRunner:Volume"];
    }

    public async Task<CodeRunResult> RunAsync(string language, string code,
        IReadOnlyList<CodeTestCase> testCases, CancellationToken ct = default)
    {
        if (!Languages.TryGetValue(language, out var spec))
            return new CodeRunResult(0, testCases.Count, new List<string>(),
                $"Язык '{language}' не поддерживается раннером. Доступны: {string.Join(", ", Languages.Keys.Distinct())}.");

        var runDir = Path.Combine(_workRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(runDir, spec.FileName), code, ct);

            // 1) компиляция (один раз на прогон)
            if (spec.CompileCmd is not null)
            {
                var (ok, _, error) = await RunContainerAsync(spec, runDir, spec.CompileCmd,
                    input: null, timeoutSec: _cfg.GetValue<int>("CodeRunner:CompileTimeoutSeconds", 40), ct);
                if (!ok)
                    return new CodeRunResult(0, testCases.Count, new List<string>(),
                        $"Ошибка компиляции: {(error ?? "неизвестная").Trim()}");
            }

            // 2) прогон по скрытым тест-кейсам
            var outputs = new List<string>();
            var passed = 0;
            string? firstError = null;
            var timeoutSec = _cfg.GetValue<int>("CodeRunner:TimeoutSeconds", 10);

            foreach (var tc in testCases)
            {
                var (ok, output, error) = await RunContainerAsync(spec, runDir, spec.RunCmd, tc.Input, timeoutSec, ct);
                outputs.Add(output ?? string.Empty);
                if (ok && Normalize(output) == Normalize(tc.Expected)) passed++;
                else if (!ok) firstError ??= error;
            }
            return new CodeRunResult(passed, testCases.Count, outputs, firstError);
        }
        finally
        {
            try { Directory.Delete(runDir, recursive: true); } catch { /* не блокируем выдачу результата */ }
        }
    }

    /// <summary>
    /// Аргументы монтирования и рабочего каталога для контейнера-раннера.
    /// Режим 1 (CodeRunner:Volume задан): общий named volume, подкаталог прогона — рабочий каталог.
    /// Режим 2: bind-mount каталога прогона (rw: компилятор пишет бинарники).
    /// </summary>
    private string MountArgs(string runDir)
    {
        if (!string.IsNullOrWhiteSpace(_sharedVolume))
        {
            var runName = Path.GetFileName(runDir);
            return $"-v \"{_sharedVolume}:/code\" -w \"/code/{runName}\"";
        }
        return $"-v \"{runDir}:/code\" -w /code";
    }

    /// <summary>
    /// Один контейнер на команду: --network none, лимиты памяти/CPU/pids, read-only отключён
    /// (компиляторам нужна запись в /code), stdin — вход тест-кейса.
    /// </summary>
    private async Task<(bool Ok, string? Output, string? Error)> RunContainerAsync(
        LanguageSpec spec, string runDir, string cmd, string? input, int timeoutSec, CancellationToken ct)
    {
        var memory = _cfg.GetValue<string>("CodeRunner:MemoryLimit", "256m");
        var cpus = _cfg.GetValue<string>("CodeRunner:Cpus", "0.5");

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = string.Join(" ",
                "run --rm",
                "--network none",
                $"--memory {memory}",
                $"--cpus {cpus}",
                "--pids-limit 64",
                MountArgs(runDir),
                "-i",
                "--entrypoint sh",
                spec.Image,
                "-c", $"\"{cmd}\"")
        };

        using var proc = new Process { StartInfo = psi };
        try { proc.Start(); } catch (Exception ex) { return (false, null, $"docker: {ex.Message}"); }

        if (input is not null)
        {
            await proc.StandardInput.WriteAsync(input);
        }
        proc.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        var outputTask = proc.StandardOutput.ReadToEndAsync();
        var errorTask = proc.StandardError.ReadToEndAsync();

        try
        {
            await proc.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            return (false, null, $"Превышено время выполнения ({timeoutSec} с).");
        }

        var output = await outputTask;
        var error = await errorTask;
        return (proc.ExitCode == 0, output, proc.ExitCode == 0 ? null : error);
    }

    private static string Normalize(string? s) =>
        (s ?? string.Empty).Replace("\r\n", "\n").TrimEnd('\n', '\r', ' ', '\t');
}