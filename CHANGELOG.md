# Changelog

All notable changes to the Clywell.Core.Data packages will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.2.2] - 2026-06-02

### Changed

#### Test dependencies

- Bumped `Microsoft.NET.Test.Sdk` from `18.5.1` to `18.6.0`
- Bumped `Npgsql.EntityFrameworkCore.PostgreSQL` from `10.0.1` to `10.0.2`

## [2.2.1] - 2026-04-20

### Changed

#### `Clywell.Core.Data`

- Bumped `Microsoft.SourceLink.GitHub` from `10.0.201` to `10.0.202`

#### `Clywell.Core.Data.EntityFramework`

- Bumped `Microsoft.EntityFrameworkCore` from `10.0.5` to `10.0.6`
- Bumped `Microsoft.EntityFrameworkCore.Relational` from `10.0.5` to `10.0.6`
- Bumped `Microsoft.Extensions.DependencyInjection.Abstractions` from `10.0.5` to `10.0.6`
- Bumped `Microsoft.SourceLink.GitHub` from `10.0.201` to `10.0.202`

#### Test dependencies

- Bumped `Microsoft.EntityFrameworkCore.Sqlite` from `10.0.5` to `10.0.6`
- Bumped `Microsoft.Extensions.DependencyInjection` from `10.0.5` to `10.0.6`
- Bumped `coverlet.collector` from `8.0.1` to `10.0.0`

## [2.2.0] - 2026-03-23

### Added

#### `Clywell.Core.Data` (Abstractions)

- `IHasDomainEvents` — non-generic interface exposing `IReadOnlyList<IDomainEvent> DomainEvents` and `ClearDomainEvents()`. Used by EF Core interceptors to drain events without knowledge of the entity's key type.
- `Entity<TId>` — abstract base class for domain entities. Implements both `IEntity<TId>` and `IHasDomainEvents`. Exposes `RaiseDomainEvent(IDomainEvent)` (protected) for raising events from within the entity, and `ClearDomainEvents()` for use by interceptors after dispatch.

#### `Clywell.Core.Data.EntityFramework` (EF Core Implementation)

- `IDomainEventDispatcher` — interface for dispatching a list of `IDomainEvent` instances to their registered handlers. Implemented by `Clywell.Core.Messaging`.
- `DomainEventDispatchInterceptor` — singleton `SaveChangesInterceptor` that dispatches domain events after `SaveChangesAsync` succeeds (post-save, in-process only). Resolves `IDomainEventDispatcher` per invocation via `IServiceScopeFactory` to prevent captive-dependency issues.
- `DbContextOptionsBuilderExtensions.UseDomainEventDispatchInterceptor(IServiceProvider)` — wires `DomainEventDispatchInterceptor` onto a `DbContextOptionsBuilder`. Call from an `IDbContextOptionsConfiguration<T>` implementation.
- `ServiceCollectionExtensions.AddDomainEventDispatching()` — registers `DomainEventDispatchInterceptor` as a singleton.

### Changed

- Updated dependency: `Clywell.Primitives` → `1.2.0` (adds `IDomainEvent` and `IIntegrationEvent`).

## [2.1.1] - 2026-03-15

### Changed

#### Dependency Updates

- `Clywell.Core.Data.EntityFramework` now targets `Microsoft.EntityFrameworkCore` `10.0.5` (from `10.0.3`)
- `Clywell.Core.Data.EntityFramework` now targets `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (from `10.0.3`)
- test dependencies updated to `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` and `Microsoft.Extensions.DependencyInjection` `10.0.5`

## [2.0.0] - 2026-03-06

### Breaking Changes

#### `Clywell.Core.Data` (Abstractions)

- `IReadRepository<TEntity, TId>` is now `IReadRepository<TEntity>` — the `TId` generic parameter has been removed; `GetByIdAsync` now accepts `object id` instead of a typed `TId id`
- `IRepository<TEntity, TId>` is now `IRepository<TEntity>` — the `TId` generic parameter has been removed
- `IDataContext.Repository<TEntity, TId>()` is now `IDataContext.Repository<TEntity>()` — callers no longer need to supply the entity ID type; specifying the entity type alone is sufficient

#### `Clywell.Core.Data.EntityFramework` (EF Core Implementation)

- `EfReadRepository<TEntity, TId>` is now `EfReadRepository<TEntity>` — the `TId` generic parameter has been removed
- `EfRepository<TEntity, TId>` is now `EfRepository<TEntity>` — the `TId` generic parameter has been removed

### Changed

#### `Clywell.Core.Data.EntityFramework` (EF Core Implementation)

- `EfReadRepository.GetByIdAsync` now uses `DbContext.FindAsync<TEntity>([id])` with a subsequent detach to preserve no-tracking semantics, replacing the previous `FirstOrDefaultAsync(e => e.Id.Equals(id))` approach — improves performance by resolving from the identity map before issuing a database query

### Migration Guide

Replace all two-argument repository generic usages with single-argument equivalents:

| Before                                  | After                                                                                        |
| --------------------------------------- | -------------------------------------------------------------------------------------------- |
| `IRepository<Order, Guid>`              | `IRepository<Order>`                                                                         |
| `IReadRepository<Order, Guid>`          | `IReadRepository<Order>`                                                                     |
| `EfRepository<Order, Guid>`             | `EfRepository<Order>`                                                                        |
| `EfReadRepository<Order, Guid>`         | `EfReadRepository<Order>`                                                                    |
| `dataContext.Repository<Order, Guid>()` | `dataContext.Repository<Order>()`                                                            |
| `GetByIdAsync(id)` (typed `TId`)        | `GetByIdAsync(id)` (`object` — passes transparently, no cast needed for `Guid`, `int`, etc.) |

## [1.1.0] - 2026-02-28

### Added

#### `Clywell.Core.Data` (Abstractions)

- `IAuditable` — entity marker interface capturing `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy`; populated automatically by the EF Core save interceptor in the Infrastructure layer
- `ITenantScoped` — entity marker interface exposing `TenantId`; the Infrastructure layer applies a global EF Core query filter to enforce tenant data isolation
- `ISoftDeletable` — entity marker interface exposing `IsDeleted`, `DeletedAt`, and `DeletedBy`; the Infrastructure layer applies a global EF Core query filter that excludes soft-deleted records from normal queries

## [1.0.1] - 2026-02-27

### Changed

#### `Clywell.Core.Data.EntityFramework`

- Source generator (`RepositoryRegistrationGenerator`) is no longer published as a separate `Clywell.Core.Data.Generators` NuGet package — it is bundled inside `Clywell.Core.Data.EntityFramework` and activated automatically; no separate package install or project reference is required

## [1.0.0] - 2026-02-26

### Added

#### `Clywell.Core.Data` (Abstractions)

- `IEntity<TId>` — base entity identity contract; no EF Core dependency
- `IReadRepository<TEntity, TId>` — read-only repository: `GetByIdAsync`, `ListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync`
- `IRepository<TEntity, TId>` — full CRUD repository extending `IReadRepository`: `AddAsync`, `AddRangeAsync`, `Update`, `UpdateRange`, `Remove`, `RemoveRange`
- `ISpecification<T>` and `ISpecification<T, TResult>` — specification interfaces for encapsulating query criteria
- `Specification<T>` — fluent spec builder: `Where`, `OrderBy`, `OrderByDescending`, `Include`, `IncludeCollection`, `ApplyPaging`, `AsReadOnly`
- `Specification<T, TResult>` — projection spec builder extending `Specification<T>` with `Select()`
- `IIncludeBuilder<T, TProperty>` — fluent builder for chaining `ThenInclude` / `ThenIncludeCollection`
- `ISpecificationEvaluator` — pluggable specification-to-query translation interface
- `IDataContext` — `Repository<TEntity, TId>()` (like `DbContext.Set<T>()`), `SaveChangesAsync`, and `BeginTransactionAsync`
- `IDataTransaction` — `CommitAsync`, `RollbackAsync`, `IAsyncDisposable`
- `OrderExpression<T>` and `IncludeExpression` — value types for specification internals

#### `Clywell.Core.Data.EntityFramework` (EF Core Implementation)

- `EfReadRepository<TEntity, TId>` — read-only EF Core repository; applies `AsNoTracking` by default
- `EfRepository<TEntity, TId>` — full CRUD EF Core repository; `GetByIdAsync` uses `FindAsync` (tracked)
- `EfDataContext` — wraps `DbContext`; exposes repositories via `Repository<TEntity, TId>()`, with per-entity caching, `SaveChangesAsync`, and `BeginTransactionAsync`
- `EfDataTransaction` — wraps `IDbContextTransaction`; rolls back on disposal if uncommitted
- `EfSpecificationEvaluator` — translates `ISpecification` to EF Core LINQ with Include, ThenInclude, ordering, and paging support
- `ServiceCollectionExtensions.AddDataAccess<TContext>()` — registers `IDataContext` (scoped) and `ISpecificationEvaluator` (singleton)
- `ServiceCollectionExtensions.AddRepository<TInterface, TImpl>()` — registers a single repository as scoped
- `ServiceCollectionExtensions.AddRepositoriesFromAssembly(Assembly)` — scans an assembly and auto-registers all repository implementations
- `ServiceCollectionExtensions.AddRepositoriesFromAssemblyContaining<T>()` — convenience overload scanning `typeof(T).Assembly`

#### `Clywell.Core.Data.Generators` (Source Generator)

- Roslyn incremental source generator (`RepositoryRegistrationGenerator`) that scans the host compilation for concrete repository implementations at compile time
- Emits a `RepositoryRegistrationExtensions` class into the consuming project's root namespace containing a single `AddRepositories(this IServiceCollection)` extension method
- Each detected repository is registered via `TryAddScoped<TInterface, TImpl>()`, allowing manual overrides to take precedence
- Detects any non-abstract, non-generic class whose interface hierarchy includes a user-defined sub-interface of `IRepository<,>` or `IReadRepository<,>`; the base interfaces themselves are not registered directly
- Zero reflection at runtime — fully compatible with NativeAOT and the .NET trimmer
- No runtime dependency; `DevelopmentDependency = true` means the package does not appear in consuming projects' dependency graphs
- Replaces `AddRepositoriesFromAssembly()` / `AddRepositoriesFromAssemblyContaining<T>()` for projects that require AOT or trim compatibility

[Unreleased]: https://github.com/clywell/clywell-data/compare/v2.2.2...HEAD
[2.2.2]: https://github.com/clywell/clywell-data/compare/v2.2.1...v2.2.2
[2.2.1]: https://github.com/clywell/clywell-data/compare/v2.2.0...v2.2.1
[2.2.0]: https://github.com/clywell/clywell-data/compare/v2.1.1...v2.2.0
[2.1.1]: https://github.com/clywell/clywell-data/compare/v2.1.0...v2.1.1
[2.0.0]: https://github.com/clywell/clywell-data/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/clywell/clywell-data/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/clywell/clywell-data/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/clywell/clywell-data/releases/tag/v1.0.0
