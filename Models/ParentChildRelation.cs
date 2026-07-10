namespace BookingHub.Api.Models;

public class ParentChildRelation : BaseEntity
{
    public Guid ParentPersonId { get; set; }
    public Guid ChildPersonId { get; set; }

    /// <summary>Admin który utworzył relację</summary>
    public Guid? CreatedByPersonId { get; set; }

    public Person Parent { get; set; } = null!;
    public Person Child { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
