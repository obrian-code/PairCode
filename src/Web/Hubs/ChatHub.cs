using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PairCode.Application.DTOs;
using PairCode.Application.Services;
using PairCode.Web.Services;

namespace PairCode.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly ConnectionTracker _connectionTracker;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        ChatService chatService,
        ConnectionTracker connectionTracker,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
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
            await Clients.OthersInGroup(roomId).SendAsync("UserLeft", userName);
            _logger.LogInformation("User {User} disconnected from room {Room}", userName, roomId);
        }

        _connectionTracker.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _connectionTracker.JoinRoom(Context.ConnectionId, roomId);
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", userName);
        _logger.LogInformation("User {User} joined room {Room}", userName, roomId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        _connectionTracker.LeaveRoom(Context.ConnectionId, roomId);
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(roomId).SendAsync("UserLeft", userName);
        _logger.LogInformation("User {User} left room {Room}", userName, roomId);
    }

    public async Task SendMessage(string roomId, string message)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        var dto = await _chatService.SendMessageAsync(Guid.Parse(roomId), userId, message);
        await Clients.Group(roomId).SendAsync("NewMessage", dto);
        _logger.LogInformation("Message sent by {User} in room {Room}", userName, roomId);
    }

    public async Task NotifyTyping(string roomId)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        var userId = Context.UserIdentifier!;
        await Clients.OthersInGroup(roomId).SendAsync("UserTyping", userName, userId);
    }

    public async Task NotifyStoppedTyping(string roomId)
    {
        var userId = Context.UserIdentifier!;
        await Clients.OthersInGroup(roomId).SendAsync("UserStoppedTyping", userId);
    }
}
