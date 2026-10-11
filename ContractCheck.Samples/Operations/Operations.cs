namespace ContractCheck.Samples.Operations;

public abstract class BinaryIntOperation : IBinaryOperation
{
    private static readonly Type[] Types = [typeof(int), typeof(int)];

    public Type[] ArgumentTypes => Types;

    public Type ResultType => typeof(int);

    public object Perform(params object[] arguments) => Operation((int)arguments[0], (int)arguments[1]);

    public abstract int Operation(int arg1, int arg2);
}

public sealed class IntAdder : BinaryIntOperation
{
    public override int Operation(int arg1, int arg2) => arg1 + arg2;
}

public sealed class IntSub : BinaryIntOperation
{
    public override int Operation(int arg1, int arg2) => arg1 - arg2;
}

public sealed class IntMul : BinaryIntOperation
{
    public override int Operation(int arg1, int arg2) => arg1 * arg2;
}

/// <summary>Like the legacy demo, division adds no contract of its own: dividing by zero throws <see cref="DivideByZeroException"/>.</summary>
public sealed class IntDiv : BinaryIntOperation
{
    public override int Operation(int arg1, int arg2) => arg1 / arg2;
}

public sealed class IntUnaryMinus : IUnaryOperation
{
    private static readonly Type[] Types = [typeof(int)];

    public Type[] ArgumentTypes => Types;

    public Type ResultType => typeof(int);

    public object Perform(params object[] arguments) => -(int)arguments[0];
}
