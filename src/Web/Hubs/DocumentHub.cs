using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PairCode.Application.Services;

namespace PairCode.Web.Hubs;

[Authorize]
public class DocumentHub : Hub
{
    private readonly SharedDocumentService _documentService;
    private readonly ILogger<DocumentHub> _logger;

    public DocumentHub(SharedDocumentService documentService, ILogger<DocumentHub> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public async Task JoinDocument(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("User joined document editing in room {Room}", roomId);
    }

    public async Task SendUpdate(string roomId, string content)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);
        var dto = await _documentService.UpdateDocumentAsync(Guid.Parse(roomId), content, userId);
        await Clients.OthersInGroup(roomId).SendAsync("DocumentUpdated", dto);
    }

    public async Task RequestDocument(string roomId)
    {
        var dto = await _documentService.GetDocumentAsync(Guid.Parse(roomId));
        await Clients.Caller.SendAsync("DocumentLoaded", dto);
    }
}
