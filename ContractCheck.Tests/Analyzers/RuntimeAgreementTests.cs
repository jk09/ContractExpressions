using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static ContractCheck.Tests.Analyzers.AnalyzerHarness;

namespace ContractCheck.Tests.Analyzers;

/// <summary>
/// Runs the analyzers on this test project's own sources, which define hundreds of contracts the run time accepts and
/// dozens it rejects: the analyzers must report errors exactly for the rejected ones (spec canyon-tundra-ymf1vj FR 3).
/// </summary>
public sealed class RuntimeAgreementTests
{
    [Fact]
    public async Task Analyzers_report_errors_exactly_for_the_definitions_the_run_time_rejects()
    {
        string root = RepositoryRoot();
        string testsDirectory = Path.Combine(root, "ContractCheck.Tests");
        CSharpParseOptions options = ParseOptions("DEBUG", "NET", "NET10_0");
        List<SyntaxTree> trees = [.. Directory.EnumerateFiles(testsDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path, cancellationToken: TestContext.Current.CancellationToken))];
        trees.Add(CSharpSyntaxTree.ParseText(
            "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Net.Http; " +
            "global using System.Threading; global using System.Threading.Tasks; global using Xunit; global using ContractCheck;",
            options,
            cancellationToken: TestContext.Current.CancellationToken));

        string self = typeof(RuntimeAgreementTests).Assembly.Location;
        IEnumerable<MetadataReference> references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => !string.Equals(path, self, StringComparison.Ordinal))
            .Select(path => MetadataReference.CreateFromFile(path));
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ContractCheck.Tests",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        Diagnostic[] errors = [.. compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error)];
        Assert.True(errors.Length == 0, "The test sources don't compile:\n" + string.Join("\n", errors.Take(20).Select(e => e.ToString())));

        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(AllAnalyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        List<string> problems = [];

        // Errors only where a test expects Dbc.Def to throw; warnings only for preconditions on inherited members (CCK0008).
        foreach (Diagnostic diagnostic in diagnostics)
        {
            MethodDeclarationSyntax? method = diagnostic.Location.SourceTree!.GetRoot(TestContext.Current.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
            bool expected = diagnostic.Severity == DiagnosticSeverity.Error
                ? method is not null && method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(ExpectsDefinitionError)
                : diagnostic.Id == "CCK0008";
            if (!expected)
            {
                problems.Add($"unexpected {Describe(diagnostic, root)}");
            }
        }

        // Every Dbc.Def/DefSetter call the suite expects to throw gets an error.
        foreach (InvocationExpressionSyntax expectation in trees.SelectMany(t => t.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()).Where(ExpectsDefinitionError))
        {
            if (expectation.ArgumentList.Arguments.FirstOrDefault()?.Expression is LambdaExpressionSyntax definition
                && definition.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsDefinition)
                && !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == expectation.SyntaxTree && definition.Span.Contains(d.Location.SourceSpan)))
            {
                FileLinePositionSpan line = expectation.GetLocation().GetLineSpan();
                problems.Add($"missed {Path.GetRelativePath(root, line.Path)}({line.StartLinePosition.Line + 1}): no analyzer error for a definition the run time rejects");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static bool ExpectsDefinitionError(InvocationExpressionSyntax invocation) =>
        invocation.Expression.ToString() is "Expect.DefinitionError" or "Assert.Throws<InvalidOperationException>";

    private static bool IsDefinition(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Dbc" }, Name.Identifier.ValueText: "Def" or "DefSetter" };

    private static string Describe(Diagnostic diagnostic, string root)
    {
        FileLinePositionSpan line = diagnostic.Location.GetLineSpan();
        return $"{Path.GetRelativePath(root, line.Path)}({line.StartLinePosition.Line + 1}): {diagnostic.Id} {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContractExpressions.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (ContractExpressions.sln) was not found above " + AppContext.BaseDirectory);
    }
}
