# Clywell.Core.Data & Clywell.Core.Data.EntityFramework

[![Build Status](https://github.com/clywell/clywell-core/actions/workflows/ci.yml/badge.svg)](https://github.com/clywell/clywell-core/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Data access abstractions and EF Core implementation for clean architecture .NET applications.

## Overview

This solution provides two NuGet packages that enforce clean architecture by separating data access abstractions from their EF Core implementation:

| Package                               | Purpose                                              | EF Core Dependency |
| ------------------------------------- | ---------------------------------------------------- | ------------------ |
| **Clywell.Core.Data**                 | Interfaces, specifications, and query abstractions   | **None**           |
| **Clywell.Core.Data.EntityFramework** | EF Core implementations of all abstractions          | **Yes**            |

Your **Application layer** references only `Clywell.Core.Data` → zero EF Core dependency.  
Your **Infrastructure layer** references `Clywell.Core.Data.EntityFramework` → provides the implementations.

## Features

- **Repository Pattern** — `IReadRepository<T, TId>` and `IRepository<T, TId>` with full CRUD
- **Specification Pattern** — Composable, testable, reusable query objects with fluent builder API
- **Projection Specifications** — `Specification<T, TResult>` with `Select()` for read-optimized queries
- **Eager Loading** — Strongly-typed `Include` / `ThenInclude` builder with collection support
- **Unit of Work** — `IUnitOfWork` with `SaveChangesAsync` and `BeginTransactionAsync`
- **Explicit Transactions** — `IDataTransaction` with `CommitAsync` / `RollbackAsync` and `IAsyncDisposable`
- **DI Registration** — `AddDataAccess<TContext>()`, `AddRepository<TInterface, TImpl>()`, and `AddRepositoriesFromAssembly()` for auto-scanning

## Installation

```bash
# Application layer (abstractions only)
dotnet add package Clywell.Core.Data

# Infrastructure layer (EF Core implementation)
dotnet add package Clywell.Core.Data.EntityFramework
```

## Quick Start

### 1. Define Your Entity

```csharp
public sealed class Ticket : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Open";
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<Comment> Comments { get; private set; } = [];
}
```

### 2. Define a Repository Interface (Domain Layer)

```csharp
public interface ITicketRepository : IRepository<Ticket, Guid>
{
    // Add domain-specific query methods if needed
}
```

### 3. Create Specifications (Application Layer)

Specifications encapsulate all query logic — filters, ordering, paging, and eager loading — in a
single reusable class. Multiple `Where` calls are AND'd together.

```csharp
public sealed class ActiveTicketsByTenantSpec : Specification<Ticket>
{
    public ActiveTicketsByTenantSpec(Guid tenantId, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        Where(t => t.TenantId == tenantId);
        Where(t => t.Status == "Open");
        OrderByDescending(t => t.CreatedAtUtc);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AsReadOnly();
    }
}

// Projection specification — maps to a DTO directly in the SQL query
public sealed class TicketSummarySpec : Specification<Ticket, TicketSummaryDto>
{
    public TicketSummarySpec(Guid tenantId)
    {
        Where(t => t.TenantId == tenantId);
        OrderByDescending(t => t.CreatedAtUtc);
        Select(t => new TicketSummaryDto(t.Id, t.Title, t.Status));
        AsReadOnly();
    }
}

// Eager loading — load related navigation properties
public sealed class TicketWithCommentsSpec : Specification<Ticket>
{
    public TicketWithCommentsSpec(Guid tenantId, Guid ticketId)
    {
        Where(t => t.TenantId == tenantId);
        Where(t => t.Id == ticketId);
        IncludeCollection(t => t.Comments)
            .ThenInclude(c => c.Author);
        AsReadOnly();
    }
}
```

### 4. Use in Command/Query Handlers (Application Layer)

**Query handler** using a specification:

```csharp
public sealed class GetActiveTicketsHandler
{
    private readonly IReadRepository<Ticket, Guid> _repository;

    public GetActiveTicketsHandler(IReadRepository<Ticket, Guid> repository)
        => _repository = repository;

    public async Task<IReadOnlyList<Ticket>> HandleAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct)
    {
        var spec = new ActiveTicketsByTenantSpec(tenantId, page, pageSize);
        return await _repository.ListAsync(spec, ct);
    }
}
```

**Projection query** — let the database do the column selection:

```csharp
public async Task<IReadOnlyList<TicketSummaryDto>> GetSummariesAsync(
    Guid tenantId, CancellationToken ct)
{
    var spec = new TicketSummarySpec(tenantId);
    return await _repository.ListAsync(spec, ct);
}
```

**Existence and count checks**:

```csharp
// Check whether any ticket matches without loading data
bool hasOpen = await _repository.AnyAsync(
    new ActiveTicketsByTenantSpec(tenantId, 1, 1), ct);

// Count without paging (spec paging/ordering is ignored for count queries)
int totalOpen = await _repository.CountAsync(
    new ActiveTicketsByTenantSpec(tenantId, 1, int.MaxValue), ct);

// Retrieve single known entity
Ticket? ticket = await _repository.GetByIdAsync(ticketId, ct);

// Retrieve first match
Ticket? first = await _repository.FirstOrDefaultAsync(
    new ActiveTicketsByTenantSpec(tenantId, 1, 1), ct);
```

**Command handler** with Unit of Work:

```csharp
public sealed class CreateTicketHandler
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketHandler(ITicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Ticket> HandleAsync(CreateTicketCommand command, CancellationToken ct)
    {
        var ticket = Ticket.Create(command.TenantId, command.Title);
        await _repository.AddAsync(ticket, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ticket;
    }
}
```

**Bulk write operations**:

```csharp
// Add multiple entities
await _repository.AddRangeAsync(tickets, ct);

// Mark entities as modified (update)
_repository.Update(ticket);
_repository.UpdateRange(tickets);

// Remove entities
_repository.Remove(ticket);
_repository.RemoveRange(tickets);

// Persist all pending changes
await _unitOfWork.SaveChangesAsync(ct);
```

### 5. Implement the Repository (Infrastructure Layer)

```csharp
public sealed class TicketRepository : EfRepository<Ticket, Guid>, ITicketRepository
{
    public TicketRepository(AppDbContext context) : base(context) { }
}
```

### 6. Register in DI (Infrastructure Layer)

**Option A — Auto-scan an assembly** (recommended for projects with many repositories):

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddDataAccess<AppDbContext>();

// Scans the assembly containing TicketRepository and registers every
// concrete repository against its repository interfaces (scoped).
services.AddRepositoriesFromAssemblyContaining<TicketRepository>();
```

You can also pass an `Assembly` directly:

```csharp
services.AddRepositoriesFromAssembly(typeof(TicketRepository).Assembly);
```

**Option B — Register individually**:

```csharp
services.AddDataAccess<AppDbContext>();
services.AddRepository<ITicketRepository, TicketRepository>();
services.AddRepository<IOrderRepository, OrderRepository>();
```

---

## Security & Multi-Tenancy

Correct use of the specification pattern is critical for enforcing data isolation. Every query that
accesses tenant-owned data **must** include a tenant filter inside the specification. This ensures
the database always sees a parameterized predicate and prevents cross-tenant data leakage.

### Tenant Isolation via Specifications

Always scope specs to the authenticated tenant's ID — never query without a tenant boundary:

```csharp
public sealed class TenantTicketsSpec : Specification<Ticket>
{
    // TenantId comes from a trusted source (e.g., ICurrentTenant service),
    // never directly from raw user-supplied input.
    public TenantTicketsSpec(Guid tenantId)
    {
        Where(t => t.TenantId == tenantId);
        AsReadOnly();
    }
}
```

Resolve the current tenant from a trusted identity service rather than request parameters:

```csharp
public sealed class GetTicketsHandler
{
    private readonly IReadRepository<Ticket, Guid> _repository;
    private readonly ICurrentTenant _currentTenant; // e.g., from your auth middleware

    public GetTicketsHandler(
        IReadRepository<Ticket, Guid> repository,
        ICurrentTenant currentTenant)
    {
        _repository = repository;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<Ticket>> HandleAsync(CancellationToken ct)
    {
        // Tenant ID is resolved from the authenticated principal, not a query string.
        var spec = new TenantTicketsSpec(_currentTenant.TenantId);
        return await _repository.ListAsync(spec, ct);
    }
}
```

### Verifying Ownership Before Mutation

Before updating or deleting an entity, confirm it belongs to the current tenant:

```csharp
public async Task HandleAsync(UpdateTicketCommand command, CancellationToken ct)
{
    // Fetch using a spec that combines tenant + entity ID — both must match.
    var spec = new TicketByIdForTenantSpec(_currentTenant.TenantId, command.TicketId);
    var ticket = await _repository.FirstOrDefaultAsync(spec, ct)
        ?? throw new NotFoundException($"Ticket {command.TicketId} was not found.");

    ticket.Update(command.Title, command.Status);
    _repository.Update(ticket);
    await _unitOfWork.SaveChangesAsync(ct);
}
```

```csharp
public sealed class TicketByIdForTenantSpec : Specification<Ticket>
{
    public TicketByIdForTenantSpec(Guid tenantId, Guid ticketId)
    {
        Where(t => t.TenantId == tenantId);
        Where(t => t.Id == ticketId);
    }
}
```

### Input Validation in Specifications

Validate inputs at the specification boundary to avoid unexpected query behaviour:

```csharp
public sealed class PagedTicketsSpec : Specification<Ticket>
{
    public PagedTicketsSpec(Guid tenantId, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100); // enforce maximum

        Where(t => t.TenantId == tenantId);
        OrderByDescending(t => t.CreatedAtUtc);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AsReadOnly();
    }
}
```

### Principle of Least Privilege

Inject `IReadRepository<T, TId>` in query handlers — not `IRepository<T, TId>`. This makes the
intent explicit and prevents accidental writes from read-only code paths:

```csharp
// Correct — read-only handler receives read-only repository
public sealed class GetTicketsHandler(IReadRepository<Ticket, Guid> repository) { ... }

// Correct — command handler receives writable repository
public sealed class CreateTicketHandler(ITicketRepository repository, IUnitOfWork unitOfWork) { ... }
```

### Explicit Transactions

Use `await using` to guarantee the transaction is disposed (and rolled back if uncommitted) even
when an exception is thrown:

```csharp
public async Task HandleAsync(TransferTicketsCommand command, CancellationToken ct)
{
    await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
    try
    {
        var source = await _sourceRepository.FirstOrDefaultAsync(
            new TicketByIdForTenantSpec(_currentTenant.TenantId, command.SourceId), ct)
            ?? throw new NotFoundException("Source ticket not found.");

        var target = await _targetRepository.FirstOrDefaultAsync(
            new TicketByIdForTenantSpec(_currentTenant.TenantId, command.TargetId), ct)
            ?? throw new NotFoundException("Target ticket not found.");

        source.Transfer(target);
        _sourceRepository.Update(source);
        _targetRepository.Update(target);

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```

---

## API Reference

### Clywell.Core.Data (Abstractions)

| Type                            | Purpose                                                              |
| ------------------------------- | -------------------------------------------------------------------- |
| `IEntity<TId>`                  | Base entity identity contract                                        |
| `IReadRepository<TEntity, TId>` | Read-only: `GetByIdAsync`, `ListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync` |
| `IRepository<TEntity, TId>`     | Full CRUD: extends `IReadRepository` + `AddAsync`, `AddRangeAsync`, `Update`, `UpdateRange`, `Remove`, `RemoveRange` |
| `ISpecification<T>`             | Query specification interface                                        |
| `Specification<T>`              | Fluent spec builder: `Where`, `OrderBy`, `OrderByDescending`, `Include`, `IncludeCollection`, `ApplyPaging`, `AsReadOnly` |
| `Specification<T, TResult>`     | Projection spec builder: extends `Specification<T>` with `Select()` |
| `IIncludeBuilder<T, TProperty>` | Fluent builder for chaining `ThenInclude` / `ThenIncludeCollection`  |
| `ISpecificationEvaluator`       | Pluggable spec-to-query translation                                  |
| `IUnitOfWork`                   | `SaveChangesAsync` + `BeginTransactionAsync`                         |
| `IDataTransaction`              | `CommitAsync` + `RollbackAsync` (`IAsyncDisposable`)                 |

#### `IReadRepository<TEntity, TId>` Methods

| Method | Description |
| ------ | ----------- |
| `GetByIdAsync(TId, ct)` | Returns the entity with the given ID, or `null` |
| `ListAsync(ISpecification<T>, ct)` | Returns all entities matching the specification |
| `ListAsync<TResult>(ISpecification<T, TResult>, ct)` | Returns projected results matching the specification |
| `ListAsync(ct)` | Returns all entities (no filter) |
| `FirstOrDefaultAsync(ISpecification<T>, ct)` | Returns the first matching entity, or `null` |
| `CountAsync(ISpecification<T>, ct)` | Returns the count of matching entities (ignores paging/ordering) |
| `AnyAsync(ISpecification<T>, ct)` | Returns `true` if any entity matches the specification |

#### `IRepository<TEntity, TId>` Additional Methods

| Method | Description |
| ------ | ----------- |
| `AddAsync(TEntity, ct)` | Adds a single entity; returns the tracked entity |
| `AddRangeAsync(IEnumerable<TEntity>, ct)` | Adds multiple entities |
| `Update(TEntity)` | Marks entity as modified |
| `UpdateRange(IEnumerable<TEntity>)` | Marks multiple entities as modified |
| `Remove(TEntity)` | Marks entity for deletion |
| `RemoveRange(IEnumerable<TEntity>)` | Marks multiple entities for deletion |

> **Note:** Write operations are not persisted until `IUnitOfWork.SaveChangesAsync` is called.

#### `Specification<T>` Builder Methods

| Method | Description |
| ------ | ----------- |
| `Where(predicate)` | Adds a filter criterion — multiple calls are AND'd |
| `OrderBy(keySelector)` | Adds an ascending ordering expression |
| `OrderByDescending(keySelector)` | Adds a descending ordering expression |
| `Include<TProperty>(expression)` | Eagerly loads a reference navigation property |
| `IncludeCollection<TProperty>(expression)` | Eagerly loads a collection navigation property |
| `Include(string path)` | String-based include path (e.g., `"Orders.Items"`) |
| `ApplyPaging(skip, take)` | Applies offset pagination |
| `AsReadOnly()` | Hints the infrastructure to use `AsNoTracking` |

After `Include` or `IncludeCollection`, you can chain deeper loads:

```csharp
IncludeCollection(t => t.Comments)          // Include comments collection
    .ThenInclude(c => c.Author)             // Then include each comment's Author
    .ThenInclude(a => a.ProfileImage);      // Then include Author's ProfileImage
```

### Clywell.Core.Data.EntityFramework (Implementations)

| Type                             | Purpose                                              |
| -------------------------------- | ---------------------------------------------------- |
| `EfReadRepository<TEntity, TId>` | Read-only EF Core repository; applies `AsNoTracking` |
| `EfRepository<TEntity, TId>`     | Full CRUD; `GetByIdAsync` uses `FindAsync` (tracked) |
| `EfUnitOfWork`                   | Wraps `DbContext.SaveChangesAsync`                   |
| `EfDataTransaction`              | Wraps `IDbContextTransaction`; rolls back on dispose |
| `EfSpecificationEvaluator`       | Translates `ISpecification` to EF Core LINQ          |
| `ServiceCollectionExtensions`    | `AddDataAccess<TContext>`, `AddRepository<,>`, `AddRepositoriesFromAssembly`, `AddRepositoriesFromAssemblyContaining<T>` |

---

## Architecture

```
┌─────────────────────────────────────────────┐
│  Application Layer                          │
│  ┌─────────────────────────────────────┐    │
│  │  References: Clywell.Core.Data      │    │
│  │  Uses: IRepository, Specification,  │    │
│  │        IUnitOfWork, IDataTransaction │    │
│  │  NO EF Core dependency              │    │
│  └─────────────────────────────────────┘    │
├─────────────────────────────────────────────┤
│  Infrastructure Layer                       │
│  ┌─────────────────────────────────────┐    │
│  │  References: Clywell.Core.Data.EF   │    │
│  │  Uses: EfRepository, EfUnitOfWork,  │    │
│  │        DbContext, EF Core           │    │
│  └─────────────────────────────────────┘    │
└─────────────────────────────────────────────┘
```

## Dependencies

- **Clywell.Core.Data:** `Clywell.Primitives` only
- **Clywell.Core.Data.EntityFramework:** `Clywell.Core.Data` + `Microsoft.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.Relational`

## Contributing

See the [getting started guide](docs/getting-started.md) for development setup.

## License

MIT © 2026 Clywell
