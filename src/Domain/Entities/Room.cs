using PairCode.Domain.Enums;

namespace PairCode.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string AccessCode { get; private set; } = null!;
    public RoomStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private readonly List<Participant> _participants = [];
    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();

    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private readonly List<SharedDocument> _documents = [];
    public IReadOnlyCollection<SharedDocument> Documents => _documents.AsReadOnly();

    private Room() { }

    public Room(string name, Guid createdBy)
    {
        Id = Guid.NewGuid();
        Name = name;
        AccessCode = GenerateAccessCode();
        Status = RoomStatus.Created;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void Open()
    {
        if (Status != RoomStatus.Created)
            throw new InvalidOperationException("Only created rooms can be opened.");

        Status = RoomStatus.Active;
    }

    public void Close()
    {
        if (Status != RoomStatus.Active)
            throw new InvalidOperationException("Only active rooms can be closed.");

        Status = RoomStatus.Finished;
    }

    public void Cancel()
    {
        if (Status is RoomStatus.Finished or RoomStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a finished or already cancelled room.");

        Status = RoomStatus.Cancelled;
    }

    public void UpdateName(string newName)
    {
        Name = newName;
    }

    private static string GenerateAccessCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
