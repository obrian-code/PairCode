using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _context;

    public EmailVerificationTokenRepository(AppDbContext context) => _context = context;

    public Task AddAsync(EmailVerificationToken token) { _context.Set<EmailVerificationToken>().Add(token); return Task.CompletedTask; }

    public async Task<EmailVerificationToken?> GetByTokenAsync(string token) =>
        await _context.Set<EmailVerificationToken>().Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    public async Task<EmailVerificationToken?> GetValidByUserIdAsync(Guid userId) =>
        await _context.Set<EmailVerificationToken>()
            .Where(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.ExpiresAt)
            .FirstOrDefaultAsync();

    public Task UpdateAsync(EmailVerificationToken token) { _context.Set<EmailVerificationToken>().Update(token); return Task.CompletedTask; }
}
