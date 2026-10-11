#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Proxies;

/// <summary><c>Dbc.Make</c> and how the proxy forwards calls (resolutions R17, R18, probes S15, S19, S21).</summary>
public class ProxyTests
{
    [Fact]
    public void Null_target_is_rejected_immediately()
    {
        Assert.Throws<ArgumentNullException>(() => Dbc.Make<IService>(null!));
    }

    [Fact]
    public void Class_type_argument_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Dbc.Make(new Service()));

        Assert.Contains("needs an interface type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Making_a_proxy_of_a_proxy_returns_it_unchanged()
    {
        IService proxy = Dbc.Make<IService>(new Service());

        Assert.Same(proxy, Dbc.Make(proxy));
    }

    [Fact]
    public void Exceptions_of_the_target_keep_their_stack_trace()
    {
        // Both source projects rethrew with "throw ex.InnerException", losing the original frames (probe S15).
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Dbc.Make<IService>(new Service()).Fail());

        Assert.Contains(nameof(Service.Fail), exception.StackTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void Exceptions_thrown_by_a_condition_propagate_and_name_the_clause()
    {
        NullReferenceException exception = Assert.Throws<NullReferenceException>(() => Dbc.Make<IService>(new Service()).Length(null!));

        Assert.Equal("Contract.Requires(text.Length > 0)", exception.Data["ContractCheck.Clause"]);
    }

    [Fact]
    public void Members_without_clauses_are_forwarded()
    {
        Assert.Equal("echo", Dbc.Make<IService>(new Service()).Echo("echo"));
    }

    [Fact]
    public void Out_and_ref_parameters_still_work_through_the_proxy()
    {
        IService proxy = Dbc.Make<IService>(new Service());
        int counter = 1;

        bool found = proxy.TryFind("key", out string value);
        proxy.Bump(ref counter);

        Assert.True(found);
        Assert.Equal("value of key", value);
        Assert.Equal(2, counter);
    }

    [Fact]
    public void Struct_targets_are_checked()
    {
        IService proxy = Dbc.Make<IService>(new StructService());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Length(string.Empty));
        Assert.Equal(3, proxy.Length("abc"));
    }

    [Fact]
    public void Default_interface_methods_are_checked()
    {
        IService proxy = Dbc.Make<IService>(new Service());

        Assert.Equal("[x]", proxy.Decorate("x"));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Decorate(""));
    }

    [Fact]
    public void Internal_interfaces_can_be_proxied()
    {
        IHidden proxy = Dbc.Make<IHidden>(new Hidden());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Run(-1));
    }

    [Fact]
    public void Violation_carries_the_interface_and_member()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IService>(new Service()).Length(""));

        Assert.Equal(typeof(IService), exception.InterfaceType);
        Assert.Equal(typeof(IService).GetMethod(nameof(IService.Length)), exception.Member);
        Assert.Equal("Contract.Requires(text.Length > 0)", exception.ContractText);
    }
}

[ContractClass(typeof(ServiceContracts))]
public interface IService
{
    int Length(string text);

    string Echo(string text);

    void Fail();

    bool TryFind(string key, out string value);

    void Bump(ref int counter);

    string Decorate(string text) => "[" + text + "]";
}

public sealed class Service : IService
{
    public int Length(string text) => text.Length;

    public string Echo(string text) => text;

    public void Fail() => throw new InvalidOperationException("failed inside the target");

    public bool TryFind(string key, out string value)
    {
        value = "value of " + key;
        return true;
    }

    public void Bump(ref int counter) => counter++;
}

public struct StructService : IService
{
    public readonly int Length(string text) => text.Length;

    public readonly string Echo(string text) => text;

    public readonly void Fail() => throw new InvalidOperationException();

    public readonly bool TryFind(string key, out string value)
    {
        value = key;
        return true;
    }

    public readonly void Bump(ref int counter) => counter++;
}

[ContractClassFor(typeof(IService))]
internal sealed class ServiceContracts
{
    public ServiceContracts()
    {
        Dbc.Def(static (IService x, string text) => x.Length(text),
            static (IService x, string text) => Contract.Requires(text.Length > 0));

        Dbc.Def(static (IService x, string text) => x.Decorate(text),
            static (IService x, string text) => Contract.Requires(text.Length > 0),
            static (IService x, string text) => Contract.Ensures(Contract.Result<string>().Length == text.Length + 2));
    }
}

[ContractClass(typeof(HiddenContracts))]
internal interface IHidden
{
    void Run(int value);
}

internal sealed class Hidden : IHidden
{
    public void Run(int value)
    {
    }
}

[ContractClassFor(typeof(IHidden))]
internal sealed class HiddenContracts
{
    public HiddenContracts() =>
        Dbc.Def(static (IHidden x, int value) => x.Run(value), static (IHidden x, int value) => Contract.Requires(value >= 0));
}
