using System.Collections.Immutable;
using System.Globalization;
using ContractCheck.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ContractCheck.Tests.Analyzers;

/// <summary>
/// Compiles C# snippets against the framework and ContractCheck and runs the ContractCheck analyzers on them. Snippets mark
/// the expected diagnostic span with <c>[|</c> and <c>|]</c>.
/// </summary>
internal static class AnalyzerHarness
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> LazyReferences = new(LoadReferences);

    public static ImmutableArray<MetadataReference> References => LazyReferences.Value;

    public static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers { get; } =
        [new ContractDefinitionAnalyzer(), new ConditionalContractsAnalyzer()];

    public static CSharpParseOptions ParseOptions(params string[] preprocessorSymbols) =>
        new(LanguageVersion.CSharp12, preprocessorSymbols: preprocessorSymbols);

    public static CSharpCompilation Compile(string source, CSharpParseOptions? options = null) =>
        CSharpCompilation.Create(
            "Snippet",
            [CSharpSyntaxTree.ParseText(source, options ?? ParseOptions(), path: "Snippet.cs", cancellationToken: TestContext.Current.CancellationToken)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    /// <summary>Runs the analyzers on a snippet that must compile without errors and returns their diagnostics.</summary>
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, CSharpParseOptions? options = null)
    {
        CSharpCompilation compilation = Compile(source, options);
        Diagnostic[] errors = [.. compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error)];
        Assert.True(errors.Length == 0, "The snippet doesn't compile:\n" + string.Join("\n", errors.Select(e => e.ToString())));
        return await compilation.WithAnalyzers(AllAnalyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Asserts that the analyzers report exactly one diagnostic: <paramref name="id"/>, at the marked span, with every fragment in its message.</summary>
    public static async Task<Diagnostic> ReportsAsync(string id, string markup, params string[] fragments) =>
        await ReportsAsync(id, markup, null, fragments);

    public static async Task<Diagnostic> ReportsAsync(string id, string markup, CSharpParseOptions? options, params string[] fragments)
    {
        (string source, TextSpan span) = Parse(markup);
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, options);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(source.Substring(span.Start, span.Length), source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        foreach (string fragment in fragments)
        {
            Assert.Contains(fragment, message, StringComparison.Ordinal);
        }

        return diagnostic;
    }

    /// <summary>Asserts that the analyzers report nothing.</summary>
    public static async Task SilentAsync(string source, CSharpParseOptions? options = null)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, options);
        Assert.True(diagnostics.IsEmpty, "Unexpected diagnostics:\n" + string.Join("\n", diagnostics.Select(d => d.ToString())));
    }

    private static (string Source, TextSpan Span) Parse(string markup)
    {
        int start = markup.IndexOf("[|", StringComparison.Ordinal);
        int end = markup.IndexOf("|]", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "The snippet must mark the expected span with [| and |].");
        string source = markup.Remove(end, 2).Remove(start, 2);
        return (source, new TextSpan(start, end - start - 2));
    }

    /// <summary>The framework assemblies the tests run on, plus ContractCheck.</summary>
    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        IEnumerable<string> framework = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.Ordinal));
        return [.. framework.Append(typeof(Dbc).Assembly.Location).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }
}
