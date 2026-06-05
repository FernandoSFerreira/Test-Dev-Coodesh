using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleSnapshotRepository _snapshotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _snapshotRepository = Substitute.For<ISaleSnapshotRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        
        // Basic mapper setup
        var mapperConfig = new MapperConfiguration(cfg => 
        {
            cfg.CreateMap<Sale, CreateSaleResult>();
        });
        _mapper = mapperConfig.CreateMapper();

        _handler = new CreateSaleHandler(
            _saleRepository, 
            _snapshotRepository, 
            _eventPublisher, 
            _mapper);
    }

    [Fact(DisplayName = "Given valid command, When handled, Then saves to db, saves snapshot and publishes event")]
    public async Task Given_ValidCommand_When_Handled_Then_Success()
    {
        // Arrange
        var command = new CreateSaleCommand
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "John Doe",
            BranchId = Guid.NewGuid(),
            BranchName = "Branch 1",
            Items = new List<CreateSaleItemCommand>
            {
                new() { ProductId = Guid.NewGuid(), ProductName = "Beer", Quantity = 5, UnitPrice = 10m }
            }
        };

        var createdSaleId = Guid.NewGuid();
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var sale = callInfo.Arg<Sale>();
                sale.Id = createdSaleId;
                return sale;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdSaleId, result.Id);

        // Verify Postgres save
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        
        // Verify MongoDB snapshot save
        await _snapshotRepository.Received(1).SaveSnapshotAsync(Arg.Any<SaleSnapshot>(), Arg.Any<CancellationToken>());
        
        // Verify RabbitMQ Event Publish
        await _eventPublisher.Received(1).PublishAsync(Arg.Any<SaleCreatedEvent>(), Arg.Any<CancellationToken>());
    }
}
