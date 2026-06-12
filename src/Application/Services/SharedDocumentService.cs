using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;

namespace PairCode.Application.Services;

public class SharedDocumentService
{
    private readonly ISharedDocumentRepository _documentRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public SharedDocumentService(ISharedDocumentRepository documentRepository, IAuditService auditService, IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SharedDocumentDto> GetDocumentAsync(Guid roomId)
    {
        var doc = await _documentRepository.GetByRoomIdAsync(roomId);
        if (doc == null)
        {
            doc = new SharedDocument(roomId);
            await _documentRepository.AddAsync(doc);
            await _unitOfWork.SaveChangesAsync();
        }

        return new SharedDocumentDto(doc.Content, doc.Version, doc.UpdatedAt);
    }

    public async Task<SharedDocumentDto> UpdateDocumentAsync(Guid roomId, string content, Guid userId)
    {
        var doc = await _documentRepository.GetByRoomIdAsync(roomId);
        if (doc == null)
        {
            doc = new SharedDocument(roomId);
            await _documentRepository.AddAsync(doc);
        }

        doc.UpdateContent(content);
        await _documentRepository.UpdateAsync(doc);
        await _auditService.LogAsync(userId, "UpdateDocument", nameof(SharedDocument), doc.Id.ToString(), $"Document updated to version {doc.Version}");
        await _unitOfWork.SaveChangesAsync();

        return new SharedDocumentDto(doc.Content, doc.Version, doc.UpdatedAt);
    }
}
