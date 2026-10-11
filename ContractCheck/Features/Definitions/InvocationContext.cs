using System.Runtime.ExceptionServices;

namespace ContractCheck;

/// <summary>
/// The state one compiled clause reads: the target object, the call's arguments, the result and the old values.
/// Compiled clause delegates are <c>Func&lt;InvocationContext, bool&gt;</c>; the clause compiler rewrites lambda parameters,
/// <c>Contract.Result</c> and <c>Contract.OldValue</c> into calls of the accessors below.
/// </summary>
internal sealed class InvocationContext(object target, object?[] arguments)
{
    public object Target { get; } = target;

    public object?[] Arguments { get; } = arguments;

    /// <summary>The member's return value; for async members, the task while synchronous clauses run and the awaited value afterwards.</summary>
    public object? Result { get; set; }

    /// <summary>Old values per clause, indexed by the clause's position among the member's clauses that capture old values.</summary>
    public CapturedValue[]?[]? OldValues { get; set; }

    /// <summary>The old values of the clause being evaluated.</summary>
    public CapturedValue[]? CurrentOldValues { get; set; }

    public T GetTarget<T>() => (T)Target;

    public T GetArgument<T>(int index) => Arguments[index] is { } value ? (T)value : default!;

    public T GetResult<T>() => Result is { } value ? (T)value : default!;

    public T GetOldValue<T>(int slot) => CurrentOld(slot).Value is { } value ? (T)value : default!;

    public T GetOldValueAt<T>(int slot, int index) => ((QuantifiedOldValues)CurrentOld(slot).Value!).Get<T>(index);

    private CapturedValue CurrentOld(int slot)
    {
        CapturedValue captured = CurrentOldValues![slot];
        captured.Failure?.Throw();
        return captured;
    }
}

/// <summary>A value captured before the call, or the exception its capture threw (rethrown only if the clause reads it).</summary>
internal readonly record struct CapturedValue(object? Value, ExceptionDispatchInfo? Failure);

/// <summary>Old values of <c>Contract.OldValue(e(i))</c> for every <c>i</c> of a <c>Contract.ForAll/Exists(from, to, i =&gt; ...)</c> range at entry.</summary>
internal sealed class QuantifiedOldValues(int from, object?[] values)
{
    public T Get<T>(int index)
    {
        int offset = index - from;
        if (offset < 0 || offset >= values.Length)
        {
            throw new InvalidOperationException(
                $"Contract.OldValue was captured for indexes {from}..{from + values.Length - 1} at entry; index {index} has no old value.");
        }

        return values[offset] is { } value ? (T)value : default!;
    }
}
