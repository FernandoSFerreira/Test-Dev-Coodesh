namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

public class ListSalesRequest
{
    public int _page { get; set; } = 1;
    public int _size { get; set; } = 10;
    public string? _order { get; set; }
}
