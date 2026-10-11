#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Order;

/// <summary>
/// Evaluation order of one call (resolution R12, probe S37), as in the CodeContracts rewriter:
/// entry checks in definition order → old values → member → postconditions → member invariants → interface invariants.
/// </summary>
[Collection(nameof(EvaluationOrderTests))]
public class EvaluationOrderTests
{
    [Fact]
    public void Call_runs_checks_in_the_legacy_order()
    {
        IRecorded proxy = Dbc.Make<IRecorded>(new Recorded());
        Log.Clear();

        proxy.Add(1);

        Assert.Equal(
            ["requires", "assert", "old", "body", "ensures", "member-invariant", "interface-invariant"],
            Log.Entries);
    }

    [Fact]
    public void Failing_entry_check_stops_the_call_before_old_values_and_body()
    {
        IRecorded proxy = Dbc.Make<IRecorded>(new Recorded());
        Log.Clear();

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Add(-1));

        Assert.Equal(["requires"], Log.Entries);
    }

    [Fact]
    public void Make_checks_interface_invariants_only()
    {
        Log.Clear();

        Dbc.Make<IRecorded>(new Recorded());

        Assert.Equal(["interface-invariant"], Log.Entries);
    }

    [Fact]
    public void Members_without_clauses_still_check_interface_invariants()
    {
        IRecorded proxy = Dbc.Make<IRecorded>(new Recorded());
        Log.Clear();

        proxy.Touch();

        Assert.Equal(["body", "interface-invariant"], Log.Entries);
    }
}

[ContractClass(typeof(RecordedContracts))]
public interface IRecorded
{
    int Count { get; }

    void Add(int value);

    void Touch();
}

public sealed class Recorded : IRecorded
{
    public int Count { get; private set; }

    public void Add(int value)
    {
        Log.Mark("body");
        Count += value;
    }

    public void Touch() => Log.Mark("body");
}

[ContractClassFor(typeof(IRecorded))]
internal sealed class RecordedContracts
{
    public RecordedContracts()
    {
        Dbc.Def(static (IRecorded x, int value) => x.Add(value),
            static (IRecorded x, int value) => Contract.Requires(Log.Mark("requires") && value > 0),
            static (IRecorded x, int value) => Contract.Ensures(Log.Mark("ensures") && x.Count == Contract.OldValue(Log.Value("old", x.Count)) + value),
            static (IRecorded x, int value) => Contract.Invariant(Log.Mark("member-invariant")),
            static (IRecorded x, int value) => Contract.Assert(Log.Mark("assert")));

        Dbc.Def(static (IRecorded x) => Contract.Invariant(Log.Mark("interface-invariant")));
    }
}

/// <summary>Records clause evaluations; tests using it share one collection so they do not interleave.</summary>
internal static class Log
{
    private static readonly List<string> entries = [];

    public static IReadOnlyList<string> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    public static void Clear()
    {
        lock (entries)
        {
            entries.Clear();
        }
    }

    public static bool Mark(string entry)
    {
        lock (entries)
        {
            entries.Add(entry);
        }

        return true;
    }

    public static int Value(string entry, int value)
    {
        Mark(entry);
        return value;
    }
}
