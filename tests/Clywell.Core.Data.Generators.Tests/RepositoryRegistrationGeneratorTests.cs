using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Clywell.Core.Data.Generators.Tests;

/// <summary>
/// Unit tests for <see cref="RepositoryRegistrationGenerator"/>.
/// </summary>
public sealed class RepositoryRegistrationGeneratorTests
{
    // ============================================================
    // Helpers
    // ============================================================

    /// <summary>
    /// Runs the generator against the provided C# source and returns all generated source files.
    /// The standard library and Clywell.Core.Data abstractions are referenced automatically.
    /// </summary>
    private static IReadOnlyList<string> RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // References: mscorlib/System.Runtime + Clywell.Core.Data abstractions
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(Clywell.Core.Data.IRepository<,>).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new RepositoryRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();

        return result.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();
    }

    // ============================================================
    // Tests
    // ============================================================

    [Fact]
    public void Generator_NoRepositories_ProducesNoOutput()
    {
        var source = """
            namespace MyApp
            {
                public class NotARepository { }
            }
            """;

        var output = RunGenerator(source);

        Assert.Empty(output);
    }

    [Fact]
    public void Generator_SingleRepository_EmitsRegistration()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Order : IEntity<System.Guid>
                {
                    public System.Guid Id { get; set; }
                }

                public interface IOrderRepository : IRepository<Order, System.Guid> { }

                public sealed class OrderRepository : EfRepositoryStub<Order, System.Guid>, IOrderRepository { }

                // Minimal stub so the class compiles in isolation
                public class EfRepositoryStub<TEntity, TId>
                    where TEntity : class, IEntity<TId>
                    where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        var generated = Assert.Single(output);
        Assert.Contains("TryAddScoped", generated);
        Assert.Contains("IOrderRepository", generated);
        Assert.Contains("OrderRepository", generated);
    }

    [Fact]
    public void Generator_MultipleRepositories_EmitsAllRegistrations()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Order : IEntity<System.Guid> { public System.Guid Id { get; set; } }
                public class Ticket : IEntity<System.Guid> { public System.Guid Id { get; set; } }

                public interface IOrderRepository : IRepository<Order, System.Guid> { }
                public interface ITicketRepository : IRepository<Ticket, System.Guid> { }

                public sealed class OrderRepository : Stub<Order, System.Guid>, IOrderRepository { }
                public sealed class TicketRepository : Stub<Ticket, System.Guid>, ITicketRepository { }

                public class Stub<T, TId> where T : class, IEntity<TId> where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        var generated = Assert.Single(output);
        Assert.Contains("IOrderRepository", generated);
        Assert.Contains("ITicketRepository", generated);
        Assert.Contains("OrderRepository", generated);
        Assert.Contains("TicketRepository", generated);
    }

    [Fact]
    public void Generator_ReadOnlyRepository_EmitsRegistration()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Report : IEntity<System.Guid> { public System.Guid Id { get; set; } }

                public interface IReportRepository : IReadRepository<Report, System.Guid> { }

                public sealed class ReportRepository : ReadStub<Report, System.Guid>, IReportRepository { }

                public class ReadStub<T, TId> where T : class, IEntity<TId> where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        var generated = Assert.Single(output);
        Assert.Contains("IReportRepository", generated);
        Assert.Contains("ReportRepository", generated);
    }

    [Fact]
    public void Generator_AbstractClass_IsNotRegistered()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Order : IEntity<System.Guid> { public System.Guid Id { get; set; } }
                public interface IOrderRepository : IRepository<Order, System.Guid> { }

                public abstract class BaseOrderRepository : Stub<Order, System.Guid>, IOrderRepository { }

                public class Stub<T, TId> where T : class, IEntity<TId> where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        // Abstract class should not be registered
        Assert.Empty(output);
    }

    [Fact]
    public void Generator_EmittedFile_ContainsAutoGeneratedHeader()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Order : IEntity<System.Guid> { public System.Guid Id { get; set; } }
                public interface IOrderRepository : IRepository<Order, System.Guid> { }
                public sealed class OrderRepository : Stub<Order, System.Guid>, IOrderRepository { }
                public class Stub<T, TId> where T : class, IEntity<TId> where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        var generated = Assert.Single(output);
        Assert.Contains("// <auto-generated/>", generated);
        Assert.Contains("AddRepositories", generated);
    }

    [Fact]
    public void Generator_EmittedMethod_HasCorrectSignature()
    {
        var source = """
            using Clywell.Core.Data;

            namespace MyApp
            {
                public class Order : IEntity<System.Guid> { public System.Guid Id { get; set; } }
                public interface IOrderRepository : IRepository<Order, System.Guid> { }
                public sealed class OrderRepository : Stub<Order, System.Guid>, IOrderRepository { }
                public class Stub<T, TId> where T : class, IEntity<TId> where TId : notnull { }
            }
            """;

        var output = RunGenerator(source);

        var generated = Assert.Single(output);
        Assert.Contains("public static IServiceCollection AddRepositories(", generated);
        Assert.Contains("this IServiceCollection services", generated);
        Assert.Contains("return services;", generated);
    }
}
