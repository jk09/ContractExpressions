#define CONTRACTS_FULL


using System.Reflection;

namespace ContractExpr;

internal class ContractContext
{
    public object? Result { get; set; }

    public IDictionary<MemberInfo, object?>? OldValues { get; set; }
}
