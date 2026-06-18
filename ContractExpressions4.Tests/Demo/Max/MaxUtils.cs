namespace ContractExpressions4.Tests.Demo.Max;

internal sealed class MaxUtils : IMaxUtils
{
    public int Max(int[] elements)
    {
        int max = int.MinValue;
        for (int i = 0; i < elements.Length; i++)
        {
            if (max < elements[i])
            {
                max = elements[i];
            }
        }

        return max;
    }

    public int[]? ParseToInts(string[] original)
    {
        int[] result = new int[original.Length];
        int position = 0;

        foreach (string value in original)
        {
            if (int.TryParse(value, out int parsed))
            {
                result[position++] = parsed;
            }
        }

        return position == 0 ? null : result;
    }
}
