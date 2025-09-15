internal static class ContractRegistry
{
    private static Dictionary<Type, ContractDelegates> Contracts { get; } = new();

    public static void Add(Type intfType, ContractDelegates contracts)
    {
        Contracts.Add(intfType, contracts);
    }

    public static ContractDelegates Get(Type intfType)
    {
        return Contracts[intfType];
    }
}
