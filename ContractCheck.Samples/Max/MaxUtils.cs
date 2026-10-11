namespace ContractCheck.Samples.Max;

public sealed class MaxUtils : IMaxUtils
{
    public int Max(int[] elements)
    {
        int max = int.MinValue;
        foreach (int element in elements)
        {
            if (max < element)
            {
                max = element;
            }
        }

        return max;
    }

    public int[]? ParseToInts(string[] original)
    {
        var result = new int[original.Length];
        int position = 0;
        foreach (string text in original)
        {
            if (int.TryParse(text, out int parsed))
            {
                result[position++] = parsed;
            }
        }

        return position == 0 ? null : result;
    }
}
