using FluentValidation;
using PairCode.Application.DTOs;

namespace PairCode.Application.Validators;

public class JoinRoomValidator : AbstractValidator<JoinRoomDto>
{
    public JoinRoomValidator()
    {
        RuleFor(x => x.AccessCode)
            .NotEmpty().WithMessage("Access code is required")
            .Length(6).WithMessage("Access code must be 6 characters");
    }
}
