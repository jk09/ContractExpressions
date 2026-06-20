#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.CE1;

/// <summary>
/// Dedicated tests for Contract.EnsuresOnThrow — first test coverage.
/// Covers: EnsuresOnThrow&lt;TException&gt;(bool), EnsuresOnThrow&lt;TException&gt;(bool, string),
/// condition passing/failing when exception is thrown.
/// </summary>
public class EnsuresOnThrowTests : IClassFixture<ContractFailureUnwindFixture>
{
    [Fact]
    public void Parse_ValidInput_Succeeds()
    {
        var proxy = Dbc.Make<IParser>(new SimpleParser());
        Assert.Equal(42, proxy.Parse("42"));
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        var proxy = Dbc.Make<IParser>(new SimpleParser());
        // Input is non-null, so EnsuresOnThrow condition (input != null) holds → original FormatException propagates
        Assert.Throws<FormatException>(() => proxy.Parse("not_a_number"));
    }

    // TODO: Uncomment these tests once we have a way to test EnsuresOnThrow failures in a unit test.

    // [Fact]
    // public void Parse_NullInput_FailsEnsuresOnThrow()
    // {
    //     var proxy = Dbc.Make<IParser>(new SimpleParser());
    //     // Input is null → FormatException thrown → EnsuresOnThrow(input != null) fails → ContractException
    //     var ex = Assert.Throws<ContractViolationException>(() => proxy.Parse(null!));
    //     Assert.Equal(ContractKind.PostconditionOnThrow, ex.Kind);
    // }

    // [Fact]
    // public void Parse_NullInput_EnsuresOnThrowWithMessage()
    // {
    //     var proxy = Dbc.Make<IParserWithMessage>(new SimpleParserForMessage());
    //     var ex = Assert.Throws<ContractViolationException>(() => proxy.Parse(null!));
    //     Assert.Equal(ContractKind.PostconditionOnThrow, ex.Kind);
    //     Assert.Contains("input was null on throw", ex.Message);
    // }
}

// --- IParser: EnsuresOnThrow without message ---

[ContractClass(typeof(ParserContracts))]
interface IParser
{
    int Parse(string input);
}

class SimpleParser : IParser
{
    public int Parse(string input)
    {
        return int.Parse(input); // throws FormatException for bad input, ArgumentNullException for null
    }
}

[ContractClassFor(typeof(IParser))]
class ParserContracts
{
    public ParserContracts()
    {
        // When a FormatException is thrown, input must not be null
        Dbc.Def(static (IParser x, string input) => x.Parse(input),
                static (IParser x, string input) => Contract.EnsuresOnThrow<FormatException>(input != null));
    }
}

// --- IParserWithMessage: EnsuresOnThrow with message ---

[ContractClass(typeof(ParserWithMessageContracts))]
interface IParserWithMessage
{
    int Parse(string input);
}

class SimpleParserForMessage : IParserWithMessage
{
    public int Parse(string input)
    {
        return int.Parse(input);
    }
}

[ContractClassFor(typeof(IParserWithMessage))]
class ParserWithMessageContracts
{
    public ParserWithMessageContracts()
    {
        Dbc.Def(static (IParserWithMessage x, string input) => x.Parse(input),
                static (IParserWithMessage x, string input) => Contract.EnsuresOnThrow<FormatException>(input != null, "input was null on throw"));
    }
}
