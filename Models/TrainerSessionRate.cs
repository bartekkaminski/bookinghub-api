namespace BookingHub.Api.Models;

public class TrainerSessionRate : BaseEntity
{
    /// <summary>Trener którego stawka dotyczy (OrganizationMember z rolą Trainer)</summary>
    public Guid TrainerMemberId { get; set; }

    /// <summary>
    /// Stawka za godzinę zajęć indywidualnych.
    /// Para: koszt = RatePerHour × czas ÷ liczba uczestników.
    /// Solista: koszt = RatePerHour × czas.
    /// </summary>
    public decimal RatePerHour { get; set; }

    /// <summary>Waluta, np. "PLN"</summary>
    public string Currency { get; set; } = "PLN";

    /// <summary>Od kiedy stawka obowiązuje</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Do kiedy stawka obowiązuje. Null = aktualnie obowiązująca.</summary>
    public DateOnly? ValidTo { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public OrganizationMember Trainer { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
