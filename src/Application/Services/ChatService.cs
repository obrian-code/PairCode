using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;

namespace PairCode.Application.Services;

public class ChatService
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;

    public ChatService(IChatMessageRepository messageRepository, IUserRepository userRepository)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid roomId, Guid userId, string message)
    {
        var chatMessage = new ChatMessage(roomId, userId, message);
        await _messageRepository.AddAsync(chatMessage);

        var user = chatMessage.User ?? await _userRepository.GetByIdAsync(userId);

        return new ChatMessageDto(
            chatMessage.Id,
            chatMessage.UserId,
            user != null
                ? new UserInfoDto(user.Id, user.Name, user.Email, user.Role.ToString())
                : new UserInfoDto(userId, "Unknown", "", ""),
            chatMessage.Message,
            chatMessage.SentAt
        );
    }

    public async Task<IEnumerable<ChatMessageDto>> GetRoomMessagesAsync(Guid roomId)
    {
        var messages = await _messageRepository.GetByRoomIdAsync(roomId);
        return messages.Select(msg => new ChatMessageDto(
            msg.Id,
            msg.UserId,
            msg.User != null
                ? new UserInfoDto(msg.User.Id, msg.User.Name, msg.User.Email, msg.User.Role.ToString())
                : new UserInfoDto(msg.UserId, "Unknown", "", ""),
            msg.Message,
            msg.SentAt
        ));
    }

    public async Task<IEnumerable<ChatMessageDto>> GetRecentMessagesAsync(Guid roomId, int count = 50)
    {
        var messages = await _messageRepository.GetRecentByRoomIdAsync(roomId, count);
        return messages.Select(msg => new ChatMessageDto(
            msg.Id,
            msg.UserId,
            msg.User != null
                ? new UserInfoDto(msg.User.Id, msg.User.Name, msg.User.Email, msg.User.Role.ToString())
                : new UserInfoDto(msg.UserId, "Unknown", "", ""),
            msg.Message,
            msg.SentAt
        ));
    }
}
