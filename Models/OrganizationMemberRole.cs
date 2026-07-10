namespace BookingHub.Api.Models;

public class OrganizationMemberRole : BaseEntity
{
    public Guid OrganizationMemberId { get; set; }
    public MemberRole Role { get; set; }

    public OrganizationMember OrganizationMember { get; set; } = null!;
}
