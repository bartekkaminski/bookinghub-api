namespace BookingHub.Api.Models;

public class MessageRecipient : BaseEntity
{
    public Guid MessageId { get; set; }

    /// <summary>Odbiorca wiadomości</summary>
    public Guid RecipientMemberId { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public Message Message { get; set; } = null!;
    public OrganizationMember Recipient { get; set; } = null!;
}
