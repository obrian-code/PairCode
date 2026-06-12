using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PairCode.Application.Services;
using PairCode.Web.Services;

namespace PairCode.Web.Hubs;

[Authorize]
public class DocumentHub : Hub
{
    private readonly SharedDocumentService _documentService;
    private readonly ConnectionTracker _connectionTracker;
    private readonly ILogger<DocumentHub> _logger;

    public DocumentHub(
        SharedDocumentService documentService,
        ConnectionTracker connectionTracker,
        ILogger<DocumentHub> logger)
    {
        _documentService = documentService;
        _connectionTracker = connectionTracker;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier!;
        _connectionTracker.AddConnection(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var rooms = _connectionTracker.GetUserRooms(Context.ConnectionId);
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        foreach (var roomId in rooms)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            _logger.LogInformation("User {User} disconnected from document room {Room}", userName, roomId);
        }

        _connectionTracker.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinDocument(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _connectionTracker.JoinRoom(Context.ConnectionId, roomId);
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
