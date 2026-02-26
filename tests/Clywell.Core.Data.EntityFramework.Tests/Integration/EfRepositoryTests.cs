namespace Clywell.Core.Data.EntityFramework.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="EfRepository{TEntity, TId}"/> CRUD operations.
/// </summary>
public abstract class EfRepositoryTests : IAsyncLifetime
{
    private TestDbContext _context = null!;
    private EfRepository<TestEntity, Guid> _repository = null!;

    protected abstract TestDbContext CreateContext();

    public Task InitializeAsync()
    {
        _context = CreateContext();
        _repository = new EfRepository<TestEntity, Guid>(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    // ============================================================
    // AddAsync Tests
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldAddEntityToContext()
    {
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Status = "Active",
            Priority = 1,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(1, await _context.TestEntities.CountAsync());
    }

    [Fact]
    public async Task AddAsync_WithNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleEntities()
    {
        var entities = Enumerable.Range(1, 5).Select(i => new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Status = "Active",
            Priority = i,
            CreatedAtUtc = DateTime.UtcNow,
        }).ToList();

        await _repository.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        Assert.Equal(5, await _context.TestEntities.CountAsync());
    }

    // ============================================================
    // GetByIdAsync Tests
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_Existing_ShouldReturnEntity()
    {
        var entity = await SeedSingleEntity();

        var result = await _repository.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ShouldReturnNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ============================================================
    // ListAsync Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_ShouldReturnAllEntities()
    {
        await SeedMultipleEntities(3);

        var results = await _repository.ListAsync();

        Assert.Equal(3, results.Count);
    }

    // ============================================================
    // Update Tests
    // ============================================================

    [Fact]
    public async Task Update_ShouldModifyEntity()
    {
        var entity = await SeedSingleEntity();

        entity.Name = "Updated Name";
        _repository.Update(entity);
        await _context.SaveChangesAsync();

        var updated = await _context.TestEntities.FindAsync(entity.Id);
        Assert.Equal("Updated Name", updated!.Name);
    }

    // ============================================================
    // Remove Tests
    // ============================================================

    [Fact]
    public async Task Remove_ShouldDeleteEntity()
    {
        var entity = await SeedSingleEntity();

        _repository.Remove(entity);
        await _context.SaveChangesAsync();

        Assert.Equal(0, await _context.TestEntities.CountAsync());
    }

    [Fact]
    public async Task RemoveRange_ShouldDeleteMultipleEntities()
    {
        var entities = await SeedMultipleEntities(3);

        _repository.RemoveRange(entities.Take(2));
        await _context.SaveChangesAsync();

        Assert.Equal(1, await _context.TestEntities.CountAsync());
    }

    // ============================================================
    // Specification Tests
    // ============================================================

    private sealed class ActiveEntitiesSpec : Specification<TestEntity>
    {
        public ActiveEntitiesSpec()
        {
            Where(e => e.Status == "Active");
            AsReadOnly();
        }
    }

    private sealed class HighPrioritySpec : Specification<TestEntity>
    {
        public HighPrioritySpec(int minPriority)
        {
            Where(e => e.Priority > minPriority);
            AsReadOnly();
        }
    }

    private sealed class EntityByIdSpec : Specification<TestEntity>
    {
        public EntityByIdSpec(Guid id)
        {
            Where(e => e.Id == id);
            AsReadOnly();
        }
    }

    [Fact]
    public async Task ListAsync_WithSpec_ShouldReturnFilteredEntities()
    {
        await SeedMultipleEntities(3);

        var spec = new ActiveEntitiesSpec();
        var results = await _repository.ListAsync(spec);

        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal("Active", e.Status));
    }

    [Fact]
    public async Task CountAsync_WithSpec_ShouldReturnCount()
    {
        await SeedMultipleEntities(5);

        var spec = new HighPrioritySpec(2);
        var count = await _repository.CountAsync(spec);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AnyAsync_WithSpec_WithMatches_ShouldReturnTrue()
    {
        await SeedMultipleEntities(3);

        var spec = new ActiveEntitiesSpec();
        var exists = await _repository.AnyAsync(spec);

        Assert.True(exists);
    }

    [Fact]
    public async Task AnyAsync_WithSpec_NoMatches_ShouldReturnFalse()
    {
        await SeedMultipleEntities(3);

        var spec = new HighPrioritySpec(100);
        var exists = await _repository.AnyAsync(spec);

        Assert.False(exists);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatch_ShouldReturnEntity()
    {
        var entity = await SeedSingleEntity();

        var spec = new EntityByIdSpec(entity.Id);
        var result = await _repository.FirstOrDefaultAsync(spec);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private async Task<TestEntity> SeedSingleEntity()
    {
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Status = "Active",
            Priority = 1,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    private async Task<List<TestEntity>> SeedMultipleEntities(int count)
    {
        var entities = Enumerable.Range(1, count).Select(i => new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Entity {i}",
            Status = "Active",
            Priority = i,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
        }).ToList();

        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities;
    }
}

// ============================================================
// Provider-specific subclasses
// ============================================================

public sealed class SqliteEfRepositoryTests : EfRepositoryTests
{
    protected override TestDbContext CreateContext() => TestDbContextFactory.CreateSqlite();
}

[Collection(PostgresCollection.Name)]
public sealed class PostgresEfRepositoryTests(PostgresContainerFixture postgres) : EfRepositoryTests
{
    protected override TestDbContext CreateContext() => TestDbContextFactory.CreatePostgres(postgres.ConnectionString);
}
