namespace PairCode.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool Used { get; private set; }

    public User User { get; private set; } = null!;

    private PasswordResetToken() { }

    public PasswordResetToken(Guid userId, string token, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        Used = false;
    }

    public void MarkUsed() => Used = true;
    public bool IsValid() => !Used && DateTime.UtcNow < ExpiresAt;
}
