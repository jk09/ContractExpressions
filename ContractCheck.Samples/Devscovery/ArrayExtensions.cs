namespace ContractCheck.Samples.Devscovery;

public sealed class ArrayExtensions(IMyMath math) : IArrayExtensions
{
    public ArrayExtensions()
        : this(Dbc.Make<IMyMath>(new MyMath()))
    {
    }

    public int[] Abs(int[] xs)
    {
        var result = new int[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            result[i] = math.AbsNetStyle(xs[i]);
        }

        return result;
    }
}
