using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

public class ListSalesRequestValidator : AbstractValidator<ListSalesRequest>
{
    public ListSalesRequestValidator()
    {
        RuleFor(x => x._page).GreaterThanOrEqualTo(1);
        RuleFor(x => x._size).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}
