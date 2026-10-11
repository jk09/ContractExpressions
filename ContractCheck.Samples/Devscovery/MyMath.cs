namespace ContractCheck.Samples.Devscovery;

public sealed class MyMath : IMyMath
{
    public int AbsNetStyle(int x) => x < 0 ? -x : x;

    public int AbsJavaStyle(int x) => x < 0 ? -x : x;
}
