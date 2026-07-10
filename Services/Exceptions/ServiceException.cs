namespace BookingHub.Api.Services.Exceptions;

/// <summary>
/// Wyjątek domenowy rzucany przez serwisy w przypadku naruszenia reguł biznesowych
/// lub braku zasobu. Kontroler przechwytuje go i tłumaczy na odpowiedni status HTTP.
/// </summary>
public sealed class ServiceException : Exception
{
    /// <summary>Kod błędu domenowego — kontroler mapuje go na status HTTP.</summary>
    public ServiceErrorCode ErrorCode { get; }

    /// <summary>
    /// Opcjonalna nazwa pola, którego dotyczy błąd (np. "Email", "Name").
    /// Używana przez kontroler do budowania odpowiedzi walidacyjnej 400.
    /// </summary>
    public string? FieldName { get; }

    public ServiceException(ServiceErrorCode errorCode, string message, string? fieldName = null)
        : base(message)
    {
        ErrorCode = errorCode;
        FieldName = fieldName;
    }
}
