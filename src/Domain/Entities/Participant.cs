namespace PairCode.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoomId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    public User User { get; private set; } = null!;
    public Room Room { get; private set; } = null!;

    private Participant() { }

    public Participant(Guid userId, Guid roomId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoomId = roomId;
        JoinedAt = DateTime.UtcNow;
    }

    public void MarkLeft()
    {
        LeftAt = DateTime.UtcNow;
    }

    public TimeSpan? GetSessionDuration()
    {
        if (LeftAt.HasValue)
            return LeftAt.Value - JoinedAt;

        return null;
    }
}
