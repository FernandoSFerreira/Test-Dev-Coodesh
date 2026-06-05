# Plano de Implementação — Sales API (v2)

> [!IMPORTANT]
> Este é um **plano de implementação**. Nenhum arquivo será alterado nesta etapa.

---

## Situação Atual

| Componente | Status |
|---|---|
| **PostgreSQL** | ✅ Configurado no docker-compose e integrado via EF Core (`DefaultContext`) |
| **MongoDB** | ⚠️ Container definido no docker-compose (`mongo:8.0`) mas **sem integração no código** — nenhum driver, connection string ou referência |
| **Redis** | ⚠️ Container definido no docker-compose mas sem integração |
| **RabbitMQ** | ❌ **Não existe** nem no docker-compose nem no código |

---

## Visão Geral da Arquitetura

```mermaid
flowchart LR
    subgraph WebApi
        C[SalesController]
    end
    subgraph Application
        H[Handlers CQRS]
    end
    subgraph Domain
        E[Sale + SaleItem]
        EV[Domain Events]
    end
    subgraph Infra - PostgreSQL
        R[SaleRepository - EF Core]
    end
    subgraph Infra - MongoDB
        M[SaleSnapshotRepository]
    end
    subgraph Infra - RabbitMQ
        P[EventPublisher]
    end

    C --> H
    H --> E
    H --> R
    H --> M
    H --> P
    E --> EV
```

**Fluxo de uma Venda:**
1. `SalesController` recebe a requisição HTTP
2. Handler (MediatR) orquestra o caso de uso
3. Entidade `Sale` aplica regras de negócio e descontos
4. **PostgreSQL**: Persiste os dados normalizados (entidade relacional)
5. **MongoDB**: Salva o **snapshot completo** da compra (documento denormalizado imutável)
6. **RabbitMQ**: Publica o evento de domínio (`SaleCreated`, `SaleModified`, etc.)

---

## Passo 1: Infraestrutura Docker

### 1.1 Adicionar RabbitMQ ao `docker-compose.yml`

```yaml
ambev.developerevaluation.messagebroker:
  container_name: ambev_developer_evaluation_messagebroker
  image: rabbitmq:3-management-alpine
  environment:
    RABBITMQ_DEFAULT_USER: developer
    RABBITMQ_DEFAULT_PASS: ev@luAt10n
  ports:
    - "5672"   # AMQP
    - "15672"  # Management UI
  restart: unless-stopped
```

### 1.2 Adicionar connection strings ao `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n",
    "MongoConnection": "mongodb://developer:ev%40luAt10n@localhost:27017",
    "MongoDatabase": "developer_evaluation"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "developer",
    "Password": "ev@luAt10n",
    "Exchange": "ambev.sales.events",
    "ExchangeType": "topic"
  }
}
```

> [!NOTE]
> A connection string do PostgreSQL também será corrigida para apontar para o container Postgres correto (atualmente aponta para SQL Server).

---

## Passo 2: Domínio (`Ambev.DeveloperEvaluation.Domain`)

### 2.1 Entidades

**`Entities/Sale.cs`**
- `Id` (Guid), `SaleNumber` (string auto-gerada), `SaleDate` (DateTime)
- `CustomerId` (Guid), `CustomerName` (string) — identidade externa denormalizada
- `BranchId` (Guid), `BranchName` (string) — identidade externa denormalizada
- `Items` (List\<SaleItem\>), `TotalAmount` (decimal)
- `IsCancelled` (bool), `CreatedAt`, `UpdatedAt`

**`Entities/SaleItem.cs`**
- `Id` (Guid), `SaleId` (Guid - FK)
- `ProductId` (Guid), `ProductName` (string)
- `Quantity` (int), `UnitPrice` (decimal)
- `Discount` (decimal), `TotalAmount` (decimal)
- `IsCancelled` (bool)

**Métodos de Negócio na Entidade `Sale`:**
- `AddItem(...)` — aplica regras de desconto automaticamente
- `CalculateItemDiscount(quantity)` — retorna % de desconto
- `Cancel()` — marca venda como cancelada
- `CancelItem(itemId)` — cancela item individual e recalcula total

### 2.2 Regras de Desconto (encapsuladas na entidade)

| Quantidade | Desconto | Validação |
|---|---|---|
| < 4 | 0% | — |
| >= 4 e < 10 | 10% | — |
| >= 10 e <= 20 | 20% | — |
| > 20 | ❌ ERRO | `BusinessRuleException` |

### 2.3 Validadores (FluentValidation)

- **`SaleValidator`**: SaleDate obrigatória, Customer/Branch obrigatórios, ao menos 1 item
- **`SaleItemValidator`**: Quantity > 0 e <= 20, UnitPrice > 0, ProductId obrigatório

### 2.4 Eventos de Domínio (`Events/`)

Todos implementando `MediatR.INotification`:

```
Events/SaleCreatedEvent.cs
Events/SaleModifiedEvent.cs
Events/SaleCancelledEvent.cs
Events/SaleItemCancelledEvent.cs
```

### 2.5 Interfaces de Repositório (`Repositories/`)

```csharp
// PostgreSQL - dados relacionais
public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken ct);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Sale> UpdateAsync(Sale sale, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<PaginatedList<Sale>> ListAsync(SaleFilter filter, CancellationToken ct);
}

// MongoDB - snapshots denormalizados
public interface ISaleSnapshotRepository
{
    Task SaveSnapshotAsync(SaleSnapshot snapshot, CancellationToken ct);
    Task<SaleSnapshot?> GetSnapshotAsync(Guid saleId, CancellationToken ct);
}

// RabbitMQ - publicação de eventos
public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken ct) where T : INotification;
}
```

### 2.6 Modelo de Snapshot (`Entities/SaleSnapshot.cs`)

Documento MongoDB denormalizado e **imutável** contendo todos os dados da compra:

```csharp
public class SaleSnapshot
{
    public Guid SaleId { get; set; }
    public string SaleNumber { get; set; }
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; }
    public Guid CustomerId { get; set; }
    public string BranchName { get; set; }
    public Guid BranchId { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime SnapshotCreatedAt { get; set; }
    public List<SaleItemSnapshot> Items { get; set; }
}

public class SaleItemSnapshot
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
}
```

> [!TIP]
> O snapshot é salvo a cada criação/atualização de venda. Mesmo que o preço do produto mude no futuro, o snapshot preserva o valor exato da transação.

---

## Passo 3: ORM — PostgreSQL (`Ambev.DeveloperEvaluation.ORM`)

### 3.1 Mapeamento EF Core

- `Mapping/SaleConfiguration.cs` — mapeamento da tabela `Sales`
- `Mapping/SaleItemConfiguration.cs` — mapeamento da tabela `SaleItems` com FK para `Sales`

### 3.2 DbContext

Adicionar ao `DefaultContext.cs`:
```csharp
public DbSet<Sale> Sales { get; set; }
public DbSet<SaleItem> SaleItems { get; set; }
```

### 3.3 Repositório

- `Repositories/SaleRepository.cs` — implementação de `ISaleRepository` com Include para Items e suporte a filtros/paginação

### 3.4 Migração

```bash
dotnet ef migrations add AddSalesAndSaleItems --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

### 3.5 Aplicação Automática de Migrations no Startup

As migrations serão aplicadas **automaticamente** ao iniciar a aplicação. No `Program.cs`, após `builder.Build()`, será adicionado um bloco que verifica e aplica as migrations pendentes:

```csharp
// Após var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
    await db.Database.MigrateAsync(); // Aplica migrations pendentes automaticamente
}
```

> [!NOTE]
> O `MigrateAsync()` do EF Core é **idempotente** — ele consulta a tabela `__EFMigrationsHistory` e aplica apenas as migrations que ainda não foram executadas. Se todas já estiverem aplicadas, o método retorna sem fazer nada.

---

## Passo 4: Novo Projeto — MongoDB (`Ambev.DeveloperEvaluation.NoSql`)

> [!IMPORTANT]
> Será criado um **novo projeto** na solução, separando a infraestrutura MongoDB do ORM EF Core (que é específico de banco relacional).

### 4.1 Estrutura do Projeto

```
src/Ambev.DeveloperEvaluation.NoSql/
├── Ambev.DeveloperEvaluation.NoSql.csproj   (referencia Domain, NuGet: MongoDB.Driver)
├── Configuration/
│   └── MongoDbSettings.cs                    (POCO para bind do appsettings)
├── Context/
│   └── MongoDbContext.cs                     (wrapper do IMongoDatabase)
└── Repositories/
    └── SaleSnapshotRepository.cs             (implementa ISaleSnapshotRepository)
```

### 4.2 NuGet Packages

- `MongoDB.Driver` (v2.28+)

### 4.3 MongoDbContext

```csharp
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<SaleSnapshot> SaleSnapshots
        => _database.GetCollection<SaleSnapshot>("sale_snapshots");
}
```

---

## Passo 5: Novo Projeto — RabbitMQ (`Ambev.DeveloperEvaluation.MessageBroker`)

> [!IMPORTANT]
> Será criado um **novo projeto** para isolar a infraestrutura de mensageria.

### 5.1 Estrutura do Projeto

```
src/Ambev.DeveloperEvaluation.MessageBroker/
├── Ambev.DeveloperEvaluation.MessageBroker.csproj  (referencia Domain, NuGet: RabbitMQ.Client)
├── Configuration/
│   └── RabbitMqSettings.cs
├── RabbitMqEventPublisher.cs                        (implementa IEventPublisher)
└── RabbitMqConnectionFactory.cs                     (gerencia conexão e canal)
```

### 5.2 NuGet Packages

- `RabbitMQ.Client` (v6.8+)

### 5.3 Publicação de Eventos

```csharp
public class RabbitMqEventPublisher : IEventPublisher
{
    // Publica mensagens JSON no exchange "ambev.sales.events"
    // Routing keys: "sale.created", "sale.modified", "sale.cancelled", "sale.item.cancelled"
    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct) where T : INotification
    {
        // 1. Serializa o evento para JSON
        // 2. Publica no exchange com routing key baseada no tipo do evento
        // 3. Log via Serilog para rastreabilidade
    }
}
```

> [!NOTE]
> Neste momento **não haverá consumidores** (consumers) para essas mensagens. As mensagens ficarão no exchange/queue do RabbitMQ e podem ser verificadas via Management UI (porta 15672).

---

## Passo 6: Application Layer (`Ambev.DeveloperEvaluation.Application`)

### 6.1 Estrutura de Casos de Uso

```
Application/Sales/
├── CreateSale/
│   ├── CreateSaleCommand.cs
│   ├── CreateSaleHandler.cs        → Persiste no PG + Snapshot no Mongo + Publica evento no RabbitMQ
│   ├── CreateSaleResult.cs
│   ├── CreateSaleValidator.cs
│   └── CreateSaleProfile.cs
├── GetSale/
│   ├── GetSaleQuery.cs
│   ├── GetSaleHandler.cs
│   ├── GetSaleResult.cs
│   └── GetSaleProfile.cs
├── UpdateSale/
│   ├── UpdateSaleCommand.cs
│   ├── UpdateSaleHandler.cs        → Atualiza PG + Novo Snapshot no Mongo + Publica evento
│   ├── UpdateSaleResult.cs
│   ├── UpdateSaleValidator.cs
│   └── UpdateSaleProfile.cs
├── DeleteSale/
│   ├── DeleteSaleCommand.cs
│   ├── DeleteSaleHandler.cs        → Cancela no PG + Publica evento SaleCancelled
│   └── DeleteSaleValidator.cs
└── ListSales/
    ├── ListSalesQuery.cs           (com filtros e paginação)
    ├── ListSalesHandler.cs
    ├── ListSalesResult.cs
    └── ListSalesProfile.cs
```

### 6.2 Fluxo do `CreateSaleHandler`

```mermaid
sequenceDiagram
    participant C as Controller
    participant H as CreateSaleHandler
    participant D as Sale (Domain)
    participant PG as PostgreSQL
    participant MG as MongoDB
    participant RQ as RabbitMQ

    C->>H: CreateSaleCommand
    H->>D: new Sale() + AddItems()
    Note over D: Aplica regras de desconto
    H->>PG: saleRepository.CreateAsync()
    H->>MG: snapshotRepository.SaveSnapshotAsync()
    H->>RQ: eventPublisher.PublishAsync(SaleCreatedEvent)
    H->>C: CreateSaleResult
```

---

## Passo 7: WebApi + IoC

### 7.1 SalesController

```
WebApi/Features/Sales/
├── SalesController.cs
├── CreateSaleRequest.cs
├── UpdateSaleRequest.cs
└── ListSalesRequest.cs (filtros e paginação)
```

**Endpoints:**

| Método | Rota | Ação |
|---|---|---|
| `POST` | `/api/sales` | Criar venda |
| `GET` | `/api/sales/{id}` | Obter venda por ID |
| `PUT` | `/api/sales/{id}` | Atualizar venda |
| `DELETE` | `/api/sales/{id}` | Cancelar venda |
| `GET` | `/api/sales` | Listar com paginação e filtros |

### 7.2 Injeção de Dependência

Adicionar ao `InfrastructureModuleInitializer.cs`:

```csharp
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
```

### 7.3 Solution (.sln)

Adicionar os dois novos projetos à solução:
```bash
dotnet sln add src/Ambev.DeveloperEvaluation.NoSql
dotnet sln add src/Ambev.DeveloperEvaluation.MessageBroker
```

---

## Passo 8: Seeds e Inicialização Automática

> [!IMPORTANT]
> Os seeds serão executados **automaticamente no startup** da aplicação, logo após a aplicação das migrations. O processo é **idempotente**: verifica se os dados já existem antes de inserir.

### 8.1 Classe de Seed (`Ambev.DeveloperEvaluation.ORM`)

Criar `Seeds/DatabaseSeeder.cs`:

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedAsync(DefaultContext context, ILogger logger)
    {
        await SeedUsersAsync(context, logger);
        await SeedSalesAsync(context, logger);
        await context.SaveChangesAsync();
    }
}
```

### 8.2 Dados de Seed

Os seeds criarão dados de demonstração que permitam utilizar o sistema imediatamente ao subir:

**Usuários (se não existirem):**

| Campo | Valor |
|---|---|
| Username | `admin` |
| Email | `admin@ambev.com` |
| Role | `Admin` |
| Status | `Active` |

**Vendas de demonstração (se não existirem):**

| Venda | Cliente | Filial | Itens |
|---|---|---|---|
| SALE-001 | João Silva (cliente externo) | Filial Centro SP | 3 itens (sem desconto) |
| SALE-002 | Maria Santos (cliente externo) | Filial Zona Sul RJ | 5 itens (desconto 10%) |
| SALE-003 | Carlos Oliveira (cliente externo) | Filial Norte BH | 12 itens (desconto 20%) |

> [!TIP]
> Os seeds de vendas também geram os **snapshots correspondentes no MongoDB**, garantindo que ao subir o sistema os dados estejam consistentes entre PostgreSQL e MongoDB.

### 8.3 Verificação de Idempotência

Cada método de seed verifica se o registro já existe antes de inserir:

```csharp
private static async Task SeedUsersAsync(DefaultContext context, ILogger logger)
{
    if (await context.Users.AnyAsync(u => u.Username == "admin"))
    {
        logger.Information("[Seed] Usuários já existem. Pulando seed de usuários.");
        return;
    }

    logger.Information("[Seed] Inserindo usuários de demonstração...");
    // ... inserção dos dados
}

private static async Task SeedSalesAsync(DefaultContext context, ILogger logger)
{
    if (await context.Sales.AnyAsync())
    {
        logger.Information("[Seed] Vendas já existem. Pulando seed de vendas.");
        return;
    }

    logger.Information("[Seed] Inserindo vendas de demonstração...");
    // ... inserção dos dados com regras de desconto aplicadas
}
```

### 8.4 Integração no Startup (`Program.cs`)

O fluxo completo de inicialização ficará assim:

```csharp
var app = builder.Build();

// === Inicialização automática do banco ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // 1. Aplica migrations pendentes (PostgreSQL)
    var dbContext = services.GetRequiredService<DefaultContext>();
    logger.Information("[Startup] Verificando migrations pendentes...");
    await dbContext.Database.MigrateAsync();
    logger.Information("[Startup] Migrations aplicadas com sucesso.");

    // 2. Executa seeds idempotentes (PostgreSQL + MongoDB snapshots)
    logger.Information("[Startup] Verificando seeds...");
    var mongoSnapshotRepo = services.GetRequiredService<ISaleSnapshotRepository>();
    await DatabaseSeeder.SeedAsync(dbContext, mongoSnapshotRepo, logger);
    logger.Information("[Startup] Seeds verificados/aplicados com sucesso.");
}
// === Fim da inicialização automática ===

app.UseMiddleware<ValidationExceptionMiddleware>();
// ... restante do pipeline
```

### 8.5 Fluxo de Inicialização

```mermaid
flowchart TD
    A["App Startup"] --> B["MigrateAsync()"]
    B --> C{Migrations pendentes?}
    C -- Sim --> D[Aplica migrations]
    C -- Não --> E[Pula]
    D --> E
    E --> F["DatabaseSeeder.SeedAsync()"]
    F --> G{Dados já existem?}
    G -- Sim --> H["Log: Pulando seed"]
    G -- Não --> I[Insere dados de demo]
    I --> J[Gera snapshots no MongoDB]
    J --> K[App pronta para uso]
    H --> K
```

---

## Passo 9: Testes Unitários

### 9.1 Testes de Regras de Negócio

| Cenário | Resultado Esperado |
|---|---|
| Item com qty = 3 | Desconto = 0% |
| Item com qty = 5 | Desconto = 10% |
| Item com qty = 15 | Desconto = 20% |
| Item com qty = 21 | `BusinessRuleException` |
| `Sale.Cancel()` | `IsCancelled = true` |
| `Sale.CancelItem(id)` | Item cancelado, total recalculado |

### 9.2 Testes de Handlers

- `CreateSaleHandler` — verifica persistência PG + snapshot Mongo + evento publicado
- `UpdateSaleHandler` — verifica atualização + novo snapshot + evento `SaleModified`
- `DeleteSaleHandler` — verifica cancelamento + evento `SaleCancelled`
- `GetSaleHandler` / `ListSalesHandler` — verifica retorno correto

### 9.3 Testes de Seed

- Verifica que `DatabaseSeeder.SeedAsync` insere dados quando a base está vazia
- Verifica que `DatabaseSeeder.SeedAsync` **não duplica** dados quando executado pela segunda vez (idempotência)

### 9.4 Mocks

- `ISaleRepository` → NSubstitute
- `ISaleSnapshotRepository` → NSubstitute
- `IEventPublisher` → NSubstitute (verifica se `PublishAsync` foi chamado)

---

## Ordem de Execução

| # | Etapa | Dependência |
|---|---|---|
| 1 | Docker Compose (RabbitMQ + connection strings) | — |
| 2 | Domain (entidades, validadores, eventos, interfaces, snapshot model) | — |
| 3 | ORM PostgreSQL (mapeamento, repositório, migração) | Domain |
| 4 | NoSql MongoDB (novo projeto, contexto, repositório) | Domain |
| 5 | MessageBroker RabbitMQ (novo projeto, publisher) | Domain |
| 6 | IoC (registrar novos serviços) | ORM, NoSql, MessageBroker |
| 7 | Application (handlers, commands, queries, profiles) | Domain, IoC |
| 8 | WebApi (controller, requests, **migrations automáticas**, **seeds**) | Application, ORM |
| 9 | Seeds e Inicialização Automática (`DatabaseSeeder`, `Program.cs`) | ORM, NoSql |
| 10 | Testes Unitários | Todos |
