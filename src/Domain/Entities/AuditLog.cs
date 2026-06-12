namespace PairCode.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string EntityName { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string Details { get; private set; } = null!;
    public DateTime Timestamp { get; private set; }

    public User User { get; private set; } = null!;

    private AuditLog() { }

    public AuditLog(Guid userId, string action, string entityName, string entityId, string details)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
