namespace ContractExpressions4.Internal;

internal static class ContractEvaluator
{
    public static void ValidateAll(ContractInvocationContext context, IEnumerable<CompiledContract> contracts, string methodName)
    {
        foreach (CompiledContract contract in contracts)
        {
            bool satisfied = contract.Predicate(context);
            if (!satisfied)
            {
                throw new ContractViolationException(contract.Kind, methodName, contract.SourceText);
            }
        }
    }
}
