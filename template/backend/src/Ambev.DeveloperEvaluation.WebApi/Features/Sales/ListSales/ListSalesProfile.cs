using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<ListSalesRequest, ListSalesQuery>()
            .ForMember(d => d.Page, o => o.MapFrom(s => s._page))
            .ForMember(d => d.Size, o => o.MapFrom(s => s._size))
            .ForMember(d => d.OrderBy, o => o.MapFrom(s => s._order));
            
        CreateMap<ListSalesResult, ListSalesResponse>();
    }
}
