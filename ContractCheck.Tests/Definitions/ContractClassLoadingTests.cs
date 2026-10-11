#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Definitions.Loading;

/// <summary>
/// How <c>Dbc.Make</c> finds and runs contract classes (resolution R15): once per interface even under contention,
/// validated like the CodeContracts checker (CC1006, CC1019), and closed over the interface's type arguments when generic.
/// </summary>
public class ContractClassLoadingTests
{
    [Fact]
    public void Contract_class_runs_once_even_when_many_threads_make_the_first_proxy()
    {
        // ContractExpressions4 ran the constructor once per racing thread and registered every clause N times (probe S35).
        Parallel.For(0, 32, _ => Dbc.Make<IRaced>(new Raced()).Touch(1));
        Dbc.Make<IRaced>(new Raced()).Touch(1);

        Assert.Equal(1, RacedContracts.Constructed);
        Assert.Equal(1, RacedContracts.EvaluationsOf(() => Dbc.Make<IRaced>(new Raced()).Touch(1)));
    }

    [Fact]
    public void Contract_class_without_ContractClassFor_is_rejected_CC1019()
    {
        Expect.DefinitionError(() => Dbc.Make<INoBackPointer>(new NoBackPointer()), "is not marked [ContractClassFor(typeof(INoBackPointer))]", "CC1019");
    }

    [Fact]
    public void Contract_class_pointing_back_to_another_interface_is_rejected()
    {
        Expect.DefinitionError(() => Dbc.Make<IWrongBackPointer>(new WrongBackPointer()), "CC1019");
    }

    [Fact]
    public void ContractClass_naming_a_struct_is_rejected_CC1006()
    {
        Expect.DefinitionError(() => Dbc.Make<IStructContracts>(new StructContractsImpl()), "which is not a class", "CC1006");
    }

    [Fact]
    public void Contract_class_without_a_parameterless_constructor_is_rejected()
    {
        Expect.DefinitionError(() => Dbc.Make<INeedsArgument>(new NeedsArgument()), "needs a parameterless constructor");
    }

    [Fact]
    public void Failing_contract_class_reports_the_cause_on_every_Make()
    {
        InvalidOperationException first = Assert.Throws<InvalidOperationException>(() => Dbc.Make<IFailingContracts>(new FailingContractsImpl()));
        InvalidOperationException second = Assert.Throws<InvalidOperationException>(() => Dbc.Make<IFailingContracts>(new FailingContractsImpl()));

        Assert.Contains("FailingContractsClass", first.Message, StringComparison.Ordinal);
        Assert.IsType<NotSupportedException>(first.InnerException);
        Assert.Same(first, second);
    }

    [Fact]
    public void Abstract_contract_class_declares_contracts_in_its_static_constructor()
    {
        IStaticDeclared proxy = Dbc.Make<IStaticDeclared>(new StaticDeclared());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Set(-1));
        proxy.Set(1);
    }

    [Fact]
    public void Generic_contract_class_is_closed_over_each_interface_instantiation()
    {
        // Both source projects failed here (probe S20).
        IRepository<string> strings = Dbc.Make<IRepository<string>>(new Repository<string>());
        IRepository<Uri> uris = Dbc.Make<IRepository<Uri>>(new Repository<Uri>());

        Expect.Violation(ContractFailureKind.Precondition, () => strings.Save(null!));
        Expect.Violation(ContractFailureKind.Precondition, () => uris.Save(null!));
        strings.Save("ok");
        Assert.Equal(1, strings.Count);
    }

    [Fact]
    public void Contract_class_is_found_through_ContractClassFor_when_ContractClass_was_compiled_away()
    {
        // [ContractClass] is [Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]: Release builds drop it from interface
        // files without #define CONTRACTS_FULL. IFoundByBackPointer has no [ContractClass] at all, like such a build.
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IFoundByBackPointer>(new FoundByBackPointer()).Apply(0));
    }

    [Fact]
    public void Generic_contract_class_is_found_through_ContractClassFor_too() =>
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IFoundBox<string>>(new FoundBox<string>()).Put(null!));

    [Fact]
    public void Several_ContractClassFor_classes_for_an_interface_without_ContractClass_are_rejected() =>
        Expect.DefinitionError(
            () => Dbc.Make<IClaimedTwice>(new ClaimedTwice()),
            "several classes are marked [ContractClassFor(typeof(IClaimedTwice))]",
            "ClaimedTwiceContractsA",
            "ClaimedTwiceContractsB");

    [Fact]
    public void Interface_without_ContractClass_can_receive_contracts_directly()
    {
        Dbc.Def(static (IDirectlyDefined x, int v) => x.Apply(v), static (IDirectlyDefined x, int v) => Contract.Requires(v != 0));

        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IDirectlyDefined>(new DirectlyDefined()).Apply(0));
    }

    [Fact]
    public void Contracts_defined_after_a_proxy_was_made_apply_to_its_next_call()
    {
        ILateDefined proxy = Dbc.Make<ILateDefined>(new LateDefined());
        proxy.Apply(0);

        Dbc.Def(static (ILateDefined x, int v) => x.Apply(v), static (ILateDefined x, int v) => Contract.Requires(v != 0));

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Apply(0));
    }
}

[ContractClass(typeof(RacedContracts))]
public interface IRaced
{
    void Touch(int value);
}

public sealed class Raced : IRaced
{
    public void Touch(int value)
    {
    }
}

[ContractClassFor(typeof(IRaced))]
internal sealed class RacedContracts
{
    private static int constructed;
    [ThreadStatic] private static int evaluations;

    public RacedContracts()
    {
        Interlocked.Increment(ref constructed);
        Thread.Sleep(50);
        Dbc.Def(static (IRaced x, int value) => x.Touch(value), static (IRaced x, int value) => Contract.Requires(Count(value)));
    }

    public static int Constructed => Volatile.Read(ref constructed);

    public static int EvaluationsOf(Action action)
    {
        evaluations = 0;
        action();
        return evaluations;
    }

    private static bool Count(int value)
    {
        evaluations++;
        return value > 0;
    }
}

[ContractClass(typeof(NoBackPointerContracts))]
public interface INoBackPointer
{
    void M();
}

public sealed class NoBackPointer : INoBackPointer
{
    public void M()
    {
    }
}

internal sealed class NoBackPointerContracts
{
}

[ContractClass(typeof(WrongBackPointerContracts))]
public interface IWrongBackPointer
{
    void M();
}

public sealed class WrongBackPointer : IWrongBackPointer
{
    public void M()
    {
    }
}

[ContractClassFor(typeof(INoBackPointer))]
internal sealed class WrongBackPointerContracts
{
}

[ContractClass(typeof(StructContracts))]
public interface IStructContracts
{
    void M();
}

public sealed class StructContractsImpl : IStructContracts
{
    public void M()
    {
    }
}

// [ContractClassFor] can only be applied to classes, which is exactly what the check reports.
internal struct StructContracts
{
}

[ContractClass(typeof(NeedsArgumentContracts))]
public interface INeedsArgument
{
    void M();
}

public sealed class NeedsArgument : INeedsArgument
{
    public void M()
    {
    }
}

[ContractClassFor(typeof(INeedsArgument))]
internal sealed class NeedsArgumentContracts(int unused)
{
    public int Unused { get; } = unused;
}

[ContractClass(typeof(FailingContractsClass))]
public interface IFailingContracts
{
    void M();
}

public sealed class FailingContractsImpl : IFailingContracts
{
    public void M()
    {
    }
}

[ContractClassFor(typeof(IFailingContracts))]
internal sealed class FailingContractsClass
{
    public FailingContractsClass() => throw new NotSupportedException("contract class failed on purpose");
}

[ContractClass(typeof(StaticDeclaredContracts))]
public interface IStaticDeclared
{
    void Set(int value);
}

public sealed class StaticDeclared : IStaticDeclared
{
    public void Set(int value)
    {
    }
}

[ContractClassFor(typeof(IStaticDeclared))]
internal static class StaticDeclaredContracts
{
    static StaticDeclaredContracts()
    {
        Dbc.Def(static (IStaticDeclared x, int value) => x.Set(value), static (IStaticDeclared x, int value) => Contract.Requires(value >= 0));
    }
}

[ContractClass(typeof(RepositoryContracts<>))]
public interface IRepository<T>
    where T : class
{
    int Count { get; }

    void Save(T item);
}

public sealed class Repository<T> : IRepository<T>
    where T : class
{
    private readonly List<T> items = [];

    public int Count => items.Count;

    public void Save(T item) => items.Add(item);
}

[ContractClassFor(typeof(IRepository<>))]
internal sealed class RepositoryContracts<T>
    where T : class
{
    public RepositoryContracts()
    {
        Dbc.Def(static (IRepository<T> x, T item) => x.Save(item),
            static (IRepository<T> x, T item) => Contract.Requires(item != null),
            static (IRepository<T> x, T item) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1));
    }
}

public interface IDirectlyDefined
{
    void Apply(int value);
}

public sealed class DirectlyDefined : IDirectlyDefined
{
    public void Apply(int value)
    {
    }
}

public interface ILateDefined
{
    void Apply(int value);
}

public sealed class LateDefined : ILateDefined
{
    public void Apply(int value)
    {
    }
}

public interface IFoundByBackPointer
{
    void Apply(int value);
}

public sealed class FoundByBackPointer : IFoundByBackPointer
{
    public void Apply(int value)
    {
    }
}

[ContractClassFor(typeof(IFoundByBackPointer))]
internal sealed class FoundByBackPointerContracts
{
    public FoundByBackPointerContracts() =>
        Dbc.Def(static (IFoundByBackPointer x, int value) => x.Apply(value), static (IFoundByBackPointer x, int value) => Contract.Requires(value != 0));
}

public interface IFoundBox<T>
    where T : class
{
    void Put(T item);
}

public sealed class FoundBox<T> : IFoundBox<T>
    where T : class
{
    public void Put(T item)
    {
    }
}

[ContractClassFor(typeof(IFoundBox<>))]
internal sealed class FoundBoxContracts<T>
    where T : class
{
    public FoundBoxContracts() =>
        Dbc.Def(static (IFoundBox<T> x, T item) => x.Put(item), static (IFoundBox<T> x, T item) => Contract.Requires(item != null));
}

public interface IClaimedTwice
{
    void Apply(int value);
}

public sealed class ClaimedTwice : IClaimedTwice
{
    public void Apply(int value)
    {
    }
}

[ContractClassFor(typeof(IClaimedTwice))]
internal sealed class ClaimedTwiceContractsA
{
}

[ContractClassFor(typeof(IClaimedTwice))]
internal sealed class ClaimedTwiceContractsB
{
}
