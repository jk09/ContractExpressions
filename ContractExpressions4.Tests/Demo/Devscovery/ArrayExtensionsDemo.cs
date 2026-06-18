namespace ContractExpressions4.Tests.Demo.Devscovery;

internal sealed class ArrayExtensionsDemo : IArrayExtensionsDemo
{
    public int[] Abs(int[] xs)
    {
        int[] result = new int[xs.Length];

        for (int i = 0; i < xs.Length; i++)
        {
            int value = xs[i];
            result[i] = value < 0 ? -value : value;
        }

        return result;
    }
}
