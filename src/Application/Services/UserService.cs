using System.Security.Claims;
using System.Security.Cryptography;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Domain.Enums;

namespace PairCode.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordResetTokenRepository _passwordResetRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IEmailService _emailService;
    private readonly IEmailVerificationTokenRepository _emailVerificationRepo;

    public UserService(
        IUserRepository userRepository, ITokenService tokenService,
        IAuditService auditService, IUnitOfWork unitOfWork,
        IPasswordResetTokenRepository passwordResetRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IEmailService emailService,
        IEmailVerificationTokenRepository emailVerificationRepo)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _passwordResetRepo = passwordResetRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _emailService = emailService;
        _emailVerificationRepo = emailVerificationRepo;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Registration failed");

        var role = dto.Role switch
        {
            "Admin" => UserRole.Admin,
            "Interviewer" => UserRole.Interviewer,
            "Candidate" => UserRole.Candidate,
            _ => throw new InvalidOperationException("Invalid role")
        };

        var passwordHash = BCryptPasswordHash(dto.Password);
        var user = new User(dto.Name, dto.Email, passwordHash, role);

        await _userRepository.AddAsync(user);
        await _auditService.LogAsync(user.Id, "Register", nameof(User), user.Id.ToString(), $"User {user.Name} registered");
        await _unitOfWork.SaveChangesAsync();

        await SendVerificationEmailAsync(user);

        var token = _tokenService.GenerateToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return new AuthResponseDto(token, user.Name, user.Email, user.Role.ToString(), user.Id, refreshToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !VerifyBCryptPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = _tokenService.GenerateToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return new AuthResponseDto(token, user.Name, user.Email, user.Role.ToString(), user.Id, refreshToken);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        user.UpdateProfile(dto.Name);
        await _userRepository.UpdateAsync(user);
        await _auditService.LogAsync(userId, "UpdateProfile", nameof(User), userId.ToString(), "Profile updated");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (!VerifyBCryptPassword(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        ValidatePassword(dto.NewPassword);

        user.UpdatePasswordHash(BCryptPasswordHash(dto.NewPassword));
        await _userRepository.UpdateAsync(user);
        await _auditService.LogAsync(userId, "ChangePassword", nameof(User), userId.ToString(), "Password changed");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            return;

        var existingToken = await _passwordResetRepo.GetValidByUserIdAsync(user.Id);
        if (existingToken != null)
            return;

        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var tokenStr = Convert.ToHexString(tokenBytes);

        var resetToken = new PasswordResetToken(user.Id, tokenStr, DateTime.UtcNow.AddHours(1));
        await _passwordResetRepo.AddAsync(resetToken);
        await _unitOfWork.SaveChangesAsync();

        var resetLink = $"https://localhost:5001/Auth/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(tokenStr)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var resetToken = await _passwordResetRepo.GetByTokenAsync(dto.Token);
        if (resetToken == null || !resetToken.IsValid())
            throw new InvalidOperationException("Invalid or expired reset token");

        if (!string.Equals(resetToken.User.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid or expired reset token");

        ValidatePassword(dto.NewPassword);

        var user = resetToken.User;
        user.UpdatePasswordHash(BCryptPasswordHash(dto.NewPassword));
        await _userRepository.UpdateAsync(user);
        resetToken.MarkUsed();
        await _passwordResetRepo.UpdateAsync(resetToken);
        await _auditService.LogAsync(user.Id, "ResetPassword", nameof(User), user.Id.ToString(), "Password reset completed");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(dto.RefreshToken);
        if (principal == null)
            throw new UnauthorizedAccessException("Invalid token");

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        var storedToken = await _refreshTokenRepo.GetByTokenAsync(dto.RefreshToken);
        if (storedToken == null || !storedToken.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        storedToken.Revoke();
        await _refreshTokenRepo.UpdateAsync(storedToken);

        var newJwt = _tokenService.GenerateToken(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();

        return new RefreshTokenResponseDto(newJwt, newRefreshToken);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var verificationToken = await _emailVerificationRepo.GetByTokenAsync(token)
            ?? throw new InvalidOperationException("Invalid verification token");

        if (!verificationToken.IsValid())
            throw new InvalidOperationException("Verification token has expired");

        var user = verificationToken.User;
        user.MarkEmailVerified();
        await _userRepository.UpdateAsync(user);
        verificationToken.MarkUsed();
        await _emailVerificationRepo.UpdateAsync(verificationToken);
        await _auditService.LogAsync(user.Id, "VerifyEmail", nameof(User), user.Id.ToString(), "Email verified");
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task SendVerificationEmailAsync(User user)
    {
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var tokenStr = Convert.ToHexString(tokenBytes);

        var verificationToken = new EmailVerificationToken(user.Id, tokenStr, DateTime.UtcNow.AddDays(7));
        await _emailVerificationRepo.AddAsync(verificationToken);
        await _unitOfWork.SaveChangesAsync();

        var verificationLink = $"https://localhost:5001/Auth/VerifyEmail?token={Uri.EscapeDataString(tokenStr)}";
        await _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationLink);
    }

    public async Task LogoutAsync(Guid userId)
    {
        await _refreshTokenRepo.RevokeAllForUserAsync(userId);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId)
    {
        var tokenStr = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(userId, tokenStr, DateTime.UtcNow.AddDays(7));
        await _refreshTokenRepo.AddAsync(refreshToken);
        return tokenStr;
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters");

        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must contain at least one digit");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Password must contain at least one special character");
    }

    private static string BCryptPasswordHash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    private static bool VerifyBCryptPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
