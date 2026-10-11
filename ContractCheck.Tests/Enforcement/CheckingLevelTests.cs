#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Levels;

/// <summary>
/// <see cref="Dbc.CheckingLevel"/>: each level checks exactly its clause kinds (CodeContracts' runtime checking levels),
/// skipped kinds cost nothing, and the initial level comes from the runtimeconfig setting <c>ContractCheck.CheckingLevel</c>.
/// </summary>
[Collection(GlobalSettings.Name)]
public sealed class CheckingLevelTests : IDisposable
{
    public void Dispose()
    {
        AppContext.SetData(CheckingLevels.SettingName, null);
        Dbc.CheckingLevel = ContractCheckingLevel.Full;
    }

    [Theory]
    //          level                                 Requires Typed  Assert Assume Ensures OnThrow MemberInv InterfaceInv
    [InlineData(ContractCheckingLevel.Full,            true,    true,  true,  true,  true,   true,   true,     true)]
    [InlineData(ContractCheckingLevel.PreAndPost,      true,    true,  false, false, true,   true,   false,    false)]
    [InlineData(ContractCheckingLevel.Preconditions,   true,    true,  false, false, false,  false,  false,    false)]
    [InlineData(ContractCheckingLevel.ReleaseRequires, false,   true,  false, false, false,  false,  false,    false)]
    [InlineData(ContractCheckingLevel.None,            false,   false, false, false, false,  false,  false,    false)]
    public void Each_level_checks_exactly_its_clause_kinds(
        ContractCheckingLevel level, bool requires, bool typed, bool assert, bool assume, bool ensures, bool onThrow, bool memberInvariant, bool interfaceInvariant)
    {
        ILevelled proxy = Dbc.Make<ILevelled>(new Levelled());
        Dbc.CheckingLevel = level;

        Fails(requires, ContractFailureKind.Precondition, () => proxy.Pre(0));
        if (typed)
        {
            Assert.Throws<ArgumentNullException>(() => proxy.Typed(null!));
        }
        else
        {
            proxy.Typed(null!);
        }

        Fails(assert, ContractFailureKind.Assert, () => proxy.Asserted(0));
        Fails(assume, ContractFailureKind.Assume, () => proxy.Assumed(0));
        Fails(ensures, ContractFailureKind.Postcondition, () => proxy.Post(1));
        if (onThrow)
        {
            Expect.Violation(ContractFailureKind.PostconditionOnException, () => proxy.Throw());
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => proxy.Throw());
        }

        Fails(memberInvariant, ContractFailureKind.Invariant, () => proxy.Grow());
        Fails(interfaceInvariant, ContractFailureKind.Invariant, () => Dbc.Make<ILevelled>(new Levelled()).Break());
    }

    [Theory]
    [InlineData(ContractCheckingLevel.Full, true)]
    [InlineData(ContractCheckingLevel.PreAndPost, false)]
    [InlineData(ContractCheckingLevel.None, false)]
    public void Creation_invariants_are_checked_only_at_Full(ContractCheckingLevel level, bool checkedAtCreation)
    {
        Dbc.CheckingLevel = level;

        Fails(checkedAtCreation, ContractFailureKind.Invariant, () => Dbc.Make<ILevelled>(new Levelled { Count = -1 }));
    }

    [Fact]
    public void Old_values_are_not_captured_when_postconditions_are_off()
    {
        var target = new Levelled();
        ILevelled proxy = Dbc.Make<ILevelled>(target);

        Dbc.CheckingLevel = ContractCheckingLevel.Preconditions;
        proxy.Track();
        Assert.Equal(0, target.Probes);

        Dbc.CheckingLevel = ContractCheckingLevel.PreAndPost;
        proxy.Track();
        Assert.Equal(1, target.Probes);
    }

    [Fact]
    public async Task Async_members_return_the_original_task_when_nothing_deferred_is_checked()
    {
        var target = new Levelled();
        ILevelled proxy = Dbc.Make<ILevelled>(target);

        Dbc.CheckingLevel = ContractCheckingLevel.Preconditions;
        Task<int> returned = proxy.PostAsync(1);
        Assert.Same(target.LastTask, returned);
        Assert.Equal(2, await returned);

        Dbc.CheckingLevel = ContractCheckingLevel.PreAndPost;
        await Expect.ViolationAsync(ContractFailureKind.Postcondition, () => proxy.PostAsync(1));
    }

    [Fact]
    public void A_level_change_applies_to_the_next_call_of_an_existing_proxy()
    {
        ILevelled proxy = Dbc.Make<ILevelled>(new Levelled());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Pre(0));
        Dbc.CheckingLevel = ContractCheckingLevel.None;
        proxy.Pre(0);
        Dbc.CheckingLevel = ContractCheckingLevel.Full;
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Pre(0));
    }

    [Fact]
    public void Contracts_are_validated_when_defined_whatever_the_level()
    {
        Dbc.CheckingLevel = ContractCheckingLevel.None;

        Expect.DefinitionError(
            () => Dbc.Def(static (ILevelled x, int v) => x.Pre(v), static (ILevelled x, int v) => Contract.Requires(Contract.Result<int>() > v)),
            "Contract.Result");
    }

    [Theory]
    [InlineData("Preconditions", ContractCheckingLevel.Preconditions)]
    [InlineData("preandpost", ContractCheckingLevel.PreAndPost)]
    [InlineData(" None ", ContractCheckingLevel.None)]
    public void The_initial_level_comes_from_the_runtimeconfig_setting(string setting, ContractCheckingLevel expected)
    {
        AppContext.SetData(CheckingLevels.SettingName, setting);
        CheckingLevels.Reset();

        Assert.Equal(expected, Dbc.CheckingLevel);
    }

    [Fact]
    public void Without_a_setting_the_level_is_Full()
    {
        CheckingLevels.Reset();

        Assert.Equal(ContractCheckingLevel.Full, Dbc.CheckingLevel);
    }

    [Fact]
    public void An_unknown_setting_fails_every_use_with_the_bad_value()
    {
        AppContext.SetData(CheckingLevels.SettingName, "Fast");
        CheckingLevels.Reset();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Dbc.CheckingLevel);
        Assert.Contains("ContractCheck.CheckingLevel is 'Fast'", exception.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => Dbc.Make<ILevelled>(new Levelled()));
    }

    [Fact]
    public void Undefined_levels_are_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Dbc.CheckingLevel = (ContractCheckingLevel)42);

    private static void Fails(bool expected, ContractFailureKind kind, Action action)
    {
        if (expected)
        {
            Expect.Violation(kind, action);
        }
        else
        {
            action();
        }
    }
}

[ContractClass(typeof(LevelledContracts))]
public interface ILevelled
{
    int Count { get; }

    void Pre(int x);

    void Typed(string text);

    void Asserted(int x);

    void Assumed(int x);

    int Post(int x);

    void Throw();

    void Grow();

    void Break();

    void Track();

    int Probe();

    Task<int> PostAsync(int x);
}

/// <summary>Breaks every postcondition and invariant on purpose, so each check that runs fails.</summary>
public sealed class Levelled : ILevelled
{
    public int Count { get; set; }

    public int Probes { get; private set; }

    public Task<int>? LastTask { get; private set; }

    public void Pre(int x)
    {
    }

    public void Typed(string text)
    {
    }

    public void Asserted(int x)
    {
    }

    public void Assumed(int x)
    {
    }

    public int Post(int x) => x + 1;

    public void Throw() => throw new InvalidOperationException("thrown by the target");

    public void Grow() => Count = 5;

    public void Break() => Count = -1;

    public void Track() => Count++;

    public int Probe() => ++Probes;

    public Task<int> PostAsync(int x) => LastTask = Task.FromResult(x + 1);
}

[ContractClassFor(typeof(ILevelled))]
internal sealed class LevelledContracts
{
    public LevelledContracts()
    {
        Dbc.Def(static (ILevelled x, int v) => x.Pre(v), static (ILevelled x, int v) => Contract.Requires(v > 0));
        Dbc.Def(static (ILevelled x, string text) => x.Typed(text), static (ILevelled x, string text) => Contract.Requires<ArgumentNullException>(text != null, "text"));
        Dbc.Def(static (ILevelled x, int v) => x.Asserted(v), static (ILevelled x, int v) => Contract.Assert(v > 0));
        Dbc.Def(static (ILevelled x, int v) => x.Assumed(v), static (ILevelled x, int v) => Contract.Assume(v > 0));
        Dbc.Def(static (ILevelled x, int v) => x.Post(v), static (ILevelled x, int v) => Contract.Ensures(Contract.Result<int>() == v));
        Dbc.Def(static (ILevelled x) => x.Throw(), static (ILevelled x) => Contract.EnsuresOnThrow<InvalidOperationException>(false));
        Dbc.Def(static (ILevelled x) => x.Grow(), static (ILevelled x) => Contract.Invariant(x.Count <= 1));
        Dbc.Def(static (ILevelled x) => x.Track(), static (ILevelled x) => Contract.Ensures(Contract.OldValue(x.Probe()) >= 0));
        Dbc.Def(static (ILevelled x, int v) => x.PostAsync(v), static (ILevelled x, int v) => Contract.Ensures(Contract.Result<int>() == v));
        Dbc.Def(static (ILevelled x) => Contract.Invariant(x.Count >= 0));
    }
}
