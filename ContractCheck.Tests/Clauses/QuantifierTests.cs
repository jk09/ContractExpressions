#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.Quantifiers;

/// <summary>
/// <c>Contract.ForAll</c> and <c>Contract.Exists</c> – both overloads each – inside preconditions and postconditions,
/// ported from the quantifier cases of CodeContracts Foxtrot/Tests/RewrittenContractTest.cs.
/// </summary>
public class QuantifierTests
{
    [Fact]
    public void ForAll_over_an_index_range_in_a_precondition_ported_from_RequiresForAll()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        proxy.RequiresForAll([3, 4, 5]);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.RequiresForAll([3, -4, 5]));
    }

    [Fact]
    public void Exists_over_an_index_range_in_a_precondition_ported_from_RequiresExists()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        proxy.RequiresExists([-3, -4, 5]);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.RequiresExists([-3, -4, -5]));
    }

    [Fact]
    public void ForAll_over_the_result_in_a_postcondition_ported_from_EnsuresForAll()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        proxy.Copy([3, 4, 5], shouldBeCorrect: true);
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.Copy([3, -4, 5], shouldBeCorrect: false));
    }

    [Fact]
    public void Exists_over_the_result_in_a_postcondition_ported_from_EnsuresExists()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        proxy.CopyOne([-3, -4, 5], shouldBeCorrect: true);
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.CopyOne([-3, -4, -5], shouldBeCorrect: false));
    }

    [Fact]
    public void Nested_quantifiers_ported_from_EnsuresDoubleClosure()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        int[] same = [3, 4, 5];
        proxy.Contained(same, same, shouldBeCorrect: true);
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.Contained([3, 4, 5], [3, 4], shouldBeCorrect: false));
    }

    [Fact]
    public void Result_inside_nested_quantifiers_ported_from_EnsuresWithResultAndQuantifiers()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        Assert.Equal(1, proxy.Maximum([-3, -4, -5], beCorrect: true));
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.Maximum([-3, -4, -5], beCorrect: false));
    }

    [Fact]
    public void ForAll_and_Exists_over_a_collection()
    {
        IQuantified proxy = Dbc.Make<IQuantified>(new Quantified());

        Assert.Equal(9, proxy.Max([3, -2, 9, 1]));
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.Max([]));
    }
}

[ContractClass(typeof(QuantifiedContracts))]
public interface IQuantified
{
    void RequiresForAll(int[] xs);

    void RequiresExists(int[] xs);

    int[] Copy(int[] xs, bool shouldBeCorrect);

    int[] CopyOne(int[] xs, bool shouldBeCorrect);

    void Contained(int[] xs, int[] ys, bool shouldBeCorrect);

    int Maximum(int[] xs, bool beCorrect);

    int Max(int[] elements);
}

public sealed class Quantified : IQuantified
{
    public void RequiresForAll(int[] xs)
    {
    }

    public void RequiresExists(int[] xs)
    {
    }

    public int[] Copy(int[] xs, bool shouldBeCorrect)
    {
        int[] ys = [.. xs];
        if (!shouldBeCorrect && xs.Length > 0)
        {
            ys[^1]++;
        }

        return ys;
    }

    public int[] CopyOne(int[] xs, bool shouldBeCorrect)
    {
        int[] ys = [.. xs.Select(x => x + 1)];
        if (shouldBeCorrect && xs.Length > 0)
        {
            ys[^1] = xs[^1];
        }

        return ys;
    }

    public void Contained(int[] xs, int[] ys, bool shouldBeCorrect)
    {
        if (!shouldBeCorrect)
        {
            xs[^1]++;
        }
    }

    public int Maximum(int[] xs, bool beCorrect)
    {
        if (!beCorrect)
        {
            xs[^1]++;
        }

        int r = xs.Max();
        return !beCorrect ? xs[0] - 1 : r < 0 ? 1 : r;
    }

    public int Max(int[] elements) => elements.Length == 0 ? int.MinValue : elements.Max();
}

[ContractClassFor(typeof(IQuantified))]
internal sealed class QuantifiedContracts
{
    public QuantifiedContracts()
    {
        Dbc.Def(static (IQuantified x, int[] xs) => x.RequiresForAll(xs),
            static (IQuantified x, int[] xs) => Contract.Requires(Contract.ForAll(0, xs.Length, i => xs[i] > 0)));

        Dbc.Def(static (IQuantified x, int[] xs) => x.RequiresExists(xs),
            static (IQuantified x, int[] xs) => Contract.Requires(Contract.Exists(0, xs.Length, i => xs[i] > 0)));

        Dbc.Def(static (IQuantified x, int[] xs, bool shouldBeCorrect) => x.Copy(xs, shouldBeCorrect),
            static (IQuantified x, int[] xs, bool shouldBeCorrect) => Contract.Ensures(Contract.ForAll(0, xs.Length, i => xs[i] == Contract.Result<int[]>()[i])));

        Dbc.Def(static (IQuantified x, int[] xs, bool shouldBeCorrect) => x.CopyOne(xs, shouldBeCorrect),
            static (IQuantified x, int[] xs, bool shouldBeCorrect) => Contract.Ensures(Contract.Exists(0, xs.Length, i => xs[i] == Contract.Result<int[]>()[i])));

        Dbc.Def(static (IQuantified x, int[] xs, int[] ys, bool shouldBeCorrect) => x.Contained(xs, ys, shouldBeCorrect),
            static (IQuantified x, int[] xs, int[] ys, bool shouldBeCorrect) => Contract.Ensures(Contract.ForAll(0, xs.Length, i => Contract.Exists(0, ys.Length, j => xs[i] == ys[j]))));

        Dbc.Def(static (IQuantified x, int[] xs, bool beCorrect) => x.Maximum(xs, beCorrect),
            static (IQuantified x, int[] xs, bool beCorrect) => Contract.Requires(xs.Length > 0),
            static (IQuantified x, int[] xs, bool beCorrect) => Contract.Ensures(Contract.Result<int>() > 0),
            static (IQuantified x, int[] xs, bool beCorrect) => Contract.Ensures(Contract.ForAll(0, xs.Length, i =>
                Contract.Result<int>() > 0 && (i == 0 || Contract.Exists(0, i, j => Contract.Result<int>() >= xs[i])))));

        Dbc.Def(static (IQuantified x, int[] elements) => x.Max(elements),
            static (IQuantified x, int[] elements) => Contract.Requires(elements != null),
            static (IQuantified x, int[] elements) => Contract.Ensures(Contract.ForAll(elements, e => e <= Contract.Result<int>())),
            static (IQuantified x, int[] elements) => Contract.Ensures(Contract.Exists(elements, e => e == Contract.Result<int>())));
    }
}
