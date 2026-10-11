#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Definitions.Setters;

/// <summary>
/// <c>Dbc.DefSetter</c>: contracts for property and indexer setters, which expression-tree selectors cannot express as an
/// assignment. Clauses receive the assigned value as their last parameter. (Arity coverage is in the arity tests.)
/// </summary>
public class DefSetterTests
{
    [Fact]
    public void Setter_precondition_and_postcondition_are_checked()
    {
        ISettings proxy = Dbc.Make<ISettings>(new Settings());

        proxy.Limit = 5;

        Assert.Equal(5, proxy.Limit);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Limit = -1);
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<ISettings>(new Settings { IgnoreWrites = true }).Limit = 7);
    }

    [Fact]
    public void Indexer_setter_clauses_see_the_index_and_the_value()
    {
        ISettings proxy = Dbc.Make<ISettings>(new Settings());

        proxy["name"] = "value";

        Assert.Equal("value", proxy["name"]);
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => proxy[""] = "value");
        Assert.Equal("set_Item", exception.Method);
    }

    [Fact]
    public void Getter_and_setter_contracts_are_independent()
    {
        ISettings proxy = Dbc.Make<ISettings>(new Settings());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy["missing"]);
        proxy["missing"] = "now present";
    }
}

[ContractClass(typeof(SettingsContracts))]
public interface ISettings
{
    int Limit { get; set; }

    string this[string key] { get; set; }
}

public sealed class Settings : ISettings
{
    private readonly Dictionary<string, string> values = [];
    private int limit;

    public bool IgnoreWrites { get; init; }

    public int Limit
    {
        get => limit;
        set => limit = IgnoreWrites ? limit : value;
    }

    public string this[string key]
    {
        get => values[key];
        set => values[key] = value;
    }

    public bool Contains(string key) => values.ContainsKey(key);
}

[ContractClassFor(typeof(ISettings))]
internal sealed class SettingsContracts
{
    public SettingsContracts()
    {
        Dbc.DefSetter(static (ISettings x) => x.Limit,
            static (ISettings x, int value) => Contract.Requires(value >= 0),
            static (ISettings x, int value) => Contract.Ensures(x.Limit == value));

        Dbc.DefSetter(static (ISettings x, string key) => x[key],
            static (ISettings x, string key, string value) => Contract.Requires(!string.IsNullOrEmpty(key)),
            static (ISettings x, string key, string value) => Contract.Ensures(x[key] == value));

        Dbc.Def(static (ISettings x, string key) => x[key],
            static (ISettings x, string key) => Contract.Requires(((Settings)x).Contains(key)));
    }
}
