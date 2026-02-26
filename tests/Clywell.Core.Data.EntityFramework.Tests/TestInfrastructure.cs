using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Clywell.Core.Data.EntityFramework.Tests;

/// <summary>
/// Shared test entity and DbContext for EF Core integration tests.
/// </summary>
public sealed class TestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Priority { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Guid? CategoryId { get; set; }

    public TestCategory? Category { get; set; }
}

public sealed class TestCategory : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<TestEntity> Entities { get; set; } = [];
}

/// <summary>
/// Custom repository interface for assembly-scanning tests.
/// </summary>
public interface ITestEntityRepository : IRepository<TestEntity, Guid>;

/// <summary>
/// Custom repository implementation for assembly-scanning tests.
/// </summary>
public sealed class TestEntityRepository(TestDbContext context) : EfRepository<TestEntity, Guid>(context), ITestEntityRepository;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    public DbSet<TestCategory> Categories => Set<TestCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Entities)
                  .HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<TestCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
        });
    }
}

/// <summary>
/// Factory for creating a SQLite in-memory <see cref="TestDbContext"/>.
/// </summary>
public static class TestDbContextFactory
{
    public static TestDbContext CreateSqlite()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    public static TestDbContext CreatePostgres(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        // Truncate all tables so each test class starts clean
        context.Database.ExecuteSqlRaw("""
            TRUNCATE TABLE "TestEntities", "Categories" CASCADE
            """);

        return context;
    }
}

/// <summary>
/// xUnit fixture that manages a shared PostgreSQL Testcontainer.
/// Started once per collection, reused across all test classes in that collection.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
