using FluentAssertions;
using Moq;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Domain.Entities;

namespace PairCode.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo.Object, _tokenService.Object, _auditService.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("existing@test.com"))
            .ReturnsAsync(new User("Existing", "existing@test.com", "hash", Domain.Enums.UserRole.Candidate));

        var act = () => _sut.RegisterAsync(new RegisterUserDto("Test", "existing@test.com", "Password1!", "Candidate"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration failed");
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenEmailIsNew()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("new@test.com"))
            .ReturnsAsync((User?)null);

        _tokenService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-token");

        var result = await _sut.RegisterAsync(new RegisterUserDto("Test", "new@test.com", "Password1!", "Candidate"));

        result.Should().NotBeNull();
        result.Email.Should().Be("new@test.com");
        result.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenCredentialsAreInvalid()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("wrong@test.com"))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginDto("wrong@test.com", "wrong"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }
}
