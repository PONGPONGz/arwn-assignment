using ClinicPos.Api.Dtos;
using FluentValidation;

namespace ClinicPos.Api.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.Role)
            .InclusiveBetween(0, 2).WithMessage("Role must be 0 (Admin), 1 (User), or 2 (Viewer)");
    }
}
