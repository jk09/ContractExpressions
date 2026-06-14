#define CONTRACTS_FULL


using System.Reflection;

namespace ContractExpressions3;

internal class ContractContext
{
    public object? Result { get; set; }

    public IDictionary<MemberInfo, object?>? OldValues { get; set; }

    public IDictionary<ParameterInfo, object?>? ValuesAtReturn { get; set; }
}

