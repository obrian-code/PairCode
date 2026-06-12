using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PairCode.Application.DTOs;
using PairCode.Application.Services;

namespace PairCode.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", userName);
        _logger.LogInformation("User {User} joined room {Room}", userName, roomId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
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
