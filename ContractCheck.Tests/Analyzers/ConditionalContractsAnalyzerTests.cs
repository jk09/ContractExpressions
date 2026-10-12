using System.Collections.Immutable;
using ContractCheck.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using static ContractCheck.Tests.Analyzers.AnalyzerHarness;
using static ContractCheck.Tests.Analyzers.Snippets;

namespace ContractCheck.Tests.Analyzers;

/// <summary>CCK0006 (contract attributes compiled away), its code fix, and CCK0007 (Contract calls that terminate the process).</summary>
public sealed class ConditionalContractsAnalyzerTests
{
    private const string ContractInterface = """
        using System.Diagnostics.Contracts;
        using ContractCheck;

        [ContractClass(typeof(GaugeContracts))]
        public interface IGauge
        {
            [Pure]
            int Read();
        }

        """;

    private const string ContractClass = """

        [ContractClassFor(typeof(IGauge))]
        internal sealed class GaugeContracts
        {
            private GaugeContracts() => Dbc.Def((IGauge x) => x.Read(), (IGauge x) => Contract.Ensures(Contract.Result<int>() >= 0));
        }
        """;

    /// <summary>A contract class without clauses, which compiles without CONTRACTS_FULL.</summary>
    private const string EmptyContractClass = """

        [ContractClassFor(typeof(IGauge))]
        internal sealed class GaugeContracts
        {
        }
        """;

    // CCK0006 – attributes compiled away

    [Fact]
    public Task CCK0006_reports_Pure_without_CONTRACTS_FULL() => ReportsAsync(
        "CCK0006",
        """
        using System.Diagnostics.Contracts;
        using ContractCheck;

        public interface IGauge
        {
            [[|Pure|]]
            int Read();
        }
        """,
        "[Pure] is removed by the compiler because CONTRACTS_FULL isn't defined in this file; add #define CONTRACTS_FULL as its first line");

    [Fact]
    public Task CCK0006_reports_ContractClass_for_Release_builds() => ReportsAsync(
        "CCK0006",
        """
        using System.Diagnostics.Contracts;
        using ContractCheck;

        [[|ContractClass(typeof(GaugeContracts))|]]
        public interface IGauge
        {
        }

        internal abstract class GaugeContracts
        {
        }
        """,
        ParseOptions("DEBUG"),
        "[ContractClass] is removed by the compiler because CONTRACTS_FULL isn't defined in this file (in Release builds, where DEBUG isn't defined either)");

    [Fact]
    public Task CCK0006_reports_ContractOption_spelled_with_its_full_name() => ReportsAsync(
        "CCK0006",
        """
        using ContractCheck;

        [[|System.Diagnostics.Contracts.ContractOptionAttribute("runtime", "checking", false)|]]
        public interface IGauge
        {
        }
        """,
        "[ContractOption] is removed by the compiler");

    [Fact]
    public Task CCK0006_reports_an_undef_after_a_project_wide_define() => ReportsAsync(
        "CCK0006",
        """
        #undef CONTRACTS_FULL
        using System.Diagnostics.Contracts;
        using ContractCheck;

        public static class Rates
        {
            [[|ContractRuntimeIgnored|]]
            public static int Expensive() => 0;
        }
        """,
        ParseOptions("CONTRACTS_FULL"),
        "[ContractRuntimeIgnored] is removed by the compiler");

    [Fact]
    public async Task CCK0006_reports_every_attribute_of_a_file()
    {
        IEnumerable<string> messages = (await AnalyzeAsync(ContractInterface + EmptyContractClass))
            .Select(d => $"{d.Id} {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)[..d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).IndexOf(']', StringComparison.Ordinal)]}]")
            .Order(StringComparer.Ordinal);
        Assert.Equal(["CCK0006 [ContractClassFor]", "CCK0006 [ContractClass]", "CCK0006 [Pure]"], messages);
    }

    [Fact]
    public async Task CCK0006_is_silent_where_CONTRACTS_FULL_is_defined()
    {
        await SilentAsync("#define CONTRACTS_FULL\n" + ContractInterface + ContractClass);
        await SilentAsync(ContractInterface + ContractClass, ParseOptions("CONTRACTS_FULL"));
        await SilentAsync("#if DEBUG\n#define CONTRACTS_FULL\n#endif\n" + ContractInterface + ContractClass, ParseOptions("DEBUG"));
    }

    [Fact]
    public async Task CCK0006_ignores_attributes_ContractCheck_does_not_read()
    {
        await SilentAsync("""
            using System;
            using System.Diagnostics.Contracts;
            using ContractCheck;

            public static class Legacy
            {
                [ContractInvariantMethod]
                private static void ObjectInvariant()
                {
                }

                [ContractVerification(false)]
                public static void Unverified()
                {
                }

                [Obsolete]
                public static void Old()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task CCK0006_code_fix_adds_the_define_as_the_first_line()
    {
        string source = "// A contract interface.\r\n" + (ContractInterface + EmptyContractClass).ReplaceLineEndings("\r\n");
        Document document = CreateDocument(source);
        ImmutableArray<Diagnostic> diagnostics = await DiagnosticsAsync(document);
        Assert.Equal(3, diagnostics.Length);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics[0], (action, _) => actions.Add(action), TestContext.Current.CancellationToken);
        await new DefineContractsFullCodeFix().RegisterCodeFixesAsync(context);
        CodeAction fix = Assert.Single(actions);
        Assert.Equal("Add #define CONTRACTS_FULL", fix.Title);

        Document fixedDocument = await ApplyAsync(fix, document);
        Assert.Equal("#define CONTRACTS_FULL\r\n" + source, (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString());
        Assert.Empty(await DiagnosticsAsync(fixedDocument));
    }

    [Fact]
    public async Task CCK0006_fix_all_defines_the_symbol_once_per_file()
    {
        Document document = CreateDocument(ContractInterface + EmptyContractClass);
        ImmutableArray<Diagnostic> diagnostics = await DiagnosticsAsync(document);
        Assert.Equal(3, diagnostics.Length);
        var provider = new DefineContractsFullCodeFix();
        var fixAll = new FixAllContext(
            document,
            provider,
            FixAllScope.Document,
            "Add #define CONTRACTS_FULL",
            provider.FixableDiagnosticIds,
            new FixedDiagnostics(diagnostics),
            TestContext.Current.CancellationToken);

        CodeAction fix = (await provider.GetFixAllProvider().GetFixAsync(fixAll))!;
        Document fixedDocument = await ApplyAsync(fix, document);
        Assert.Equal("#define CONTRACTS_FULL\n" + ContractInterface + EmptyContractClass, (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString());
        Assert.Empty(await DiagnosticsAsync(fixedDocument));
    }

    // CCK0007 – Contract calls that terminate the process

    [Fact]
    public Task CCK0007_reports_Requires_in_a_method_body_with_CONTRACTS_FULL() => ReportsAsync(
        "CCK0007",
        """
        #define CONTRACTS_FULL
        using System.Diagnostics.Contracts;
        using ContractCheck;

        public sealed class Gauge
        {
            public void Set(int value)
            {
                [|Contract.Requires(value >= 0)|];
            }
        }
        """,
        "Contract.Requires outside a Dbc.Def clause terminates the process, because CONTRACTS_FULL is defined and the call needs the retired CodeContracts rewriter");

    [Fact]
    public Task CCK0007_reports_Ensures_in_a_lambda_with_a_project_wide_CONTRACTS_FULL() => ReportsAsync(
        "CCK0007",
        """
        using System;
        using System.Diagnostics.Contracts;
        using ContractCheck;

        public static class Checks
        {
            public static readonly Action<int> Check = value => [|Contract.Ensures(value >= 0)|];
        }
        """,
        ParseOptions("CONTRACTS_FULL"),
        "Contract.Ensures outside a Dbc.Def clause terminates the process");

    [Fact]
    public Task CCK0007_reports_typed_Requires_even_without_CONTRACTS_FULL() => ReportsAsync(
        "CCK0007",
        """
        using System;
        using System.Diagnostics.Contracts;
        using ContractCheck;

        public sealed class Gauge
        {
            public void Set(int value) => [|Contract.Requires<ArgumentOutOfRangeException>(value >= 0)|];
        }
        """,
        "Contract.Requires<ArgumentOutOfRangeException> outside a Dbc.Def clause terminates the process, because Requires<TException> is never compiled away");

    [Fact]
    public async Task CCK0007_is_silent_on_clauses_expression_trees_compiled_away_calls_and_assertions()
    {
        await SilentAsync(Define("""
            Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Requires<ArgumentNullException>(item != null));
            Expression<Action<int>> tree = value => Contract.Requires(value >= 0);
            Contract.Assert(tree != null);
            Contract.Assume(tree != null);
            Contract.EndContractBlock();
            """));
        await SilentAsync("""
            using System.Diagnostics.Contracts;
            using ContractCheck;

            public sealed class Gauge
            {
                public void Set(int value)
                {
                    Contract.Requires(value >= 0);
                    Contract.Ensures(value >= 0);
                    Contract.Invariant(value >= 0);
                }
            }
            """);
    }

    [Fact]
    public async Task Analyzers_stay_silent_without_ContractCheck()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "NoContractCheck",
            [CSharpSyntaxTree.ParseText("""
                using System.Diagnostics.Contracts;

                public interface IGauge
                {
                    [Pure]
                    int Read();
                }
                """, cancellationToken: TestContext.Current.CancellationToken)],
            References.Where(r => !string.Equals(r.Display, typeof(Dbc).Assembly.Location, StringComparison.Ordinal)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(await compilation.WithAnalyzers(AllAnalyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken));
    }

    private static Document CreateDocument(string source)
    {
        var workspace = new AdhocWorkspace();
        Project project = workspace.AddProject("Snippet", LanguageNames.CSharp)
            .WithMetadataReferences(References)
            .WithParseOptions(ParseOptions())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return project.AddDocument("IGauge.cs", source);
    }

    private static async Task<ImmutableArray<Diagnostic>> DiagnosticsAsync(Document document)
    {
        Compilation compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        return await compilation.WithAnalyzers(AllAnalyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Document> ApplyAsync(CodeAction fix, Document document)
    {
        ImmutableArray<CodeActionOperation> operations = await fix.GetOperationsAsync(TestContext.Current.CancellationToken);
        Solution changed = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
        return changed.GetDocument(document.Id)!;
    }

    /// <summary>Hands the fix-all provider the diagnostics computed by the test.</summary>
    private sealed class FixedDiagnostics(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>([]);

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
    }
}
