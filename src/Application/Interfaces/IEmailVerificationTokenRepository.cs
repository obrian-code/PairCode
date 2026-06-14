using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token);
    Task<EmailVerificationToken?> GetByTokenAsync(string token);
    Task<EmailVerificationToken?> GetValidByUserIdAsync(Guid userId);
    Task UpdateAsync(EmailVerificationToken token);
}
