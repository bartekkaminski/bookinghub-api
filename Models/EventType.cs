namespace BookingHub.Api.Models;

public enum EventType
{
    /// <summary>Trening grupowy — zajęcia dla całej grupy</summary>
    GroupTraining,

    /// <summary>Zajęcia indywidualne z trenerem — solo lub para</summary>
    IndividualSession,

    /// <summary>Obóz / camp — jednorazowy koszt per uczestnik</summary>
    Camp,

    /// <summary>Inne wydarzenie</summary>
    Other
}
