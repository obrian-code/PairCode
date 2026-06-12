using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly AppDbContext _context;

    public ChatMessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id) =>
        await _context.ChatMessages.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<ChatMessage>> GetByRoomIdAsync(Guid roomId) =>
        await _context.ChatMessages.Include(m => m.User)
            .Where(m => m.RoomId == roomId).OrderBy(m => m.SentAt).ToListAsync();

    public async Task<IEnumerable<ChatMessage>> GetRecentByRoomIdAsync(Guid roomId, int count) =>
        await _context.ChatMessages.Include(m => m.User)
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .ToListAsync();

    public async Task<ChatMessage> AddAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<int> GetTotalMessagesCountAsync() =>
        await _context.ChatMessages.CountAsync();
}
