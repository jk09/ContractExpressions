internal static class ContractRegistry
{
    private static Dictionary<Type, ContractDelegates> Contracts { get; } = new();

    public static void Add(Type intfType, ContractDelegates contracts)
    {
        if (Contracts.TryGetValue(intfType, out var existingContracts))
        {
            // merge
            foreach (var (k, v) in contracts.Preconditions)
            {
                existingContracts.Preconditions.AddItem(k, v);
            }

            foreach (var (k, v) in contracts.Postconditions)
            {
                existingContracts.Postconditions.AddItem(k, v);
            }

            foreach (var (k, v) in contracts.OldValueCollectors)
            {
                existingContracts.OldValueCollectors[k] = v;
            }
        }
        else
        {
            Contracts[intfType] = contracts;
        }
    }

    public static ContractDelegates Get(Type intfType)
    {
        return Contracts[intfType];
    }
}
