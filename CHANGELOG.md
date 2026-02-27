# Changelog

All notable changes to the Clywell.Core.Data packages will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/clywell/clywell-core/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/clywell/clywell-core/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/clywell/clywell-core/releases/tag/v1.0.0
