using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.NoSql.Configuration;
using Ambev.DeveloperEvaluation.NoSql.Context;
using Ambev.DeveloperEvaluation.NoSql.Repositories;
using Ambev.DeveloperEvaluation.MessageBroker;
using Ambev.DeveloperEvaluation.MessageBroker.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        
        // PostgreSQL
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();

        // MongoDB
        builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("ConnectionStrings"));
        builder.Services.AddSingleton<MongoDbContext>();
        builder.Services.AddScoped<ISaleSnapshotRepository, SaleSnapshotRepository>();

        // RabbitMQ
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<RabbitMqConnectionFactory>();
        builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
    }
}