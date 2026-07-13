using BookingHub.Api.Data;
using BookingHub.Api.Hubs;
using BookingHub.Api.Infrastructure;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Middleware;
using BookingHub.Api.Repositories;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Settings;
using DotNetEnv;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// ── Firebase Admin SDK (opcjonalny — graceful degradation jeśli brak konfiguracji) ──
var firebaseKeyRaw = builder.Configuration["Firebase:ServiceAccountKeyJson"]
    ?? Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY");

// Normalizacja: usuń otaczające białe znaki i ewentualne apostrofy (z .env z single-quote syntax)
var firebaseKeyNormalized = firebaseKeyRaw?.Trim().Trim('\'').Trim('"').Trim();

string? firebaseKeyJson = null;

if (!string.IsNullOrWhiteSpace(firebaseKeyNormalized))
{
    if (firebaseKeyNormalized.StartsWith('{'))
    {
        // Surowy JSON — używamy bezpośrednio
        firebaseKeyJson = firebaseKeyNormalized;
    }
    else
    {
        // Prawdopodobnie Base64 — usuń whitespace (DigitalOcean może dodać newline) i zdekoduj
        try
        {
            var cleanBase64 = firebaseKeyNormalized
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "");
            firebaseKeyJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cleanBase64));
        }
        catch (FormatException ex)
        {
            Console.Error.WriteLine($"[Firebase] Nie udało się zdekodować Base64: {ex.Message}. Próbuję użyć jako raw JSON.");
            // Ostatnia próba — może to jednak JSON z dziwnym formatowaniem
            firebaseKeyJson = firebaseKeyNormalized;
        }
    }
}

if (!string.IsNullOrWhiteSpace(firebaseKeyJson))
{
    try
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(firebaseKeyJson));
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromStream(stream),
        });
        Console.WriteLine("[Firebase] Admin SDK zainicjowany pomyślnie.");
    }
    catch (Exception ex)
    {
        // Loguj błąd — FCM jest opcjonalny, ale warto wiedzieć co poszło nie tak
        Console.Error.WriteLine($"[Firebase] Błąd inicjalizacji: {ex.Message}");
        // Zapisz błąd globalnie — dostępny przez /api/diagnostics
        FirebaseInitError.Message = ex.Message;
    }
}
else if (!string.IsNullOrWhiteSpace(firebaseKeyRaw))
{
    Console.Error.WriteLine("[Firebase] Klucz jest obecny, ale po normalizacji jest pusty.");
    FirebaseInitError.Message = "Key present but empty after normalization.";
}

// ── Uwierzytelnianie JWT (Kinde jako OIDC provider) ──────────────────────────
var kindeAuthority = builder.Configuration["Kinde:Authority"]
    ?? throw new InvalidOperationException("Brak konfiguracji 'Kinde:Authority'.");
var kindeAudience = builder.Configuration["Kinde:Audience"]
    ?? throw new InvalidOperationException("Brak konfiguracji 'Kinde:Audience'.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = kindeAuthority;
        options.Audience  = kindeAudience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            NameClaimType            = "sub",
        };

        // Zachowaj oryginalne nazwy claims z Kinde (sub, email, given_name…)
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            // Obsługa tokenu JWT dla połączeń SignalR WebSocket.
            // SignalR WebSocket nie może wysyłać nagłówka Authorization,
            // więc klient przesyła token jako query string ?access_token=...
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT auth failed: {Message}", ctx.Exception.Message);
                return Task.CompletedTask;
            },
        };
    });

// ── Polityki autoryzacji ─────────────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiPolicies.AuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());

// ── Globalna obsługa błędów — RFC 7807 ProblemDetails ────────────────────────
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Kontrolery + JSON ─────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
.AddJsonProtocol(options =>
{
    // Spójność z kontrolerami — camelCase + enumy jako stringi
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ── OpenAPI + Scalar UI ───────────────────────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title       = "BookingHub API",
            Version     = "v1",
            Description = "API platformy do zarządzania szkołami tańca, klubami i innymi placówkami. " +
                          "Autoryzacja: Bearer JWT z Kinde — wywołaj POST /api/auth/me po zalogowaniu.",
        };

        var jwtScheme = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Name         = "Authorization",
            Description  = "Token JWT z Kinde. Wklej sam token (bez prefiksu 'Bearer').",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = jwtScheme;

        var schemeRef   = new OpenApiSecuritySchemeReference("Bearer", document);
        var requirement = new OpenApiSecurityRequirement { [schemeRef] = [] };
        document.Security ??= [];
        document.Security.Add(requirement);

        return Task.CompletedTask;
    });
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "database");

// ── CORS ───────────────────────────────────────────────────────────────────────
var rawOrigins = builder.Configuration["Cors:Origins"]
    ?? throw new InvalidOperationException("Brak konfiguracji 'Cors:Origins'.");

var corsOrigins = rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // wymagane dla SignalR WebSocket
    });
});

// ── HttpContext + Memory Cache ─────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// ── HttpClient (Kinde Management API) ─────────────────────────────────────────
builder.Services.AddHttpClient("KindeManagement");

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Brak connection string 'DefaultConnection'.");

    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null));
});

// ── Opcje domenowe ─────────────────────────────────────────────────────────────
builder.Services.Configure<OrganizationLimits>(
    builder.Configuration.GetSection(OrganizationLimits.SectionName));

// ── Repositories ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventEnrollmentRepository, EventEnrollmentRepository>();
builder.Services.AddScoped<IEventTeamEnrollmentRepository, EventTeamEnrollmentRepository>();
builder.Services.AddScoped<ICancellationRequestRepository, CancellationRequestRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMemberAvailabilityRepository, MemberAvailabilityRepository>();
builder.Services.AddScoped<IGroupCostRateRepository, GroupCostRateRepository>();
builder.Services.AddScoped<ITrainerSessionRateRepository, TrainerSessionRateRepository>();
builder.Services.AddScoped<IParentChildRelationRepository, ParentChildRelationRepository>();
builder.Services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();

// ── Services ───────────────────────────────────────────────────────────────────

// Singleton — cache tokenu Kinde M2M współdzielony przez wszystkie żądania
builder.Services.AddSingleton<IKindeManagementService, KindeManagementService>();

// Singleton — Firebase Admin SDK ma własny lifecycle;
// FcmService używa IServiceScopeFactory do tworzenia zakresów per operacja
builder.Services.AddSingleton<IFcmService, FcmService>();

// Scoped — jeden per HTTP request
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IEventSeriesService, EventSeriesService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICancellationRequestService, CancellationRequestService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<ICostService, CostService>();

// BackgroundService — Singleton, przetwarza OutboxEvent i wysyła przez SignalR/FCM
builder.Services.AddHostedService<OutboxProcessor>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────

// 1. Globalna obsługa wyjątków
app.UseExceptionHandler();

// 2. Scalar API UI + OpenAPI — tylko w Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title             = "BookingHub API";
        options.Theme             = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
    });

    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
       .ExcludeFromDescription();
}

// 3. CORS (przed HTTPS redirect, aby OPTIONS preflight nie był przekierowywany)
app.UseCors("FrontendPolicy");

// 4. HTTPS redirect (tylko poza Development)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 5. JWT Authentication
app.UseAuthentication();

// 6. ActiveUser middleware — ładuje User z bazy do HttpContext.Items
app.UseMiddleware<ActiveUserMiddleware>();

// 7. Authorization
app.UseAuthorization();

// 8. Kontrolery REST
app.MapControllers();

// 9. SignalR Hub
app.MapHub<AppHub>("/hubs/app");

// 10. Health check — bez wymogu autoryzacji
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
