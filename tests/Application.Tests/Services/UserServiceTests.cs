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
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailVerificationTokenRepository> _emailVerificationRepo = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("test-refresh-token");
        _sut = new UserService(_userRepo.Object, _tokenService.Object, _auditService.Object,
            _unitOfWork.Object, _passwordResetRepo.Object, _refreshTokenRepo.Object, _emailService.Object, _emailVerificationRepo.Object);
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

    [Fact]
    public async Task ForgotPasswordAsync_ShouldCreateToken_WhenUserExists()
    {
        var user = new User("Test", "test@test.com", "hash", Domain.Enums.UserRole.Candidate);
        _userRepo.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _passwordResetRepo.Setup(x => x.GetValidByUserIdAsync(user.Id)).ReturnsAsync((PasswordResetToken?)null);

        await _sut.ForgotPasswordAsync(new ForgotPasswordDto("test@test.com"));

        _passwordResetRepo.Verify(x => x.AddAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        _emailService.Verify(x => x.SendPasswordResetEmailAsync("test@test.com", "Test", It.IsAny<string>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldNotCreateToken_WhenUserNotFound()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("nonexistent@test.com")).ReturnsAsync((User?)null);

        await _sut.ForgotPasswordAsync(new ForgotPasswordDto("nonexistent@test.com"));

        _passwordResetRepo.Verify(x => x.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenTokenInvalid()
    {
        _passwordResetRepo.Setup(x => x.GetByTokenAsync("invalid-token")).ReturnsAsync((PasswordResetToken?)null);

        var act = () => _sut.ResetPasswordAsync(new ResetPasswordDto("test@test.com", "invalid-token", "NewPass1!"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid or expired reset token");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokenAndRefreshToken_WhenCredentialsValid()
    {
        var user = new User("Test", "test@test.com", BCrypt.Net.BCrypt.HashPassword("Password1!"), Domain.Enums.UserRole.Candidate);
        _userRepo.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _tokenService.Setup(x => x.GenerateToken(user)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginDto("test@test.com", "Password1!"));

        result.Should().NotBeNull();
        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBeNull();
        _refreshTokenRepo.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }
}
