namespace Clywell.Core.Data.Tests.Unit;

/// <summary>
/// Tests for <see cref="OrderExpression{T}"/> and <see cref="IncludeExpression"/>.
/// </summary>
public class ExpressionTypeTests
{
    // ============================================================
    // Test Entity
    // ============================================================

    private sealed class TestEntity : IEntity<Guid>
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    // ============================================================
    // OrderExpression Tests
    // ============================================================

    [Fact]
    public void OrderExpression_Ascending_ShouldNotBeDescending()
    {
        var order = new OrderExpression<TestEntity>(x => x.Name, Descending: false);

        Assert.False(order.Descending);
        Assert.NotNull(order.KeySelector);
    }

    [Fact]
    public void OrderExpression_Descending_ShouldBeDescending()
    {
        var order = new OrderExpression<TestEntity>(x => x.Name, Descending: true);

        Assert.True(order.Descending);
    }

    [Fact]
    public void OrderExpression_RecordEquality_ShouldWork()
    {
        System.Linq.Expressions.Expression<Func<TestEntity, object?>> selector = x => x.Name;
        var order1 = new OrderExpression<TestEntity>(selector, Descending: true);
        var order2 = new OrderExpression<TestEntity>(selector, Descending: true);

        Assert.Equal(order1, order2);
    }

    // ============================================================
    // IncludeExpression Tests
    // ============================================================

    [Fact]
    public void IncludeExpression_TopLevel_ShouldHaveNoPrevious()
    {
        System.Linq.Expressions.Expression<Func<TestEntity, string>> expr = x => x.Name;
        var include = new IncludeExpression(expr, null, IncludeType.Include);

        Assert.Equal(IncludeType.Include, include.Type);
        Assert.Null(include.PreviousExpression);
        Assert.NotNull(include.Expression);
    }

    [Fact]
    public void IncludeExpression_ThenInclude_ShouldHavePrevious()
    {
        System.Linq.Expressions.Expression<Func<TestEntity, string>> expr1 = x => x.Name;
        System.Linq.Expressions.Expression<Func<string, int>> expr2 = s => s.Length;
        var include = new IncludeExpression(expr2, expr1, IncludeType.ThenInclude);

        Assert.Equal(IncludeType.ThenInclude, include.Type);
        Assert.NotNull(include.PreviousExpression);
    }

    // ============================================================
    // IncludeType Tests
    // ============================================================

    [Fact]
    public void IncludeType_ShouldHaveTwoValues()
    {
        var values = Enum.GetValues<IncludeType>();

        Assert.Equal(2, values.Length);
        Assert.Contains(IncludeType.Include, values);
        Assert.Contains(IncludeType.ThenInclude, values);
    }
}
