#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Generics;

/// <summary>
/// Generic interfaces with a generic contract class, ported from CodeContracts Foxtrot/Tests/GenericsTest.cs:
/// explicit and implicit, generic and non-generic implementations of <c>IGenericInterface&lt;T&gt;</c>.
/// </summary>
public class GenericsTests
{
    public static TheoryData<string> Implementations => ["NonGenericExplicit", "NonGenericImplicit", "GenericExplicit", "GenericImplicit"];

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Positive_M_ported_from_GenericsTest(string implementation)
    {
        Assert.Equal("x", Make(implementation).M("x", behave: true));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Negative_M_ported_from_GenericsTest(string implementation)
    {
        Expect.Violation(ContractFailureKind.Postcondition, () => Make(implementation).M("x", behave: false));
        Expect.Violation(ContractFailureKind.Precondition, () => Make(implementation).M(null!, behave: true));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Quantifier_over_the_generic_array_ported_from_WithStaticClosure(string implementation)
    {
        IGenericInterface<string> proxy = Make(implementation);

        proxy.WithStaticClosure(["a", "b"], behave: true);
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.WithStaticClosure(["a", "b"], behave: false));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Index_quantifier_over_the_generic_array_ported_from_WithClosureObject(string implementation)
    {
        IGenericInterface<string> proxy = Make(implementation);

        proxy.WithClosureObject(["a", "b"], behave: true);
        Expect.Violation(ContractFailureKind.Postcondition, () => proxy.WithClosureObject(["a", "b"], behave: false));
    }

    [Fact]
    public void Each_instantiation_gets_its_own_contracts()
    {
        IGenericInterface<Uri> proxy = Dbc.Make<IGenericInterface<Uri>>(new GenericImplicitImpl<Uri>());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.M(null!, behave: true));
    }

    private static IGenericInterface<string> Make(string implementation) => implementation switch
    {
        "NonGenericExplicit" => Dbc.Make<IGenericInterface<string>>(new NonGenericExplicitImpl()),
        "NonGenericImplicit" => Dbc.Make<IGenericInterface<string>>(new NonGenericImplicitImpl()),
        "GenericExplicit" => Dbc.Make<IGenericInterface<string>>(new GenericExplicitImpl<string>()),
        _ => Dbc.Make<IGenericInterface<string>>(new GenericImplicitImpl<string>()),
    };
}

[ContractClass(typeof(ContractClassForAnInterface<>))]
public interface IGenericInterface<T>
    where T : class
{
    T M(T x, bool behave);

    void WithStaticClosure(T[] x, bool behave);

    void WithClosureObject(T[] x, bool behave);
}

[ContractClassFor(typeof(IGenericInterface<>))]
internal sealed class ContractClassForAnInterface<W>
    where W : class
{
    public ContractClassForAnInterface()
    {
        Dbc.Def(static (IGenericInterface<W> i, W y, bool b) => i.M(y, b),
            static (IGenericInterface<W> i, W y, bool b) => Contract.Requires(y != null),
            static (IGenericInterface<W> i, W y, bool b) => Contract.Ensures(Contract.Result<W>() != null && Contract.Result<W>().Equals(y)));

        Dbc.Def(static (IGenericInterface<W> i, W[] x, bool behave) => i.WithStaticClosure(x, behave),
            static (IGenericInterface<W> i, W[] x, bool behave) => Contract.Ensures(Contract.ForAll(x, e => e != null)));

        Dbc.Def(static (IGenericInterface<W> i, W[] x, bool behave) => i.WithClosureObject(x, behave),
            static (IGenericInterface<W> i, W[] x, bool behave) => Contract.Ensures(Contract.ForAll(0, x.Length, k => x[k] != null)));
    }
}

public sealed class NonGenericExplicitImpl : IGenericInterface<string>
{
    string IGenericInterface<string>.M(string z, bool behave) => behave ? z : string.Concat(z, z);

    void IGenericInterface<string>.WithStaticClosure(string[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }

    void IGenericInterface<string>.WithClosureObject(string[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }
}

public sealed class NonGenericImplicitImpl : IGenericInterface<string>
{
    public string M(string z, bool behave) => behave ? z : string.Concat(z, z);

    public void WithStaticClosure(string[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }

    public void WithClosureObject(string[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }
}

public sealed class GenericExplicitImpl<U> : IGenericInterface<U>
    where U : class
{
    U IGenericInterface<U>.M(U z, bool behave) => behave ? z : null!;

    void IGenericInterface<U>.WithStaticClosure(U[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }

    void IGenericInterface<U>.WithClosureObject(U[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }
}

public sealed class GenericImplicitImpl<U> : IGenericInterface<U>
    where U : class
{
    public U M(U z, bool behave) => behave ? z : null!;

    public void WithStaticClosure(U[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }

    public void WithClosureObject(U[] x, bool behave)
    {
        if (!behave)
        {
            x[0] = null!;
        }
    }
}
