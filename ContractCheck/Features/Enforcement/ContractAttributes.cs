using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractCheck;

/// <summary>Reads the <c>System.Diagnostics.Contracts</c> attributes that change runtime checking.</summary>
internal static class ContractAttributes
{
    /// <summary>
    /// <c>[ContractOption("runtime", "checking", false)]</c> on the proxied interface, the member, its property,
    /// its declaring interface or that interface's assembly turns runtime checking off.
    /// </summary>
    public static bool RuntimeCheckingDisabled(Type proxiedInterface, MethodInfo? member)
    {
        if (IsOff(proxiedInterface, "runtime", "checking") || IsOff(proxiedInterface.Assembly, "runtime", "checking"))
        {
            return true;
        }

        if (member is null)
        {
            return false;
        }

        return IsOff(member, "runtime", "checking")
            || (MemberSelector.FindProperty(member) is { } property && IsOff(property, "runtime", "checking"))
            || (member.DeclaringType is { } declaringType
                && (IsOff(declaringType, "runtime", "checking") || IsOff(declaringType.Assembly, "runtime", "checking")));
    }

    /// <summary><c>[ContractOption("contract", "inheritance", false)]</c> on the proxied interface stops it inheriting contracts of its base interfaces.</summary>
    public static bool InheritanceDisabled(Type proxiedInterface) => IsOff(proxiedInterface, "contract", "inheritance");

    /// <summary>
    /// Pure members do not change state, so interface invariants are not re-checked after them – as in CodeContracts:
    /// property getters and methods marked <see cref="PureAttribute"/>.
    /// </summary>
    public static bool IsPure(MethodInfo member) =>
        member.IsDefined(typeof(PureAttribute), inherit: false)
        || MethodIdentity.Comparer.Equals(MemberSelector.FindProperty(member)?.GetMethod, member);

    private static bool IsOff(ICustomAttributeProvider provider, string category, string setting) =>
        provider.GetCustomAttributes(typeof(ContractOptionAttribute), inherit: false)
            .Cast<ContractOptionAttribute>()
            .Any(option =>
                string.Equals(option.Category, category, StringComparison.OrdinalIgnoreCase)
                && string.Equals(option.Setting, setting, StringComparison.OrdinalIgnoreCase)
                && (option.Value is null ? !option.Enabled : string.Equals(option.Value, "false", StringComparison.OrdinalIgnoreCase)));
}
