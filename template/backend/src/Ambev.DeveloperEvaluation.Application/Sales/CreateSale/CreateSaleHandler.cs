using AutoMapper;
using FluentValidation;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Handler for processing CreateSaleCommand requests.
/// </summary>
public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleSnapshotRepository _snapshotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        ISaleSnapshotRepository snapshotRepository,
        IEventPublisher eventPublisher,
        IMapper mapper)
    {
        _saleRepository = saleRepository;
        _snapshotRepository = snapshotRepository;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        // 1. Validation
        var validator = new CreateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Map Command to Entity & Apply Domain Rules
        var sale = new Sale
        {
            SaleDate = command.SaleDate,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName
        };

        foreach (var item in command.Items)
        {
            // AddItem encapsulates the business logic (quantities, discounts)
            sale.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        // 3. Persist to Relational DB
        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);

        // 4. Save Immutable Snapshot to NoSQL
        var snapshot = SaleSnapshot.FromSale(createdSale);
        await _snapshotRepository.SaveSnapshotAsync(snapshot, cancellationToken);

        // 5. Publish Domain Event
        var saleCreatedEvent = new SaleCreatedEvent(createdSale);
        await _eventPublisher.PublishAsync(saleCreatedEvent, cancellationToken);

        // 6. Return Result
        return _mapper.Map<CreateSaleResult>(createdSale);
    }
}
