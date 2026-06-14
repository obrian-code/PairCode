using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context) => _context = context;

    public Task AddAsync(RefreshToken token) { _context.Set<RefreshToken>().Add(token); return Task.CompletedTask; }

    public async Task<RefreshToken?> GetByTokenAsync(string token) =>
        await _context.Set<RefreshToken>().Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    public Task UpdateAsync(RefreshToken token) { _context.Set<RefreshToken>().Update(token); return Task.CompletedTask; }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var active = await _context.Set<RefreshToken>().Where(t => t.UserId == userId && !t.Revoked).ToListAsync();
        foreach (var t in active) t.Revoke();
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId) =>
        await _context.Set<RefreshToken>().Where(t => t.UserId == userId && !t.Revoked && t.ExpiresAt > DateTime.UtcNow).ToListAsync();
}
