using FluentValidation;

namespace NileTechno.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("رابط الاستعادة غير صحيح.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور لازم تكون 6 أحرف على الأقل.");
    }
}
