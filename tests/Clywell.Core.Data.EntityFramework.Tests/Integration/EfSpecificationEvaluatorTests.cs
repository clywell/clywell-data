namespace Clywell.Core.Data.EntityFramework.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="EfSpecificationEvaluator"/> with real EF Core queries.
/// </summary>
public abstract class EfSpecificationEvaluatorTests : IAsyncLifetime
{
    private TestDbContext _context = null!;
    private EfRepository<TestEntity, Guid> _repository = null!;

    protected abstract TestDbContext CreateContext();

    public Task InitializeAsync()
    {
        _context = CreateContext();
        _repository = new EfRepository<TestEntity, Guid>(_context);
        SeedTestData();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    // ============================================================
    // Test Specifications
    // ============================================================

    private sealed class ActiveEntitiesSpec : Specification<TestEntity>
    {
        public ActiveEntitiesSpec()
        {
            Where(e => e.Status == "Active");
            AsReadOnly();
        }
    }

    private sealed class HighPriorityActiveSpec : Specification<TestEntity>
    {
        public HighPriorityActiveSpec(int minPriority)
        {
            Where(e => e.Status == "Active");
            Where(e => e.Priority >= minPriority);
            OrderByDescending(e => e.Priority);
            AsReadOnly();
        }
    }

    private sealed class PagedSpec : Specification<TestEntity>
    {
        public PagedSpec(int skip, int take)
        {
            Where(e => e.Status == "Active");
            OrderBy(e => e.Name);
            ApplyPaging(skip, take);
            AsReadOnly();
        }
    }

    private sealed class OrderedByCreatedSpec : Specification<TestEntity>
    {
        public OrderedByCreatedSpec()
        {
            OrderByDescending(e => e.CreatedAtUtc);
            AsReadOnly();
        }
    }

    private sealed class EntitySummarySpec : Specification<TestEntity, EntitySummary>
    {
        public EntitySummarySpec(string status)
        {
            Where(e => e.Status == status);
            OrderBy(e => e.Name);
            Select(e => new EntitySummary(e.Id, e.Name, e.Priority));
            AsReadOnly();
        }
    }

    private sealed record EntitySummary(Guid Id, string Name, int Priority);

    private sealed class WithCategorySpec : Specification<TestEntity>
    {
        public WithCategorySpec()
        {
            Include("Category");
            AsReadOnly();
        }
    }

    private sealed class WithCategoryEntitiesSpec : Specification<TestCategory>
    {
        public WithCategoryEntitiesSpec()
        {
            IncludeCollection<TestEntity>(c => c.Entities);
        }
    }

    // ============================================================
    // Filter Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_WithCriteria_ShouldFilterCorrectly()
    {
        var spec = new ActiveEntitiesSpec();

        var results = await _repository.ListAsync(spec);

        Assert.All(results, e => Assert.Equal("Active", e.Status));
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task ListAsync_WithMultipleCriteria_ShouldAndThem()
    {
        var spec = new HighPriorityActiveSpec(4);

        var results = await _repository.ListAsync(spec);

        Assert.All(results, e =>
        {
            Assert.Equal("Active", e.Status);
            Assert.True(e.Priority >= 4);
        });
        Assert.Equal(2, results.Count);
    }

    // ============================================================
    // Ordering Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_WithDescendingOrder_ShouldOrderCorrectly()
    {
        var spec = new OrderedByCreatedSpec();

        var results = await _repository.ListAsync(spec);

        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].CreatedAtUtc >= results[i].CreatedAtUtc);
        }
    }

    [Fact]
    public async Task ListAsync_WithOrdering_ShouldReturnOrdered()
    {
        var spec = new HighPriorityActiveSpec(1);

        var results = await _repository.ListAsync(spec);

        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Priority >= results[i].Priority);
        }
    }

    // ============================================================
    // Paging Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_WithPaging_ShouldLimitResults()
    {
        var spec = new PagedSpec(0, 2);

        var results = await _repository.ListAsync(spec);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ListAsync_WithPagingSkip_ShouldSkipCorrectly()
    {
        var firstPage = await _repository.ListAsync(new PagedSpec(0, 2));
        var secondPage = await _repository.ListAsync(new PagedSpec(2, 2));

        Assert.Equal(2, firstPage.Count);
        Assert.Equal(2, secondPage.Count);
        Assert.DoesNotContain(secondPage, e => firstPage.Any(f => f.Id == e.Id));
    }

    // ============================================================
    // Count/Any Tests
    // ============================================================

    [Fact]
    public async Task CountAsync_ShouldReturnFilteredCount()
    {
        var spec = new ActiveEntitiesSpec();

        var count = await _repository.CountAsync(spec);

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task AnyAsync_WithMatches_ShouldReturnTrue()
    {
        var spec = new ActiveEntitiesSpec();

        var exists = await _repository.AnyAsync(spec);

        Assert.True(exists);
    }

    [Fact]
    public async Task AnyAsync_NoMatches_ShouldReturnFalse()
    {
        var spec = new HighPriorityActiveSpec(100);

        var exists = await _repository.AnyAsync(spec);

        Assert.False(exists);
    }

    // ============================================================
    // FirstOrDefault Tests
    // ============================================================

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatch_ShouldReturnEntity()
    {
        var spec = new HighPriorityActiveSpec(5);

        var result = await _repository.FirstOrDefaultAsync(spec);

        Assert.NotNull(result);
        Assert.Equal(5, result.Priority);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ShouldReturnNull()
    {
        var spec = new HighPriorityActiveSpec(100);

        var result = await _repository.FirstOrDefaultAsync(spec);

        Assert.Null(result);
    }

    // ============================================================
    // Projection Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_WithProjection_ShouldReturnProjectedResults()
    {
        var spec = new EntitySummarySpec("Active");

        var results = await _repository.ListAsync(spec);

        Assert.Equal(5, results.Count);
        Assert.All(results, r =>
        {
            Assert.NotEqual(Guid.Empty, r.Id);
            Assert.NotEmpty(r.Name);
        });
    }

    // ============================================================
    // Include Tests
    // ============================================================

    [Fact]
    public async Task ListAsync_WithStringInclude_ShouldLoadRelated()
    {
        var categoryRepo = new EfRepository<TestEntity, Guid>(_context);
        var spec = new WithCategorySpec();

        var results = await categoryRepo.ListAsync(spec);

        // Entities with categories should have them loaded
        var withCategory = results.Where(e => e.CategoryId.HasValue).ToList();
        Assert.NotEmpty(withCategory);
        Assert.All(withCategory, e => Assert.NotNull(e.Category));
    }

    [Fact]
    public async Task ListAsync_WithThenInclude_ShouldLoadNestedRelated()
    {
        var categoryRepo = new EfRepository<TestCategory, Guid>(_context);
        var spec = new WithCategoryEntitiesSpec();

        var results = await categoryRepo.ListAsync(spec);

        Assert.NotEmpty(results);
        var category = results.First();
        Assert.NotEmpty(category.Entities);
    }

    [Fact]
    public async Task CountAsync_OnPagedSpec_ShouldIgnorePaging()
    {
        // A paged spec with take=2, but CountAsync should count ALL matching entities
        var spec = new PagedSpec(0, 2);

        var count = await _repository.CountAsync(spec);

        // There are 5 active entities, counting should ignore the Take(2)
        Assert.Equal(5, count);
    }

    // ============================================================
    // Seed Data
    // ============================================================

    private void SeedTestData()
    {
        var category = new TestCategory
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
        };

        _context.Categories.Add(category);

        for (int i = 1; i <= 5; i++)
        {
            _context.TestEntities.Add(new TestEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Active Entity {i}",
                Status = "Active",
                Priority = i,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                CategoryId = i <= 3 ? category.Id : null,
            });
        }

        for (int i = 1; i <= 3; i++)
        {
            _context.TestEntities.Add(new TestEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Closed Entity {i}",
                Status = "Closed",
                Priority = i,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i - 5),
            });
        }

        _context.SaveChanges();
    }
}

// ============================================================
// Provider-specific subclasses
// ============================================================

public sealed class SqliteEfSpecificationEvaluatorTests : EfSpecificationEvaluatorTests
{
    protected override TestDbContext CreateContext() => TestDbContextFactory.CreateSqlite();
}

[Collection(PostgresCollection.Name)]
public sealed class PostgresEfSpecificationEvaluatorTests(PostgresContainerFixture postgres) : EfSpecificationEvaluatorTests
{
    protected override TestDbContext CreateContext() => TestDbContextFactory.CreatePostgres(postgres.ConnectionString);
}
