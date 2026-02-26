namespace Clywell.Core.Data.Tests.Unit;

/// <summary>
/// Tests for <see cref="Specification{T}"/>.
/// </summary>
public class SpecificationTests
{
    // ============================================================
    // Test Entity
    // ============================================================

    private sealed class TestEntity : IEntity<Guid>
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public int Priority { get; init; }
    }

    private sealed class RelatedEntity
    {
        public Guid Id { get; init; }

        public string Value { get; init; } = string.Empty;
    }

    private sealed class TestEntityWithNav : IEntity<Guid>
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public RelatedEntity? Related { get; init; }

        public ICollection<RelatedEntity> Items { get; init; } = [];
    }

    // ============================================================
    // Test Specifications
    // ============================================================

    private sealed class EmptySpec : Specification<TestEntity>;

    private sealed class SingleCriteriaSpec : Specification<TestEntity>
    {
        public SingleCriteriaSpec(string status)
        {
            Where(t => t.Status == status);
        }
    }

    private sealed class MultipleCriteriaSpec : Specification<TestEntity>
    {
        public MultipleCriteriaSpec(string status, int minPriority)
        {
            Where(t => t.Status == status);
            Where(t => t.Priority >= minPriority);
        }
    }

    private sealed class OrderedSpec : Specification<TestEntity>
    {
        public OrderedSpec()
        {
            OrderBy(t => t.Name);
        }
    }

    private sealed class OrderedDescSpec : Specification<TestEntity>
    {
        public OrderedDescSpec()
        {
            OrderByDescending(t => t.CreatedAt);
        }
    }

    private sealed class MultiOrderSpec : Specification<TestEntity>
    {
        public MultiOrderSpec()
        {
            OrderBy(t => t.Status);
            OrderByDescending(t => t.Priority);
        }
    }

    private sealed class PagedSpec : Specification<TestEntity>
    {
        public PagedSpec(int skip, int take)
        {
            ApplyPaging(skip, take);
        }
    }

    private sealed class ReadOnlySpec : Specification<TestEntity>
    {
        public ReadOnlySpec()
        {
            Where(t => t.Status == "Active");
            AsReadOnly();
        }
    }

    private sealed class IncludeStringSpec : Specification<TestEntityWithNav>
    {
        public IncludeStringSpec()
        {
            Include("Related");
        }
    }

    private sealed class IncludeExpressionSpec : Specification<TestEntityWithNav>
    {
        public IncludeExpressionSpec()
        {
            Include(x => x.Related!);
        }
    }

    private sealed class FullSpec : Specification<TestEntity>
    {
        public FullSpec(string status, int page, int pageSize)
        {
            Where(t => t.Status == status);
            OrderByDescending(t => t.CreatedAt);
            ApplyPaging((page - 1) * pageSize, pageSize);
            AsReadOnly();
        }
    }

    // ============================================================
    // Empty Specification Tests
    // ============================================================

    [Fact]
    public void EmptySpec_ShouldHaveNoCriteria()
    {
        var spec = new EmptySpec();

        Assert.Empty(spec.Criteria);
        Assert.Empty(spec.OrderExpressions);
        Assert.Empty(spec.IncludeExpressions);
        Assert.Empty(spec.IncludeStrings);
        Assert.Null(spec.Skip);
        Assert.Null(spec.Take);
        Assert.False(spec.IsReadOnly);
    }

    // ============================================================
    // Criteria Tests
    // ============================================================

    [Fact]
    public void SingleCriteria_ShouldAddOnePredicate()
    {
        var spec = new SingleCriteriaSpec("Active");

        Assert.Single(spec.Criteria);
    }

    [Fact]
    public void MultipleCriteria_ShouldAddAllPredicates()
    {
        var spec = new MultipleCriteriaSpec("Active", 3);

        Assert.Equal(2, spec.Criteria.Count);
    }

    [Fact]
    public void Criteria_ShouldCompileAndEvaluateCorrectly()
    {
        var spec = new SingleCriteriaSpec("Active");
        var compiled = spec.Criteria[0].Compile();

        var activeEntity = new TestEntity { Status = "Active" };
        var closedEntity = new TestEntity { Status = "Closed" };

        Assert.True(compiled(activeEntity));
        Assert.False(compiled(closedEntity));
    }

    // ============================================================
    // Ordering Tests
    // ============================================================

    [Fact]
    public void OrderBy_ShouldAddAscendingOrder()
    {
        var spec = new OrderedSpec();

        Assert.Single(spec.OrderExpressions);
        Assert.False(spec.OrderExpressions[0].Descending);
    }

    [Fact]
    public void OrderByDescending_ShouldAddDescendingOrder()
    {
        var spec = new OrderedDescSpec();

        Assert.Single(spec.OrderExpressions);
        Assert.True(spec.OrderExpressions[0].Descending);
    }

    [Fact]
    public void MultipleOrders_ShouldPreserveInsertionOrder()
    {
        var spec = new MultiOrderSpec();

        Assert.Equal(2, spec.OrderExpressions.Count);
        Assert.False(spec.OrderExpressions[0].Descending); // OrderBy
        Assert.True(spec.OrderExpressions[1].Descending);   // OrderByDescending
    }

    // ============================================================
    // Paging Tests
    // ============================================================

    [Fact]
    public void ApplyPaging_ShouldSetSkipAndTake()
    {
        var spec = new PagedSpec(10, 25);

        Assert.Equal(10, spec.Skip);
        Assert.Equal(25, spec.Take);
    }

    [Fact]
    public void ApplyPaging_WithNegativeSkip_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedSpec(-1, 10));
    }

    [Fact]
    public void ApplyPaging_WithZeroTake_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedSpec(0, 0));
    }

    [Fact]
    public void ApplyPaging_WithZeroSkip_ShouldBeValid()
    {
        var spec = new PagedSpec(0, 10);

        Assert.Equal(0, spec.Skip);
        Assert.Equal(10, spec.Take);
    }

    // ============================================================
    // ReadOnly Tests
    // ============================================================

    [Fact]
    public void AsReadOnly_ShouldSetFlag()
    {
        var spec = new ReadOnlySpec();

        Assert.True(spec.IsReadOnly);
    }

    // ============================================================
    // Include Tests
    // ============================================================

    [Fact]
    public void IncludeString_ShouldAddPath()
    {
        var spec = new IncludeStringSpec();

        Assert.Single(spec.IncludeStrings);
        Assert.Equal("Related", spec.IncludeStrings[0]);
    }

    [Fact]
    public void IncludeExpression_ShouldAddExpression()
    {
        var spec = new IncludeExpressionSpec();

        Assert.Single(spec.IncludeExpressions);
        Assert.Equal(IncludeType.Include, spec.IncludeExpressions[0].Type);
        Assert.Null(spec.IncludeExpressions[0].PreviousExpression);
    }

    // ============================================================
    // Full Specification Tests
    // ============================================================

    [Fact]
    public void FullSpec_ShouldCombineAllFeatures()
    {
        var spec = new FullSpec("Active", 2, 10);

        Assert.Single(spec.Criteria);
        Assert.Single(spec.OrderExpressions);
        Assert.True(spec.OrderExpressions[0].Descending);
        Assert.Equal(10, spec.Skip);  // (2-1) * 10
        Assert.Equal(10, spec.Take);
        Assert.True(spec.IsReadOnly);
    }
}
