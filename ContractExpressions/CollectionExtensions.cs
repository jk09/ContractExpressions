namespace ContractExpressions;

internal static class CollectionExtensions
{
    public static void AddItem<TKey, TItemValue>(this IDictionary<TKey, IList<TItemValue>> dict, TKey key, TItemValue item)
    {
        dict.AddItem(key, [item]);
    }

    public static void AddItem<TKey, TItemValue>(this IDictionary<TKey, IList<TItemValue>> dict, TKey key, IEnumerable<TItemValue> items)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, []);
        }
        foreach (var item in items)
        {
            dict[key].Add(item);
        }
    }
}
