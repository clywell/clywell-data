using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// Extension methods for <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Registers the <see cref="DomainEventDispatchInterceptor"/> on the DbContext options.
    /// Call this from your <c>IDbContextOptionsConfiguration&lt;T&gt;</c> implementation.
    /// </summary>
    public static DbContextOptionsBuilder UseDomainEventDispatchInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        return optionsBuilder.AddInterceptors(interceptor);
    }
}