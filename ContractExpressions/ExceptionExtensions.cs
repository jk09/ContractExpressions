namespace ContractExpressions;

internal static class ExceptionExtensions
{
    public static void AddContractData(this ContractViolationException ex, string contractDescription)
    {
        ex.Data[typeof(ContractExceptionData)] = new ContractExceptionData(contractDescription);
    }

    public static ContractExceptionData? GetContractData(this ContractViolationException ex)
    {
        if (ex.Data.Contains(typeof(ContractExceptionData)))
        {
            return ex.Data[typeof(ContractExceptionData)] as ContractExceptionData;
        }
        return null;
    }
}
