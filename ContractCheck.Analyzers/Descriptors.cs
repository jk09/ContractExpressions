using Microsoft.CodeAnalysis;

namespace ContractCheck.Analyzers;

/// <summary>The ContractCheck diagnostics. Messages match the run-time errors of <c>Dbc.Def</c> where both exist.</summary>
internal static class Descriptors
{
    public const string Category = "ContractCheck";
    private const string HelpLink = "https://github.com/jk09/ContractExpressions#analyzer";

    public static readonly DiagnosticDescriptor InvalidSelector = Create(
        "CCK0001", "Invalid contract selector", DiagnosticSeverity.Error,
        "The first lambda of Dbc.Def/Dbc.DefSetter must select one member of the interface, passing the lambda's own parameters in order and Dbc.Out<T>/Dbc.Ref<T> placeholders for out/ref parameters.");

    public static readonly DiagnosticDescriptor InvalidClause = Create(
        "CCK0002", "Invalid contract clause", DiagnosticSeverity.Error,
        "Each clause lambda must be a single call to Contract.Requires, Requires<TException>, Ensures, EnsuresOnThrow<TException>, Invariant, Assert or Assume.");

    public static readonly DiagnosticDescriptor ResultMisuse = Create(
        "CCK0003", "Contract.Result misused", DiagnosticSeverity.Error,
        "Contract.Result<T>() is only valid in Ensures clauses of members that return a value, with T the return type or the awaited type.");

    public static readonly DiagnosticDescriptor OldValueMisuse = Create(
        "CCK0004", "Contract.OldValue misused", DiagnosticSeverity.Error,
        "Contract.OldValue is only valid in Ensures and EnsuresOnThrow clauses, on a non-literal expression without Result or OldValue.");

    public static readonly DiagnosticDescriptor ByRefMisuse = Create(
        "CCK0005", "Out or ref value misused", DiagnosticSeverity.Error,
        "Dbc.Out<T>/Dbc.Ref<T> placeholders and Contract.ValueAtReturn must stand for out/ref parameters of the selected member and be read where they have a value; Contract.EndContractBlock has no place in a clause.");

    public static readonly DiagnosticDescriptor AttributeCompiledAway = Create(
        "CCK0006", "Contract attribute compiled away", DiagnosticSeverity.Warning,
        "[Pure], [ContractOption], [ContractRuntimeIgnored], [ContractClass] and [ContractClassFor] are [Conditional(\"CONTRACTS_FULL\")] ([ContractClass] also on DEBUG): in a file that doesn't define CONTRACTS_FULL the compiler removes them, so ContractCheck never sees them.",
        "[{0}] is removed by the compiler because CONTRACTS_FULL isn't defined in this file{1}; add #define CONTRACTS_FULL as its first line");

    public static readonly DiagnosticDescriptor ContractCallTerminatesProcess = Create(
        "CCK0007", "Contract call terminates the process", DiagnosticSeverity.Error,
        "Contract.Requires, Ensures, EnsuresOnThrow and Invariant need the retired CodeContracts rewriter: called outside an expression-tree clause they call Environment.FailFast. The compiler removes them where CONTRACTS_FULL isn't defined, except Requires<TException>, which is not conditional.",
        "Contract.{0} outside a Dbc.Def clause terminates the process, because {1} and the call needs the retired CodeContracts rewriter; define the check in a contract class or use an if/throw guard");

    public static readonly DiagnosticDescriptor InheritedPrecondition = Create(
        "CCK0008", "Precondition added to an inherited member", DiagnosticSeverity.Warning,
        "A derived interface that adds preconditions to an inherited member breaks Liskov substitution: code that holds the base interface can't know them. CodeContracts reported this as error CC1033.",
        "Contract.{0} adds a precondition to {1}.{2} through {3}, which code holding a {1} can't know (Liskov substitution); define it in the contract of {1}, or check it as a postcondition or invariant");

    private static DiagnosticDescriptor Create(string id, string title, DiagnosticSeverity severity, string description, string message = "{0}") =>
        new(id, title, message, Category, severity, isEnabledByDefault: true, description, HelpLink);
}
