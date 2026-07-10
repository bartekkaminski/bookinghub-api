using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookingHub.Api.Services;

/// <summary>
/// Klient Kinde Management API.
/// Pobiera token M2M (Client Credentials) i cachuje go do wygaśnięcia.
/// Rejestrowany jako Singleton — cache tokenu współdzielony per instancja.
/// </summary>
public sealed class KindeManagementService : IKindeManagementService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KindeManagementService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string Authority     => _configuration["Kinde:ManagementApiUrl"]!;
    private string ClientId      => _configuration["Kinde:ManagementClientId"]!;
    private string ClientSecret  => _configuration["Kinde:ManagementClientSecret"]!;

    public KindeManagementService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<KindeManagementService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;
    }

    /// <inheritdoc/>
    public async Task<string> CreateUserInKindeAsync(string firstName, string lastName, string email, CancellationToken ct = default)
    {
        var token  = await GetManagementTokenAsync(ct);
        var client = CreateClient(token);

        var payload = new
        {
            profile = new { given_name = firstName, family_name = lastName },
            identities = new[] { new { type = "email", details = new { email } } }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var resp    = await client.PostAsync("/api/v1/user", content, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.Conflict ||
            resp.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            throw new ServiceException(ServiceErrorCode.EmailAlreadyTaken,
                $"Adres e-mail '{email}' jest już zarejestrowany w Kinde.", "Email");
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Błąd tworzenia użytkownika w Kinde {Status}: {Body}", resp.StatusCode, body);
            throw new ServiceException(ServiceErrorCode.KindeApiError,
                $"Kinde Management API zwróciło błąd: {resp.StatusCode}");
        }

        var json   = await resp.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<KindeCreateUserResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ServiceException(ServiceErrorCode.KindeApiError,
                "Kinde zwrócił pustą odpowiedź przy tworzeniu użytkownika.");

        _logger.LogInformation("Utworzono użytkownika Kinde {KindeId} dla {Email}.", result.Id, email);
        return result.Id;
    }

    /// <inheritdoc/>
    public async Task SuspendUserAsync(string externalId, CancellationToken ct = default)
        => await SetSuspendedAsync(externalId, suspended: true, ct);

    /// <inheritdoc/>
    public async Task UnsuspendUserAsync(string externalId, CancellationToken ct = default)
        => await SetSuspendedAsync(externalId, suspended: false, ct);

    private async Task SetSuspendedAsync(string externalId, bool suspended, CancellationToken ct)
    {
        var token   = await GetManagementTokenAsync(ct);
        var client  = CreateClient(token);
        var payload = new { is_suspended = suspended };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await client.PatchAsync($"/api/v1/users/{externalId}", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Błąd {Action} konta Kinde {UserId} — status {Status}: {Body}",
                suspended ? "zawieszania" : "odwieszania", externalId, resp.StatusCode, body);
            throw new ServiceException(ServiceErrorCode.KindeApiError,
                $"Kinde Management API zwróciło błąd przy zmianie stanu konta: {resp.StatusCode}");
        }

        _logger.LogInformation("Konto Kinde {UserId} zostało {Action}.",
            externalId, suspended ? "zawieszone" : "przywrócone");
    }

    // ── Token M2M ─────────────────────────────────────────────────────────────

    private async Task<string> GetManagementTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30))
                return _cachedToken;

            var client = _httpClientFactory.CreateClient("KindeManagement");
            var form   = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = ClientId,
                ["client_secret"] = ClientSecret,
                ["audience"]      = $"{Authority}/api",
            });

            var resp = await client.PostAsync($"{Authority}/oauth2/token", form, ct);
            resp.EnsureSuccessStatusCode();

            var json      = await resp.Content.ReadAsStringAsync(ct);
            var tokenResp = JsonSerializer.Deserialize<KindeTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Pusta odpowiedź z Kinde token endpoint.");

            _cachedToken    = tokenResp.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResp.ExpiresIn);

            _logger.LogDebug("Pobrano nowy token M2M Kinde, wygasa za {Seconds}s.", tokenResp.ExpiresIn);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private HttpClient CreateClient(string bearerToken)
    {
        var client = _httpClientFactory.CreateClient("KindeManagement");
        client.BaseAddress = new Uri(Authority.TrimEnd('/'));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return client;
    }

    // ── Wewnętrzne modele deserializacji ─────────────────────────────────────

    private sealed class KindeTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]   public int    ExpiresIn   { get; set; }
    }

    private sealed class KindeCreateUserResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    }
}
