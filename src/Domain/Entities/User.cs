using PairCode.Domain.Enums;

namespace PairCode.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool EmailVerified { get; private set; }

    private User() { }

    public User(string name, string email, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name)
    {
        Name = name;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }

    public void UpdatePasswordHash(string newHash)
    {
        PasswordHash = newHash;
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
    }
}
