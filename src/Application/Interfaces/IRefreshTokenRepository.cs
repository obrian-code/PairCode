using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
}
