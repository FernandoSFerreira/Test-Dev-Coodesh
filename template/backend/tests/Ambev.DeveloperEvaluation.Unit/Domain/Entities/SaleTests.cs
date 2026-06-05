using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Given an item with quantity 3, When adding, Then no discount is applied")]
    public void Given_Quantity3_When_AddingItem_Then_NoDiscount()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        var unitPrice = 10m;
        var quantity = 3;

        // Act
        sale.AddItem(Guid.NewGuid(), "Product A", quantity, unitPrice);

        // Assert
        var item = sale.Items.First();
        Assert.Equal(0, item.Discount);
        Assert.Equal(30m, item.TotalAmount); // 3 * 10
    }

    [Fact(DisplayName = "Given an item with quantity 5, When adding, Then 10% discount is applied")]
    public void Given_Quantity5_When_AddingItem_Then_10PercentDiscount()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        var unitPrice = 10m;
        var quantity = 5;

        // Act
        sale.AddItem(Guid.NewGuid(), "Product A", quantity, unitPrice);

        // Assert
        var item = sale.Items.First();
        Assert.Equal(0.10m, item.Discount);
        Assert.Equal(45m, item.TotalAmount); // (5 * 10) * 0.9
    }

    [Fact(DisplayName = "Given an item with quantity 15, When adding, Then 20% discount is applied")]
    public void Given_Quantity15_When_AddingItem_Then_20PercentDiscount()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        var unitPrice = 10m;
        var quantity = 15;

        // Act
        sale.AddItem(Guid.NewGuid(), "Product A", quantity, unitPrice);

        // Assert
        var item = sale.Items.First();
        Assert.Equal(0.20m, item.Discount);
        Assert.Equal(120m, item.TotalAmount); // (15 * 10) * 0.8
    }

    [Fact(DisplayName = "Given an item with quantity 21, When adding, Then throws InvalidOperationException")]
    public void Given_Quantity21_When_AddingItem_Then_ThrowsException()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        
        // Act & Assert
        var ex = Assert.Throws<Ambev.DeveloperEvaluation.Domain.Exceptions.BusinessRuleException>(() => 
            sale.AddItem(Guid.NewGuid(), "Product A", 21, 10m));
        
        Assert.Contains("Cannot sell more than 20", ex.Message);
    }

    [Fact(DisplayName = "Given an existing sale, When cancelled, Then IsCancelled is true")]
    public void Given_Sale_When_Cancelled_Then_IsCancelledTrue()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        
        // Act
        sale.Cancel();

        // Assert
        Assert.True(sale.IsCancelled);
    }
}
