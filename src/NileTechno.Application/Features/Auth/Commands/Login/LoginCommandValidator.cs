using FluentValidation;

namespace NileTechno.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}
