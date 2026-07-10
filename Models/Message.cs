namespace BookingHub.Api.Models;

public class Message : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Nadawca wiadomości</summary>
    public Guid SenderMemberId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    /// <summary>True = wygenerowana przez system (np. automatyczne powiadomienie o odwołaniu zajęć)</summary>
    public bool IsAutomatic { get; set; } = false;

    /// <summary>Opcjonalne powiązanie z konkretnymi zajęciami</summary>
    public Guid? RelatedEventId { get; set; }

    /// <summary>FK do wiadomości nadrzędnej — null = nowy wątek, wypełnione = odpowiedź</summary>
    public Guid? ParentMessageId { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public OrganizationMember Sender { get; set; } = null!;
    public Event? RelatedEvent { get; set; }
    public Message? ParentMessage { get; set; }
    public Person? CreatedBy { get; set; }
    public ICollection<MessageRecipient> Recipients { get; set; } = [];
    public ICollection<Message> Replies { get; set; } = [];
}
