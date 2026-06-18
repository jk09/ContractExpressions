namespace ContractExpressions4.Tests.Demo.Operations;

internal sealed class IntAdderOperation : IOperationDemo
{
    private static readonly Type[] Types = [typeof(int), typeof(int)];

    public Type[] ArgumentTypes => Types;
    public Type ResultType => typeof(int);

    public object Perform(params object[] arguments)
    {
        int arg1 = (int)arguments[0];
        int arg2 = (int)arguments[1];
        return arg1 + arg2;
    }
}
