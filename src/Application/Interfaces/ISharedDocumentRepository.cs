using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface ISharedDocumentRepository
{
    Task<SharedDocument?> GetByRoomIdAsync(Guid roomId);
    Task<SharedDocument> AddAsync(SharedDocument document);
    Task UpdateAsync(SharedDocument document);
}
