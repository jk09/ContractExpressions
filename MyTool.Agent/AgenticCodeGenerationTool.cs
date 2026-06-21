using ContractExpressions4;

namespace MyTool.Agent;

public sealed class AgenticCodeGenerationTool
{
    private readonly PromptInterpreter _interpreter = new();
    private readonly ContractInferenceEngine _inference = new();
    private readonly CSharpContractTemplateGenerator _generator = new();
    private readonly WorkspaceWriter _writer = new();

    public AgenticCodeGenerationTool(SemanticKernelRuntime runtime)
    {
        Runtime = runtime;
    }

    public SemanticKernelRuntime Runtime { get; }

    public GenerationOutput Generate(AgentToolOptions options)
    {
        var steps = new List<string>
        {
            "Parse prompt into domain specification.",
            $"Infer contract predicates using {nameof(Dbc)}.{nameof(Dbc.Def)} patterns.",
            "Generate class, contract, and test files.",
            "Write generated artifacts into the workspace."
        };

        DomainSpec parsed = _interpreter.Parse(options.Prompt, options.WorkspaceRoot, options.AugmentExisting);
        DomainSpec enriched = _inference.Enrich(parsed);
        IReadOnlyList<string> warnings = _inference.Verify(enriched);

        string outputDirectory = enriched.ClassAlreadyExists && enriched.ExistingClassPath is not null
            ? Path.GetRelativePath(options.WorkspaceRoot, Path.GetDirectoryName(enriched.ExistingClassPath)!)
            : options.OutputDirectory;

        IReadOnlyList<GeneratedArtifact> artifacts = _generator.Generate(enriched, outputDirectory);
        _writer.Write(options.WorkspaceRoot, artifacts);

        return new GenerationOutput(steps, artifacts, warnings);
    }
}
