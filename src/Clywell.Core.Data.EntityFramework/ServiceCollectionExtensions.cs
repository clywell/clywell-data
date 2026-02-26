using System.Reflection;

namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// Extension methods for registering Clywell data access services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Clywell data access infrastructure with the specified <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The concrete <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Registers:
    /// <list type="bullet">
    ///   <item><description><see cref="IDataContext"/> as scoped (backed by <typeparamref name="TContext"/>)</description></item>
    ///   <item><description><see cref="ISpecificationEvaluator"/> as singleton</description></item>
    ///   <item><description><see cref="DbContext"/> resolved as <typeparamref name="TContext"/></description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddDataAccess<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register specification evaluator as singleton
        services.TryAddSingleton<ISpecificationEvaluator>(EfSpecificationEvaluator.Default);

        // Register UnitOfWork as scoped, backed by the concrete context
        services.TryAddScoped<IDataContext>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            var evaluator = sp.GetRequiredService<ISpecificationEvaluator>();
            return new EfDataContext(context, evaluator);
        });

        return services;
    }

    /// <summary>
    /// Registers a repository interface with its EF Core implementation.
    /// </summary>
    /// <typeparam name="TInterface">The repository interface type.</typeparam>
    /// <typeparam name="TImplementation">The concrete repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepository<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<TInterface, TImplementation>();
        return services;
    }

    /// <summary>
    /// Scans the specified assembly for concrete repository implementations and registers
    /// each one against its repository interfaces as scoped services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for repository implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// A type is considered a repository implementation if it is a non-abstract, non-generic
    /// class that implements at least one interface assignable to
    /// <see cref="IReadRepository{TEntity, TId}"/> or <see cref="IRepository{TEntity, TId}"/>.
    /// </para>
    /// <para>
    /// Each matching interface is registered as a scoped service. Existing registrations
    /// are not overwritten (uses <c>TryAddScoped</c>).
    /// </para>
    /// </remarks>
    public static IServiceCollection AddRepositoriesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var implementationType in assembly.GetTypes())
        {
            if (!implementationType.IsClass || implementationType.IsAbstract || implementationType.IsGenericTypeDefinition)
                continue;

            foreach (var iface in implementationType.GetInterfaces())
            {
                if (!IsRepositoryInterface(iface))
                    continue;

                services.TryAddScoped(iface, implementationType);
            }
        }

        return services;
    }

    /// <summary>
    /// Scans the assembly containing <typeparamref name="T"/> for concrete repository implementations
    /// and registers each one against its repository interfaces as scoped services.
    /// </summary>
    /// <typeparam name="T">
    /// A type whose assembly will be scanned. Typically one of your repository implementations
    /// (e.g., <c>AddRepositoriesFromAssemblyContaining&lt;TicketRepository&gt;()</c>).
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositoriesFromAssemblyContaining<T>(this IServiceCollection services)
    {
        return services.AddRepositoriesFromAssembly(typeof(T).Assembly);
    }

    private static bool IsRepositoryInterface(Type type)
    {
        if (!type.IsInterface)
            return false;

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IReadRepository<,>) || def == typeof(IRepository<,>))
                return true;
        }

        return type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() is var g &&
            (g == typeof(IReadRepository<,>) || g == typeof(IRepository<,>)));
    }
}
