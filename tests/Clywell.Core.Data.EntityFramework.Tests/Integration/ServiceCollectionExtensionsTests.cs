using Microsoft.Extensions.DependencyInjection;

namespace Clywell.Core.Data.EntityFramework.Tests.Integration;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/> DI registration.
/// These are provider-independent (only test the container wiring), so no Postgres variant needed.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClywellDataAccess_ShouldRegisterUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();

        using var provider = services.BuildServiceProvider();
        var uow = provider.GetService<IUnitOfWork>();

        Assert.NotNull(uow);
        Assert.IsType<EfUnitOfWork>(uow);
    }

    [Fact]
    public void AddClywellDataAccess_ShouldRegisterSpecificationEvaluator()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();

        using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetService<ISpecificationEvaluator>();

        Assert.NotNull(evaluator);
    }

    [Fact]
    public void AddRepository_ShouldRegisterScopedRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddRepository<IRepository<TestEntity, Guid>, TestEntityRepository>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repo = scope.ServiceProvider.GetService<IRepository<TestEntity, Guid>>();

        Assert.NotNull(repo);
        Assert.IsType<TestEntityRepository>(repo);
    }

    [Fact]
    public void AddClywellDataAccess_CalledTwice_ShouldNotDuplicate()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddDataAccess<TestDbContext>(); // Second call

        var uowRegistrations = services.Where(s => s.ServiceType == typeof(IUnitOfWork)).ToList();
        Assert.Single(uowRegistrations);
    }

    [Fact]
    public void AddRepositoriesFromAssembly_ShouldRegisterCustomInterface()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddRepositoriesFromAssembly(typeof(TestEntityRepository).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repo = scope.ServiceProvider.GetService<ITestEntityRepository>();

        Assert.NotNull(repo);
        Assert.IsType<TestEntityRepository>(repo);
    }

    [Fact]
    public void AddRepositoriesFromAssembly_ShouldRegisterBaseInterfaces()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddRepositoriesFromAssembly(typeof(TestEntityRepository).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var repo = scope.ServiceProvider.GetService<IRepository<TestEntity, Guid>>();
        var readRepo = scope.ServiceProvider.GetService<IReadRepository<TestEntity, Guid>>();

        Assert.NotNull(repo);
        Assert.NotNull(readRepo);
    }

    [Fact]
    public void AddRepositoriesFromAssemblyContaining_ShouldRegisterRepositories()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddRepositoriesFromAssemblyContaining<TestEntityRepository>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repo = scope.ServiceProvider.GetService<ITestEntityRepository>();

        Assert.NotNull(repo);
        Assert.IsType<TestEntityRepository>(repo);
    }

    [Fact]
    public void AddRepositoriesFromAssembly_CalledTwice_ShouldNotDuplicate()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, "DataSource=:memory:"));
        services.AddDataAccess<TestDbContext>();
        services.AddRepositoriesFromAssembly(typeof(TestEntityRepository).Assembly);
        services.AddRepositoriesFromAssembly(typeof(TestEntityRepository).Assembly);

        var repoRegistrations = services.Where(s => s.ServiceType == typeof(ITestEntityRepository)).ToList();
        Assert.Single(repoRegistrations);
    }
}
