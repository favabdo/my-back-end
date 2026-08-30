using FluentValidation;

namespace NileTechno.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.IdToken) || !string.IsNullOrWhiteSpace(x.AccessToken))
            .WithMessage("لازم تسجّل دخولك على حساب جوجل الأول.");
    }
}
