namespace PairCode.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string Message { get; private set; } = null!;
    public DateTime SentAt { get; private set; }

    public Room Room { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ChatMessage() { }

    public ChatMessage(Guid roomId, Guid userId, string message)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        UserId = userId;
        Message = message;
        SentAt = DateTime.UtcNow;
    }
}
