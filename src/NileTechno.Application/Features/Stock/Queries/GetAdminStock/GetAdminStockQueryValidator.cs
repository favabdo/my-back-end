using FluentValidation;

namespace NileTechno.Application.Features.Stock.Queries.GetAdminStock;

public class GetAdminStockQueryValidator : AbstractValidator<GetAdminStockQuery>
{
    public GetAdminStockQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("رقم الصفحة لازم يكون أكبر من صفر.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("حجم الصفحة بين 1 و 200.");
    }
}
