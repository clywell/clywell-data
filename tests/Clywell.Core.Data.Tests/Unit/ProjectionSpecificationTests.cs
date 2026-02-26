namespace Clywell.Core.Data.Tests.Unit;

/// <summary>
/// Tests for <see cref="Specification{T, TResult}"/> (projection specification).
/// </summary>
public class ProjectionSpecificationTests
{
    // ============================================================
    // Test Types
    // ============================================================

    private sealed class TestEntity : IEntity<Guid>
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    private sealed record TestDto(Guid Id, string Name);

    // ============================================================
    // Test Specifications
    // ============================================================

    private sealed class ProjectionSpec : Specification<TestEntity, TestDto>
    {
        public ProjectionSpec()
        {
            Select(e => new TestDto(e.Id, e.Name));
            AsReadOnly();
        }
    }

    private sealed class FilteredProjectionSpec : Specification<TestEntity, TestDto>
    {
        public FilteredProjectionSpec(string status)
        {
            Where(e => e.Status == status);
            OrderByDescending(e => e.CreatedAt);
            Select(e => new TestDto(e.Id, e.Name));
            AsReadOnly();
        }
    }

    private sealed class PagedProjectionSpec : Specification<TestEntity, TestDto>
    {
        public PagedProjectionSpec(int skip, int take)
        {
            Select(e => new TestDto(e.Id, e.Name));
            ApplyPaging(skip, take);
        }
    }

    private sealed class NoSelectorSpec : Specification<TestEntity, TestDto>;

    // ============================================================
    // Tests
    // ============================================================

    [Fact]
    public void ProjectionSpec_ShouldHaveSelector()
    {
        var spec = new ProjectionSpec();

        Assert.NotNull(spec.Selector);
        Assert.True(spec.IsReadOnly);
    }

    [Fact]
    public void ProjectionSpec_SelectorShouldCompile()
    {
        var spec = new ProjectionSpec();
        var compiled = spec.Selector!.Compile();
        var id = Guid.NewGuid();

        var entity = new TestEntity { Id = id, Name = "Test", Description = "Desc", Status = "Active" };
        var dto = compiled(entity);

        Assert.Equal(id, dto.Id);
        Assert.Equal("Test", dto.Name);
    }

    [Fact]
    public void FilteredProjectionSpec_ShouldCombineFiltersAndProjection()
    {
        var spec = new FilteredProjectionSpec("Active");

        Assert.Single(spec.Criteria);
        Assert.Single(spec.OrderExpressions);
        Assert.NotNull(spec.Selector);
        Assert.True(spec.IsReadOnly);
    }

    [Fact]
    public void PagedProjectionSpec_ShouldHavePagingAndSelector()
    {
        var spec = new PagedProjectionSpec(0, 20);

        Assert.NotNull(spec.Selector);
        Assert.Equal(0, spec.Skip);
        Assert.Equal(20, spec.Take);
    }

    [Fact]
    public void NoSelectorSpec_ShouldHaveNullSelector()
    {
        var spec = new NoSelectorSpec();

        Assert.Null(spec.Selector);
    }
}
