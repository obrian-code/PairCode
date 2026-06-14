using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _context;

    public PasswordResetTokenRepository(AppDbContext context) => _context = context;

    public Task AddAsync(PasswordResetToken token) { _context.Set<PasswordResetToken>().Add(token); return Task.CompletedTask; }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token) =>
        await _context.Set<PasswordResetToken>().Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    public async Task<PasswordResetToken?> GetValidByUserIdAsync(Guid userId) =>
        await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.ExpiresAt)
            .FirstOrDefaultAsync();

    public Task UpdateAsync(PasswordResetToken token) { _context.Set<PasswordResetToken>().Update(token); return Task.CompletedTask; }
}
