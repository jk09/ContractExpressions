using System.Diagnostics.Contracts;

namespace ContractExpressions2;

internal static class TypeExtensions
{
    public static bool IsContractClassFor(this Type cls, Type typeContractsAreFor)
    {
        return cls.GetCustomAttributesData().Any(a => a.AttributeType == typeof(ContractClassForAttribute)
                                                && a.ConstructorArguments[0].ArgumentType == typeContractsAreFor);
    }

}
