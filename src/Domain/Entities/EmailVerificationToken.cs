namespace PairCode.Domain.Entities;

public class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool Used { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private EmailVerificationToken() { }

    public EmailVerificationToken(Guid userId, string token, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        Used = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkUsed() => Used = true;
    public bool IsValid() => !Used && DateTime.UtcNow < ExpiresAt;
}
