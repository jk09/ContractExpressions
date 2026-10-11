#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.OldValue;

/// <summary>
/// <c>Contract.OldValue(expression)</c> (resolution R5): any expression over the target and the arguments, captured after the
/// preconditions and before the call. ContractExpressions only supported <c>OldValue(x.Property)</c> (probes S04–S06).
/// </summary>
public class OldValueTests
{
    [Fact]
    public void Property_old_value_ported_from_StackContractTests()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        proxy.Add(2);
        proxy.Add(3);

        Assert.Equal(5, proxy.Count);
    }

    [Fact]
    public void Two_clauses_may_read_the_same_old_value()
    {
        // ContractExpressions failed to register this ("An item with the same key has already been added", probe S04).
        Assert.Equal(2, Dbc.Make<ICounter>(new Counter()).Add(2));
    }

    [Fact]
    public void Old_value_of_an_argument_member()
    {
        // ContractExpressions failed: "Property 'Count' is not defined for type ..." (probe S05).
        var source = new List<int> { 1, 2, 3 };

        Dbc.Make<ICounter>(new Counter()).Drain(source);

        Assert.Empty(source);
    }

    [Fact]
    public void Old_value_of_a_composite_expression()
    {
        // ContractExpressions failed: "Cannot handle old value expression (x.Count + 1)" (probe S06).
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        proxy.Increment();

        Assert.Equal(1, proxy.Count);
    }

    [Fact]
    public void Old_value_detects_wrong_state_change()
    {
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<ICounter>(new Counter { Broken = true }).Add(2));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Old_value_of_an_argument_ported_from_EnsuresAnything(bool anything)
    {
        Dbc.Make<ICounter>(new Counter()).Anything(anything);
    }

    [Fact]
    public void Snapshot_of_a_collection_compares_elements_across_the_call()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        proxy.Reverse([1, 2, 3]);
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<ICounter>(new Counter { Broken = true }).Reverse([1, 2, 3]));
    }

    [Fact]
    public void Old_value_that_cannot_be_captured_is_harmless_when_a_guard_skips_it()
    {
        // OldValue(xs.Length) throws for xs == null at entry, but the guard means the clause never reads it.
        Dbc.Make<ICounter>(new Counter()).Measure(null);
        Dbc.Make<ICounter>(new Counter()).Measure([1, 2]);
    }

    [Fact]
    public void Old_value_that_cannot_be_captured_rethrows_its_exception_when_read()
    {
        NullReferenceException exception = Assert.Throws<NullReferenceException>(() => Dbc.Make<ICounter>(new Counter()).MeasureUnguarded(null));

        Assert.Equal("Contract.Ensures(x.Count >= Contract.OldValue(xs.Length) - xs.Length)", exception.Data["ContractCheck.Clause"]);
    }

    [Fact]
    public void Old_value_is_captured_only_after_the_preconditions_hold()
    {
        int before = CounterContracts.Captures;

        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<ICounter>(new Counter()).Add(-1));

        Assert.Equal(before, CounterContracts.Captures);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Old_value_inside_an_index_quantifier_ported_from_EnsuresForAllWithOld(bool shouldBeCorrect)
    {
        // CodeContracts Foxtrot/Tests/RewrittenContractTest.cs: ForAll(0, xs.Length, i => xs[i] == OldValue(xs[i])).
        IArrays proxy = Dbc.Make<IArrays>(new Arrays());

        if (shouldBeCorrect)
        {
            proxy.KeepAll([3, 4, 5], shouldBeCorrect: true);
        }
        else
        {
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.KeepAll([3, -4, 5], shouldBeCorrect: false));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Old_value_inside_an_index_Exists_ported_from_EnsuresExistsWithOld(bool shouldBeCorrect)
    {
        IArrays proxy = Dbc.Make<IArrays>(new Arrays());

        if (shouldBeCorrect)
        {
            proxy.KeepOne([-3, -4, 5], shouldBeCorrect: true);
        }
        else
        {
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.KeepOne([-3, -4, -5], shouldBeCorrect: false));
        }
    }

    [Fact]
    public void Old_value_of_an_index_that_did_not_exist_at_entry_is_reported()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Dbc.Make<IArrays>(new Arrays()).Grow());

        Assert.Contains("has no old value", exception.Message, StringComparison.Ordinal);
    }
}

[ContractClass(typeof(CounterContracts))]
public interface ICounter
{
    int Count { get; }

    int Add(int value);

    void Increment();

    void Drain(List<int> source);

    void Anything(bool anything);

    void Reverse(int[] values);

    void Measure(int[]? xs);

    void MeasureUnguarded(int[]? xs);
}

public sealed class Counter : ICounter
{
    public bool Broken { get; init; }

    public int Count { get; private set; }

    public int Add(int value) => Count += Broken ? value + 1 : value;

    public void Increment() => Count++;

    public void Drain(List<int> source)
    {
        Count += source.Count;
        source.Clear();
    }

    public void Anything(bool anything)
    {
    }

    public void Reverse(int[] values)
    {
        if (!Broken)
        {
            Array.Reverse(values);
        }
    }

    public void Measure(int[]? xs)
    {
    }

    public void MeasureUnguarded(int[]? xs)
    {
    }
}

[ContractClassFor(typeof(ICounter))]
internal sealed class CounterContracts
{
    private static int captures;

    public CounterContracts()
    {
        Dbc.Def(static (ICounter x, int value) => x.Add(value),
            static (ICounter x, int value) => Contract.Requires(value > 0),
            static (ICounter x, int value) => Contract.Ensures(x.Count == Contract.OldValue(Captured(x.Count)) + value),
            static (ICounter x, int value) => Contract.Ensures(Contract.Result<int>() == Contract.OldValue(x.Count) + value));

        Dbc.Def(static (ICounter x) => x.Increment(),
            static (ICounter x) => Contract.Ensures(x.Count == Contract.OldValue(x.Count + 1)));

        Dbc.Def(static (ICounter x, List<int> source) => x.Drain(source),
            static (ICounter x, List<int> source) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + Contract.OldValue(source.Count)),
            static (ICounter x, List<int> source) => Contract.Ensures(source.Count == 0));

        Dbc.Def(static (ICounter x, bool anything) => x.Anything(anything),
            static (ICounter x, bool anything) => Contract.Ensures(anything == Contract.OldValue(anything)));

        Dbc.Def(static (ICounter x, int[] values) => x.Reverse(values),
            static (ICounter x, int[] values) => Contract.Ensures(Contract.ForAll(0, values.Length, i => values[i] == Contract.OldValue(values.ToArray())[values.Length - 1 - i])));

        Dbc.Def(static (ICounter x, int[]? xs) => x.Measure(xs),
            static (ICounter x, int[]? xs) => Contract.Ensures(xs == null || x.Count >= Contract.OldValue(xs.Length) - xs.Length));

        Dbc.Def(static (ICounter x, int[]? xs) => x.MeasureUnguarded(xs),
            static (ICounter x, int[]? xs) => Contract.Ensures(x.Count >= Contract.OldValue(xs!.Length) - xs.Length));
    }

    public static int Captures => Volatile.Read(ref captures);

    private static int Captured(int value)
    {
        Interlocked.Increment(ref captures);
        return value;
    }
}

[ContractClass(typeof(ArraysContracts))]
public interface IArrays
{
    void KeepAll(int[] xs, bool shouldBeCorrect);

    void KeepOne(int[] xs, bool shouldBeCorrect);

    void Grow();

    int[] Values { get; }
}

public sealed class Arrays : IArrays
{
    public int[] Values { get; private set; } = [1, 2];

    public void KeepAll(int[] xs, bool shouldBeCorrect)
    {
        if (!shouldBeCorrect)
        {
            xs[^1]++;
        }
    }

    public void KeepOne(int[] xs, bool shouldBeCorrect)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            xs[i]++;
        }

        if (shouldBeCorrect)
        {
            xs[^1]--;
        }
    }

    public void Grow() => Values = [.. Values, 3];
}

[ContractClassFor(typeof(IArrays))]
internal sealed class ArraysContracts
{
    public ArraysContracts()
    {
        Dbc.Def(static (IArrays x, int[] xs, bool shouldBeCorrect) => x.KeepAll(xs, shouldBeCorrect),
            static (IArrays x, int[] xs, bool shouldBeCorrect) => Contract.Requires(xs != null && xs.Length > 0),
            static (IArrays x, int[] xs, bool shouldBeCorrect) => Contract.Ensures(Contract.ForAll(0, xs.Length, i => xs[i] == Contract.OldValue(xs[i]))));

        Dbc.Def(static (IArrays x, int[] xs, bool shouldBeCorrect) => x.KeepOne(xs, shouldBeCorrect),
            static (IArrays x, int[] xs, bool shouldBeCorrect) => Contract.Requires(xs != null && xs.Length > 0),
            static (IArrays x, int[] xs, bool shouldBeCorrect) => Contract.Ensures(Contract.Exists(0, xs.Length, i => xs[i] == Contract.OldValue(xs[i]))));

        // The range is captured at entry (0..1); after Grow index 2 has no old value.
        Dbc.Def(static (IArrays x) => x.Grow(),
            static (IArrays x) => Contract.Ensures(Contract.ForAll(0, x.Values.Length, i => x.Values[i] == Contract.OldValue(x.Values[i]))));
    }
}
