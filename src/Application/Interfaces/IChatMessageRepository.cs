using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id);
    Task<IEnumerable<ChatMessage>> GetByRoomIdAsync(Guid roomId);
    Task<IEnumerable<ChatMessage>> GetRecentByRoomIdAsync(Guid roomId, int count);
    Task<ChatMessage> AddAsync(ChatMessage message);
    Task<int> GetTotalMessagesCountAsync();
}
