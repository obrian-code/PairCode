using FluentValidation;
using PairCode.Application.DTOs;

namespace PairCode.Application.Validators;

public class CreateRoomValidator : AbstractValidator<CreateRoomDto>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MaximumLength(100).WithMessage("Room name must not exceed 100 characters");
    }
}
