namespace ContractExpressions;

internal static class CollectionExtensions
{
    public static void AddSafe<TK, TV>(this IDictionary<TK, IList<TV>> dict, TK key, TV item)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, new List<TV>());
        }

        dict[key].Add(item);
    }

}