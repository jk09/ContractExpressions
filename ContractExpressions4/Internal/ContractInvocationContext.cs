using System.Globalization;

namespace ContractExpressions4.Internal;

internal sealed class ContractInvocationContext
{
    private readonly Dictionary<(string Token, int Slot), object?> oldValues = [];

    public ContractInvocationContext(object target, object?[] arguments)
    {
        Target = target;
        Arguments = arguments;
    }

    public object Target { get; }

    public object?[] Arguments { get; }

    public object? Result { get; set; }

    public T GetTarget<T>() => (T)Target;

    public T GetArgument<T>(int index)
    {
        if ((uint)index >= (uint)Arguments.Length)
        {
            throw new IndexOutOfRangeException(FormattableString.Invariant($"Argument index {index} is out of bounds."));
        }

        object? value = Arguments[index];
        return value is null ? default! : (T)value;
    }

    public T GetResult<T>()
    {
        object? value = Result;
        return value is null ? default! : (T)value;
    }

    public void SetOldValue(string token, int slot, object? value) => oldValues[(token, slot)] = value;

    public T GetOldValue<T>(string token, int slot)
    {
        if (!oldValues.TryGetValue((token, slot), out object? value))
        {
            string message = string.Format(CultureInfo.InvariantCulture, "Old value '{0}:{1}' was not captured.", token, slot);
            throw new InvalidOperationException(message);
        }

        return value is null ? default! : (T)value;
    }
}
