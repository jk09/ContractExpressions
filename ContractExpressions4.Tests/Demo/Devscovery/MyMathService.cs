namespace ContractExpressions4.Tests.Demo.Devscovery;

internal sealed class MyMathService : IMyMath
{
    public int AbsNetStyle(int x)
    {
        if (x < 0)
        {
            return -x;
        }

        return x;
    }

    public int AbsJavaStyle(int x)
    {
        if (x < 0)
        {
            return -x;
        }

        return x;
    }
}
