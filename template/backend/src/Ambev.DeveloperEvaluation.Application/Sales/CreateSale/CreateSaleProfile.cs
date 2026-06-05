using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// AutoMapper profile for mapping between CreateSaleCommand, Sale entity, and CreateSaleResult.
/// </summary>
public class CreateSaleProfile : Profile
{
    public CreateSaleProfile()
    {
        // Command to Entity mapping is handled manually in the Handler 
        // to properly apply the business rules (AddItems)
        
        CreateMap<Sale, CreateSaleResult>();
    }
}
