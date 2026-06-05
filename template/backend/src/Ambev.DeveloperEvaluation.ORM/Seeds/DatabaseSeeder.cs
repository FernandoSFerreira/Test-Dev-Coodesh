using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Common.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ambev.DeveloperEvaluation.ORM.Seeds;

/// <summary>
/// Handles automatic database seeding with initial demo data.
/// Operations are idempotent (safe to run multiple times).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        DefaultContext context,
        ISaleSnapshotRepository snapshotRepository,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        await SeedUsersAsync(context, passwordHasher, logger);
        await SeedSalesAsync(context, snapshotRepository, logger);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(DefaultContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Email == "admin@ambev.com"))
        {
            logger.Information("[Seed] Admin user already exists. Skipping user seed.");
            return;
        }

        logger.Information("[Seed] Inserting demo users...");

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@ambev.com",
            Password = passwordHasher.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            Phone = "11999999999",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
    }

    private static async Task SeedSalesAsync(DefaultContext context, ISaleSnapshotRepository snapshotRepository, ILogger logger)
    {
        if (await context.Sales.AnyAsync())
        {
            logger.Information("[Seed] Sales already exist. Skipping sales seed.");
            return;
        }

        logger.Information("[Seed] Inserting demo sales...");

        // Sale 1: No discount (3 items)
        var sale1 = new Sale
        {
            Id = Guid.NewGuid(),
            SaleDate = DateTime.UtcNow.AddDays(-2),
            CustomerId = Guid.NewGuid(),
            CustomerName = "João Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Centro SP"
        };
        sale1.AddItem(Guid.NewGuid(), "Skol Pilsen 350ml", 3, 3.50m);
        await context.Sales.AddAsync(sale1);
        await snapshotRepository.SaveSnapshotAsync(SaleSnapshot.FromSale(sale1));

        // Sale 2: 10% discount (5 items)
        var sale2 = new Sale
        {
            Id = Guid.NewGuid(),
            SaleDate = DateTime.UtcNow.AddDays(-1),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Santos",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Zona Sul RJ"
        };
        sale2.AddItem(Guid.NewGuid(), "Brahma Chopp 350ml", 5, 3.80m);
        await context.Sales.AddAsync(sale2);
        await snapshotRepository.SaveSnapshotAsync(SaleSnapshot.FromSale(sale2));

        // Sale 3: 20% discount (12 items)
        var sale3 = new Sale
        {
            Id = Guid.NewGuid(),
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Carlos Oliveira",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Norte BH"
        };
        sale3.AddItem(Guid.NewGuid(), "Stella Artois 275ml", 12, 5.50m);
        await context.Sales.AddAsync(sale3);
        await snapshotRepository.SaveSnapshotAsync(SaleSnapshot.FromSale(sale3));
    }
}
