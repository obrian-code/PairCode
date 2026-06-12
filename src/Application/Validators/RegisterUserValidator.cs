using FluentValidation;
using PairCode.Application.DTOs;

namespace PairCode.Application.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one digit")
            .Must(p => p.Any(c => !char.IsLetterOrDigit(c))).WithMessage("Password must contain at least one special character");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(r => r is "Admin" or "Interviewer" or "Candidate")
            .WithMessage("Role must be Admin, Interviewer, or Candidate");
    }
}
