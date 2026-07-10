using BookingHub.Api.Models;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingHub.Api.Infrastructure.Authorization;

/// <summary>
/// Filtr akcji weryfikujący, że zalogowany użytkownik jest członkiem organizacji
/// z wymaganymi rolami. Organizacja jest identyfikowana przez parametr trasy "organizationId".
///
/// Użycie:
///   [RequireOrgMembership]                           — dowolny aktywny członek
///   [RequireOrgMembership(OrgRoles.Admin)]           — tylko Admin
///   [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Trainer)] — Admin LUB Trainer
///
/// Filtr jest asynchroniczny i scoped — rejestrowany przez IFilterFactory.
/// Nie używa polityk ASP.NET Core (które są per-request bez dostępu do trasy),
/// lecz bezpośrednio wywołuje ICurrentUserService.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireOrgMembershipAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _requiredRoles;

    /// <param name="requiredRoles">
    /// Opcjonalne role — jeśli puste, wystarczy bycie dowolnym aktywnym członkiem.
    /// Wiele ról = OR (wystarczy jedna).
    /// </param>
    public RequireOrgMembershipAttribute(params string[] requiredRoles)
    {
        _requiredRoles = requiredRoles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // 1. Sprawdź czy użytkownik jest zalogowany
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status   = StatusCodes.Status401Unauthorized,
                Title    = "Wymagane uwierzytelnienie.",
                Extensions = { ["errorCode"] = "Unauthorized" },
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        // 2. Pobierz organizationId z route values
        if (!context.RouteData.Values.TryGetValue("organizationId", out var orgIdObj)
            || !Guid.TryParse(orgIdObj?.ToString(), out var organizationId))
        {
            // Brak organizationId w trasie — filtr nie dotyczy tego endpointu, przepuszczamy.
            await next();
            return;
        }

        // 3. Pobierz ICurrentUserService z DI
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        // 4. Sprawdź członkostwo
        var member = await currentUser.GetMemberAsync(organizationId, httpContext.RequestAborted);
        if (member is null || !member.IsActive)
        {
            context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status   = StatusCodes.Status403Forbidden,
                Title    = "Brak dostępu do organizacji.",
                Detail   = "Nie jesteś aktywnym członkiem tej organizacji.",
                Extensions = { ["errorCode"] = "NotMember" },
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        // 5. Sprawdź wymagane role (OR logic)
        if (_requiredRoles.Length > 0)
        {
            var memberRoles = member.Roles.Select(r => r.Role.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hasRequiredRole = _requiredRoles.Any(r => memberRoles.Contains(r));

            if (!hasRequiredRole)
            {
                context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status   = StatusCodes.Status403Forbidden,
                    Title    = "Niewystarczające uprawnienia.",
                    Detail   = $"Wymagana rola: {string.Join(" lub ", _requiredRoles)}.",
                    Extensions = { ["errorCode"] = "InsufficientRole" },
                }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }

        await next();
    }
}
