namespace PairCode.Domain.Entities;

public class SharedDocument
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public string Content { get; private set; } = null!;
    public int Version { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Room Room { get; private set; } = null!;

    private SharedDocument() { }

    public SharedDocument(Guid roomId)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        Content = string.Empty;
        Version = 1;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string newContent)
    {
        if (newContent.Length > 10_000_000)
            throw new InvalidOperationException("Document content exceeds maximum size of 10MB");

        Content = newContent;
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
