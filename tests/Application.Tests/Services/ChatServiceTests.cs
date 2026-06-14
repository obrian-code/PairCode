using FluentAssertions;
using Moq;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Domain.Entities;

namespace PairCode.Application.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<IChatMessageRepository> _messageRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _sut = new ChatService(_messageRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnDto_WhenUserExists()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User("TestUser", "test@test.com", "hash", Domain.Enums.UserRole.Candidate);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _messageRepo.Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
            .ReturnsAsync((ChatMessage msg) => msg);

        var result = await _sut.SendMessageAsync(roomId, userId, "Hello!");

        result.Should().NotBeNull();
        result.Message.Should().Be("Hello!");
        result.User.Name.Should().Be("TestUser");
        _messageRepo.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetRecentMessagesAsync_ShouldReturnMessages()
    {
        var roomId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(roomId, Guid.NewGuid(), "First"),
            new(roomId, Guid.NewGuid(), "Second")
        };

        _messageRepo.Setup(x => x.GetRecentByRoomIdAsync(roomId, 10)).ReturnsAsync(messages);

        var result = await _sut.GetRecentMessagesAsync(roomId, 10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRoomMessagesAsync_ShouldReturnEmpty_WhenNoMessages()
    {
        _messageRepo.Setup(x => x.GetByRoomIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ChatMessage>());

        var result = await _sut.GetRoomMessagesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }
}
