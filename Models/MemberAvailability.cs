namespace BookingHub.Api.Models;

public class MemberAvailability : BaseEntity
{
    public Guid OrganizationMemberId { get; set; }

    /// <summary>Dzień tygodnia którego dotyczy slot dostępności</summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Godzina rozpoczęcia dostępności</summary>
    public TimeOnly TimeFrom { get; set; }

    /// <summary>Godzina zakończenia dostępności</summary>
    public TimeOnly TimeTo { get; set; }

    /// <summary>Od kiedy slot obowiązuje. Null = od zawsze.</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>Do kiedy slot obowiązuje. Null = bezterminowo.</summary>
    public DateOnly? ValidTo { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public OrganizationMember OrganizationMember { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
