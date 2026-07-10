using BookingHub.Api.Data;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Middleware;
using BookingHub.Api.Repositories;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Settings;
using DotNetEnv;
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
            NameClaimType            = "sub",   // claim 'sub' = identyfikator użytkownika
        };

        // Wyłącz automatyczne mapowanie claims — zachowaj oryginalne nazwy z Kinde (sub, email, given_name…)
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
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
// Autoryzacja per-rola-w-organizacji odbywa się przez [RequireOrgMembership(...)] filter.
// Tutaj definiujemy tylko politykę globalną "zalogowany użytkownik".
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
        // Enumy jako stringi (czytelne dla frontendu: "Enrolled" zamiast 0)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());

        // Zachowaj camelCase (domyślnie w .NET — ale jawnie dla pewności)
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
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
              .AllowCredentials();
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

// Scoped — jeden per HTTP request
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
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

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────

// 1. Globalna obsługa wyjątków (ServiceException → właściwy HTTP, inne → 500)
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

    // Przekierowanie z root / → dokumentacja Scalar
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
       .ExcludeFromDescription();
}

// 3. CORS (przed HTTPS redirect, aby OPTIONS preflight nie był przekierowywany)
app.UseCors("FrontendPolicy");

// 4. HTTPS redirect (tylko poza Development, żeby nie blokować CORS preflight)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 5. JWT Authentication
app.UseAuthentication();

// 6. ActiveUser middleware — ładuje User z bazy do HttpContext.Items, blokuje dezaktywowane konta
app.UseMiddleware<ActiveUserMiddleware>();

// 7. Authorization
app.UseAuthorization();

// 8. Kontrolery
app.MapControllers();

// 9. Health check — bez wymogu autoryzacji
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
