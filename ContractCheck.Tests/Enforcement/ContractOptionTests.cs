#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Options;

/// <summary>
/// The <c>System.Diagnostics.Contracts</c> attributes that change runtime checking:
/// <c>[ContractOption("runtime", "checking", false)]</c> and <c>[ContractRuntimeIgnored]</c>.
/// (<c>[ContractOption("contract", "inheritance", false)]</c> and <c>[Pure]</c> are covered by the inheritance and invariant tests.)
/// </summary>
public class ContractOptionTests
{
    [Fact]
    public void Runtime_checking_can_be_switched_off_for_an_interface()
    {
        IUnchecked proxy = Dbc.Make<IUnchecked>(new Options(-1));

        proxy.Run(-1);
    }

    [Fact]
    public void Runtime_checking_can_be_switched_off_for_a_member()
    {
        IPartlyChecked proxy = Dbc.Make<IPartlyChecked>(new Options(0));

        proxy.Unchecked(-1);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Checked(-1));
    }

    [Fact]
    public void Runtime_checking_can_be_switched_off_for_a_property()
    {
        IPartlyChecked proxy = Dbc.Make<IPartlyChecked>(new Options(0));

        proxy.Level = -1;
    }

    [Fact]
    public void Clauses_that_use_ContractRuntimeIgnored_members_are_not_checked()
    {
        IPartlyChecked proxy = Dbc.Make<IPartlyChecked>(new Options(0));

        proxy.Validated(-1);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Validated(-2));
    }
}

[ContractClass(typeof(UncheckedContracts))]
[ContractOption("runtime", "checking", false)]
public interface IUnchecked
{
    void Run(int value);
}

[ContractClass(typeof(PartlyCheckedContracts))]
public interface IPartlyChecked
{
    [ContractOption("runtime", "checking", false)]
    int Level { get; set; }

    void Checked(int value);

    [ContractOption("runtime", "checking", false)]
    void Unchecked(int value);

    void Validated(int value);
}

public sealed class Options(int level) : IUnchecked, IPartlyChecked
{
    public int Level { get; set; } = level;

    public void Run(int value)
    {
    }

    public void Checked(int value)
    {
    }

    public void Unchecked(int value)
    {
    }

    public void Validated(int value)
    {
    }
}

[ContractClassFor(typeof(IUnchecked))]
internal sealed class UncheckedContracts
{
    public UncheckedContracts()
    {
        Dbc.Def(static (IUnchecked x, int value) => x.Run(value), static (IUnchecked x, int value) => Contract.Requires(value >= 0));
        Dbc.Def(static (IUnchecked x) => Contract.Invariant(false));
    }
}

[ContractClassFor(typeof(IPartlyChecked))]
internal sealed class PartlyCheckedContracts
{
    public PartlyCheckedContracts()
    {
        Dbc.Def(static (IPartlyChecked x, int value) => x.Checked(value), static (IPartlyChecked x, int value) => Contract.Requires(value >= 0));
        Dbc.Def(static (IPartlyChecked x, int value) => x.Unchecked(value), static (IPartlyChecked x, int value) => Contract.Requires(value >= 0));
        Dbc.DefSetter(static (IPartlyChecked x) => x.Level, static (IPartlyChecked x, int value) => Contract.Requires(value >= 0));
        Dbc.Def(static (IPartlyChecked x, int value) => x.Validated(value),
            static (IPartlyChecked x, int value) => Contract.Requires(ExpensiveCheck.IsValid(value)),
            static (IPartlyChecked x, int value) => Contract.Requires(value != -2));
    }
}

internal static class ExpensiveCheck
{
    [ContractRuntimeIgnored]
    public static bool IsValid(int value) => value >= 0;
}
