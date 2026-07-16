using BookingHub.Api.Services.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Middleware;

/// <summary>
/// Globalny handler wyjątków domenowych ServiceException.
/// Mapuje ServiceErrorCode na właściwy kod HTTP i zwraca RFC 7807 ProblemDetails.
/// Rejestrowany jako pierwszy — jeśli wyjątek nie jest ServiceException, przekazuje dalej.
/// </summary>
public sealed class ServiceExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ServiceExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public ServiceExceptionHandler(ILogger<ServiceExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env    = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not ServiceException ex)
            return false;

        var statusCode = ex.ErrorCode switch
        {
            ServiceErrorCode.NotFound        or
            ServiceErrorCode.MessageNotFound
                => StatusCodes.Status404NotFound,

            ServiceErrorCode.Unauthorized
                => StatusCodes.Status401Unauthorized,

            ServiceErrorCode.Forbidden
                => StatusCodes.Status403Forbidden,

            ServiceErrorCode.Conflict              or
            ServiceErrorCode.EmailAlreadyTaken     or
            ServiceErrorCode.ExternalIdAlreadyTaken or
            ServiceErrorCode.OrganizationNameTaken  or
            ServiceErrorCode.AlreadyMember          or
            ServiceErrorCode.RoleAlreadyAssigned    or
            ServiceErrorCode.GroupNameTaken         or
            ServiceErrorCode.MemberAlreadyInGroup   or
            ServiceErrorCode.TeamAlreadyInGroup     or
            ServiceErrorCode.TrainerAlreadyAssignedToGroup        or
            ServiceErrorCode.MemberAlreadyInTeam    or
            ServiceErrorCode.TrainerAlreadyAssignedToTeam         or
            ServiceErrorCode.TrainerAlreadyAssignedToParticipant  or
            ServiceErrorCode.LocationNameTaken      or
            ServiceErrorCode.TrainerAlreadyAssignedToEvent        or
            ServiceErrorCode.MemberAlreadyEnrolled  or
            ServiceErrorCode.TeamAlreadyEnrolled    or
            ServiceErrorCode.CancellationRequestAlreadyPending    or
            ServiceErrorCode.ActiveRateAlreadyExists or
            ServiceErrorCode.RankNameTaken          or
            ServiceErrorCode.DisciplineNameTaken    or
            ServiceErrorCode.DisciplineHasRanks
                => StatusCodes.Status409Conflict,

            ServiceErrorCode.KindeApiError
                => StatusCodes.Status502BadGateway,

            ServiceErrorCode.DatabaseError
                => StatusCodes.Status500InternalServerError,

            ServiceErrorCode.NotMember               or
            ServiceErrorCode.CannotRemoveLastAdmin   or
            ServiceErrorCode.NotATrainer             or
            ServiceErrorCode.NotAParticipant
                => StatusCodes.Status403Forbidden,

            ServiceErrorCode.AccountInactive
                => StatusCodes.Status403Forbidden,

            ServiceErrorCode.EventCancelled          or
            ServiceErrorCode.EventCompleted          or
            ServiceErrorCode.EnrollmentNotActive     or
            ServiceErrorCode.CancellationRequestNotPending or
            ServiceErrorCode.InvalidEventDateRange   or
            ServiceErrorCode.InvalidRateDateRange
                => StatusCodes.Status409Conflict,

            ServiceErrorCode.MessageNoRecipients
                => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest,
        };

        if (ex.ErrorCode == ServiceErrorCode.KindeApiError)
            _logger.LogError("Błąd Kinde API [{Path}]: {Message}", context.Request.Path, ex.Message);
        else if (ex.ErrorCode == ServiceErrorCode.DatabaseError)
            _logger.LogCritical("Błąd bazy danych [{Path}]: {Message}", context.Request.Path, ex.Message);

        var pd = new ProblemDetails
        {
            Status   = statusCode,
            Title    = GetTitle(ex.ErrorCode),
            Detail   = _env.IsDevelopment() ? ex.Message : null,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };
        pd.Extensions["requestId"] = context.TraceIdentifier;
        pd.Extensions["errorCode"] = ex.ErrorCode.ToString();
        if (ex.FieldName is not null)
            pd.Extensions["field"] = ex.FieldName;

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(pd, ct);
        return true;
    }

    private static string GetTitle(ServiceErrorCode code) => code switch
    {
        ServiceErrorCode.NotFound        or
        ServiceErrorCode.MessageNotFound
            => "Zasób nie został znaleziony.",
        ServiceErrorCode.Unauthorized
            => "Brak uwierzytelnienia lub nieprawidłowy token.",
        ServiceErrorCode.Forbidden
            => "Brak uprawnień do wykonania tej operacji.",
        ServiceErrorCode.Conflict              or
        ServiceErrorCode.EmailAlreadyTaken     or
        ServiceErrorCode.ExternalIdAlreadyTaken or
        ServiceErrorCode.OrganizationNameTaken  or
        ServiceErrorCode.AlreadyMember          or
        ServiceErrorCode.RoleAlreadyAssigned    or
        ServiceErrorCode.GroupNameTaken         or
        ServiceErrorCode.MemberAlreadyInGroup   or
        ServiceErrorCode.TeamAlreadyInGroup     or
        ServiceErrorCode.TrainerAlreadyAssignedToGroup       or
        ServiceErrorCode.MemberAlreadyInTeam    or
        ServiceErrorCode.TrainerAlreadyAssignedToTeam        or
        ServiceErrorCode.TrainerAlreadyAssignedToParticipant or
        ServiceErrorCode.LocationNameTaken      or
        ServiceErrorCode.TrainerAlreadyAssignedToEvent       or
        ServiceErrorCode.MemberAlreadyEnrolled  or
        ServiceErrorCode.TeamAlreadyEnrolled    or
        ServiceErrorCode.CancellationRequestAlreadyPending   or
        ServiceErrorCode.ActiveRateAlreadyExists or
        ServiceErrorCode.RankNameTaken          or
        ServiceErrorCode.DisciplineNameTaken    or
        ServiceErrorCode.DisciplineHasRanks
            => "Konflikt — zasób już istnieje lub operacja narusza ograniczenie.",
        ServiceErrorCode.NotMember               or
        ServiceErrorCode.CannotRemoveLastAdmin   or
        ServiceErrorCode.NotATrainer             or
        ServiceErrorCode.NotAParticipant         or
        ServiceErrorCode.AccountInactive
            => "Brak uprawnień do wykonania tej operacji.",
        ServiceErrorCode.EventCancelled
            => "Zajęcia zostały odwołane.",
        ServiceErrorCode.EventCompleted
            => "Zajęcia zostały zakończone.",
        ServiceErrorCode.EnrollmentNotActive
            => "Zapis jest już odwołany lub nieaktywny.",
        ServiceErrorCode.CancellationRequestNotPending
            => "Wniosek nie jest w stanie oczekującym.",
        ServiceErrorCode.InvalidEventDateRange   or
        ServiceErrorCode.InvalidRateDateRange
            => "Nieprawidłowy zakres dat.",
        ServiceErrorCode.MessageNoRecipients
            => "Lista odbiorców wiadomości jest pusta.",
        ServiceErrorCode.KindeApiError
            => "Błąd zewnętrznego serwisu logowania (Kinde).",
        ServiceErrorCode.DatabaseError
            => "Wewnętrzny błąd serwera — operacja bazodanowa nie powiodła się.",
        _   => "Nieprawidłowe dane wejściowe lub naruszenie reguły biznesowej.",
    };
}
