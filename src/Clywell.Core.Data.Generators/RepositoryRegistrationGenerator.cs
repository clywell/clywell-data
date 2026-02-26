using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Clywell.Core.Data.Generators;

/// <summary>
/// Roslyn incremental source generator that scans a compilation for concrete repository
/// implementations and emits a compile-time <c>AddRepositories()</c> DI extension method,
/// replacing reflection-based assembly scanning at startup.
/// </summary>
/// <remarks>
/// <para>
/// The generator detects any non-abstract class whose interface hierarchy includes
/// <c>Clywell.Core.Data.IRepository&lt;,&gt;</c> or <c>Clywell.Core.Data.IReadRepository&lt;,&gt;</c>
/// and emits the corresponding <c>TryAddScoped</c> registrations at compile time.
/// </para>
/// <para>
/// The emitted <c>AddRepositories()</c> method lives in a <c>RepositoryRegistrationExtensions</c>
/// class generated inside the consuming project's root namespace, so it requires no extra
/// <c>using</c> directive beyond what the host project already has.
/// </para>
/// <para>
/// Usage in the host project's DI setup:
/// <code>
/// services.AddDataAccess&lt;AppDbContext&gt;();
/// services.AddRepositories(); // generated — zero reflection
/// </code>
/// </para>
/// </remarks>
[Generator]
public sealed class RepositoryRegistrationGenerator : IIncrementalGenerator
{
    private const string IRepositoryMetadataName = "Clywell.Core.Data.IRepository`2";
    private const string IReadRepositoryMetadataName = "Clywell.Core.Data.IReadRepository`2";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: resolve the two base repository interfaces from the compilation
        var baseInterfaces = context.CompilationProvider.Select(
            static (compilation, _) => GetBaseInterfaces(compilation));

        // Step 2: find candidate types — any class declaration in the user's source
        var classSyntax = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c
                    && c.BaseList is not null
                    && c.BaseList.Types.Count > 0,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static c => c is not null);

        // Step 3: combine each class with its semantic model and the base interface symbols
        var combined = classSyntax
            .Combine(context.CompilationProvider)
            .Combine(baseInterfaces);

        // Step 4: extract registration info (interface → implementation pairs)
        var registrations = combined
            .Select(static (pair, _) =>
            {
                var ((classSyntax, compilation), bases) = pair;
                return ExtractRegistrations(classSyntax, compilation, bases);
            })
            .Where(static r => r.Count > 0);

        // Step 5: collect all registrations and emit a single source file
        var allRegistrations = registrations.Collect();

        context.RegisterSourceOutput(
            allRegistrations.Combine(context.CompilationProvider),
            static (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }

    // ============================================================
    // Symbol helpers
    // ============================================================

    private static (INamedTypeSymbol? IRepository, INamedTypeSymbol? IReadRepository) GetBaseInterfaces(
        Compilation compilation)
    {
        return (
            compilation.GetTypeByMetadataName(IRepositoryMetadataName),
            compilation.GetTypeByMetadataName(IReadRepositoryMetadataName));
    }

    private static IReadOnlyList<RegistrationInfo> ExtractRegistrations(
        ClassDeclarationSyntax classSyntax,
        Compilation compilation,
        (INamedTypeSymbol? IRepository, INamedTypeSymbol? IReadRepository) bases)
    {
        if (bases.IRepository is null && bases.IReadRepository is null)
            return ImmutableArray<RegistrationInfo>.Empty;

        var model = compilation.GetSemanticModel(classSyntax.SyntaxTree);
        if (model.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classSymbol)
            return ImmutableArray<RegistrationInfo>.Empty;

        // Skip abstract, generic, and non-class types
        if (classSymbol.IsAbstract || classSymbol.IsGenericType
            || classSymbol.TypeKind != TypeKind.Class)
            return ImmutableArray<RegistrationInfo>.Empty;

        var results = new List<RegistrationInfo>();

        foreach (var iface in classSymbol.AllInterfaces)
        {
            // We only register user-declared repository interfaces (not the base ones directly)
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, bases.IRepository)
                || SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, bases.IReadRepository))
                continue;

            // The interface must itself inherit from IRepository<,> or IReadRepository<,>
            if (!IsRepositoryInterface(iface, bases))
                continue;

            results.Add(new RegistrationInfo(
                InterfaceFullName: iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ImplementationFullName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        return results;
    }

    private static bool IsRepositoryInterface(
        INamedTypeSymbol iface,
        (INamedTypeSymbol? IRepository, INamedTypeSymbol? IReadRepository) bases)
    {
        foreach (var inherited in iface.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(inherited.OriginalDefinition, bases.IRepository)
                || SymbolEqualityComparer.Default.Equals(inherited.OriginalDefinition, bases.IReadRepository))
                return true;
        }

        return false;
    }

    // ============================================================
    // Code emission
    // ============================================================

    private static void Emit(
        SourceProductionContext spc,
        ImmutableArray<IReadOnlyList<RegistrationInfo>> allGroups,
        Compilation compilation)
    {
        // Flatten and deduplicate registrations across all classes
        var seen = new HashSet<string>();
        var registrations = new List<RegistrationInfo>();

        foreach (var group in allGroups)
        {
            foreach (var info in group)
            {
                var key = $"{info.InterfaceFullName}|{info.ImplementationFullName}";
                if (seen.Add(key))
                    registrations.Add(info);
            }
        }

        if (registrations.Count == 0)
            return;

        // Resolve root namespace from assembly name, falling back to a fixed namespace
        var rootNamespace = compilation.AssemblyName ?? "Clywell.Core.Data.EntityFramework";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Source-generated repository DI registrations.");
        sb.AppendLine("    /// Replaces <c>AddRepositoriesFromAssembly()</c> with zero reflection.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class RepositoryRegistrationExtensions");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Registers all detected repository implementations as scoped services.");
        sb.AppendLine("        /// Generated at compile time — no reflection, NativeAOT compatible.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static IServiceCollection AddRepositories(");
        sb.AppendLine("            this IServiceCollection services)");
        sb.AppendLine("        {");

        foreach (var reg in registrations)
        {
            sb.AppendLine(
                $"            services.TryAddScoped<{reg.InterfaceFullName}, {reg.ImplementationFullName}>();");
        }

        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource(
            "RepositoryRegistrationExtensions.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ============================================================
    // Data
    // ============================================================

    private readonly record struct RegistrationInfo(
        string InterfaceFullName,
        string ImplementationFullName);
}
