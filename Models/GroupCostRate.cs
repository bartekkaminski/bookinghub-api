namespace BookingHub.Api.Models;

public class GroupCostRate : BaseEntity
{
    public Guid GroupId { get; set; }

    /// <summary>Miesięczna stawka za uczestnictwo w grupie</summary>
    public decimal MonthlyCost { get; set; }

    /// <summary>Waluta, np. "PLN"</summary>
    public string Currency { get; set; } = "PLN";

    /// <summary>Od kiedy stawka obowiązuje</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Do kiedy stawka obowiązuje. Null = aktualnie obowiązująca.</summary>
    public DateOnly? ValidTo { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Group Group { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
