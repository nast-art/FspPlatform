using System.Net.Http.Headers;
using System.Text.Json;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// OAuth-обмен для учётных данных GigaChat (base64(client_id:client_secret)).
/// Формат запроса — по документации Sber: POST /api/v2/oauth,
/// Authorization: Basic, обязательный заголовок RqUID (UUID на каждый запрос).
/// Кэширует access_token до истечения срока (с запасом 2 минуты).
/// Scope: GIGACHAT_API_PERS (персональный) или GIGACHAT_API_COR (корпоративный).
/// ВАЖНО: expires_at приходит в МИЛЛИСЕКУНДАХ Unix-эпохи — парсим FromUnixTimeMilliseconds.
/// </summary>
public class GigaChatTokenProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<GigaChatTokenProvider> _log;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public GigaChatTokenProvider(IHttpClientFactory httpFactory, IConfiguration cfg,
        ILogger<GigaChatTokenProvider> log)
        => (_httpFactory, _cfg, _log) = (httpFactory, cfg, log);

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        // возвращаем кэш, пока он жив (с запасом 2 минуты до истечения)
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt.AddMinutes(-2))
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // повторная проверка внутри лока: другой поток мог уже обновить
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt.AddMinutes(-2))
                return _cachedToken;

            // ApiKey в appsettings хранит base64(client_id:client_secret)
            var basic = _cfg["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey не задан (base64(client_id:client_secret))");
            var scope = _cfg["OpenAI:Scope"] ?? "GIGACHAT_API_PERS";

            var client = _httpFactory.CreateClient("ai");
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");

            // Документация Sber: RqUID обязателен — уникальный UUID каждого запроса
            request.Headers.TryAddWithoutValidation("RqUID", Guid.NewGuid().ToString());
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["scope"] = scope
            });

            using var resp = await client.SendAsync(request, ct);
            var payload = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"GigaChat OAuth вернул HTTP {(int)resp.StatusCode}: {payload}");

            using var doc = JsonDocument.Parse(payload);
            _cachedToken = doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("GigaChat OAuth не вернул access_token");

            // ИСПРАВЛЕНО: expires_at — миллисекунды Unix-эпохи, не секунды
            _expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(
                doc.RootElement.GetProperty("expires_at").GetInt64());

            _log.LogInformation("Получен access_token GigaChat, действует до {Exp} UTC", _expiresAt);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}