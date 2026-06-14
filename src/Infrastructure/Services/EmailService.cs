using Microsoft.Extensions.Logging;
using PairCode.Application.Interfaces;

namespace PairCode.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    public Task SendPasswordResetEmailAsync(string email, string userName, string resetLink)
    {
        _logger.LogInformation("[EMAIL] Password reset requested for {Email} ({Name})", email, userName);
        _logger.LogInformation("[EMAIL] Reset link: {Link}", resetLink);
        return Task.CompletedTask;
    }

    public Task SendVerificationEmailAsync(string email, string userName, string verificationLink)
    {
        _logger.LogInformation("[EMAIL] Email verification for {Email} ({Name})", email, userName);
        _logger.LogInformation("[EMAIL] Verify link: {Link}", verificationLink);
        return Task.CompletedTask;
    }
}
