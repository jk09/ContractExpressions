#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractCheck.Tests.Definitions.Printing;

/// <summary>Condition text in failure messages reads like the C# that was written.</summary>
public class ExpressionPrinterTests
{
    [Fact]
    public void Prints_comparisons_and_member_access() =>
        Assert.Equal("x.Count == 1 + Contract.OldValue(x.Count)", Body((IShape x, int v) => Contract.Ensures(x.Count == 1 + Contract.OldValue(x.Count))));

    [Fact]
    public void Prints_Result_with_its_type_argument() =>
        Assert.Equal("Contract.Result<int>() >= 0 && Contract.Result<int>() < x.Count", Body((IShape x, int v) => Contract.Ensures(Contract.Result<int>() >= 0 && Contract.Result<int>() < x.Count)));

    [Fact]
    public void Adds_parentheses_only_where_precedence_needs_them() =>
        Assert.Equal("(v + 1) * 2 == v - (1 - v)", Body((IShape x, int v) => Contract.Requires((v + 1) * 2 == v - (1 - v))));

    [Fact]
    public void Drops_the_conversions_the_compiler_inserts() =>
        Assert.Equal("x.Payload != null && x.Long == v", Body((IShape x, int v) => Contract.Requires(x.Payload != null && x.Long == v)));

    [Fact]
    public void Keeps_explicit_narrowing_casts() =>
        Assert.Equal("(int)x.Long == v", Body((IShape x, int v) => Contract.Requires((int)x.Long == v)));

    [Fact]
    public void Prints_lambdas_and_quantifiers() =>
        Assert.Equal("Contract.ForAll(0, x.Count, i => x[i] != null)", Body((IShape x, int v) => Contract.Requires(Contract.ForAll(0, x.Count, i => x[i] != null))));

    [Fact]
    public void Prints_extension_and_static_calls() =>
        Assert.Equal("x.Names.Any(n => string.IsNullOrEmpty(n)) || Math.Abs(v) > 1", Body((IShape x, int v) => Contract.Requires(x.Names.Any(n => string.IsNullOrEmpty(n)) || Math.Abs(v) > 1)));

    [Fact]
    public void Prints_constants_as_the_compiler_inlined_them() =>
        Assert.Equal("v > -2147483648", Body((IShape x, int v) => Contract.Requires(v > int.MinValue)));

    [Fact]
    public void Prints_conditional_expressions() =>
        Assert.Equal("v == 0 ? Contract.Result<int>() == v : Contract.Result<int>() >= 0", Body((IShape x, int v) => Contract.Ensures(v == 0 ? Contract.Result<int>() == v : Contract.Result<int>() >= 0)));

    [Fact]
    public void Prints_enum_comparisons_with_enum_names() =>
        Assert.Equal("x.Kind == ShapeKind.Circle", Body((IShape x, int v) => Contract.Requires(x.Kind == ShapeKind.Circle)));

    [Fact]
    public void Escapes_string_and_char_literals() =>
        Assert.Equal("x.Name != \"a\\\"b\\n\" && x.Name[0] != '\\''", Body((IShape x, int v) => Contract.Requires(x.Name != "a\"b\n" && x.Name[0] != '\'')));

    [Fact]
    public void Prints_typeof_is_and_array_creation() =>
        Assert.Equal("x.Payload is string && new[] { 1, v }.Length == 2 && x.GetType() != typeof(int)", Body((IShape x, int v) => Contract.Requires(x.Payload is string && new[] { 1, v }.Length == 2 && x.GetType() != typeof(int))));

    [Fact]
    public void Prints_captured_variables_by_name()
    {
        int limit = 3;
        Expression<Action<IShape, int>> clause = (IShape x, int v) => Contract.Requires(v < limit);

        Assert.Equal("v < limit", ExpressionPrinter.Print(((MethodCallExpression)clause.Body).Arguments[0]));
    }

    [Fact]
    public void Prints_whole_clauses_with_messages() =>
        Assert.Equal("Contract.Requires<ArgumentNullException>(x.Payload != null, \"item\")", ExpressionPrinter.Print(((Expression<Action<IShape, int>>)((IShape x, int v) => Contract.Requires<ArgumentNullException>(x.Payload != null, "item"))).Body));

    private static string Body(Expression<Action<IShape, int>> clause) =>
        ExpressionPrinter.Print(((MethodCallExpression)clause.Body).Arguments[0]);
}

public enum ShapeKind
{
    Square,
    Circle,
}

public interface IShape
{
    int Count { get; }

    long Long { get; }

    object? Payload { get; }

    string Name { get; }

    string[] Names { get; }

    ShapeKind Kind { get; }

    object? this[int index] { get; }
}
