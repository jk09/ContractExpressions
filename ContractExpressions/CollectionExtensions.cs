
internal static class CollectionExtensions
{
    public static void AddItem<TKey, TItemValue>(this IDictionary<TKey, IList<TItemValue>> dict, TKey key, TItemValue item)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, new List<TItemValue>());
        }

        dict[key].Add(item);
    }
}
