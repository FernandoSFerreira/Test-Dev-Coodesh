using AutoMapper;
using FluentValidation;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleSnapshotRepository _snapshotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public UpdateSaleHandler(
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

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        if (sale.IsCancelled)
            throw new InvalidOperationException("Cannot update a cancelled sale.");

        // Update properties
        sale.CustomerId = command.CustomerId;
        sale.CustomerName = command.CustomerName;
        sale.BranchId = command.BranchId;
        sale.BranchName = command.BranchName;
        sale.UpdatedAt = DateTime.UtcNow;

        // Clear existing items and re-add to apply business rules again
        sale.Items.Clear();
        foreach (var item in command.Items)
        {
            sale.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        var snapshot = SaleSnapshot.FromSale(updatedSale);
        await _snapshotRepository.SaveSnapshotAsync(snapshot, cancellationToken);

        var saleModifiedEvent = new SaleModifiedEvent(updatedSale);
        await _eventPublisher.PublishAsync(saleModifiedEvent, cancellationToken);

        return _mapper.Map<UpdateSaleResult>(updatedSale);
    }
}
