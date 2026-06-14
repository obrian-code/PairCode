using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<PasswordResetToken?> GetValidByUserIdAsync(Guid userId);
    Task UpdateAsync(PasswordResetToken token);
}
