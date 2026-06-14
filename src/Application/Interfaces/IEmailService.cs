namespace PairCode.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string userName, string resetLink);
    Task SendVerificationEmailAsync(string email, string userName, string verificationLink);
}
