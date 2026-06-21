namespace MyTool.Agent;

public sealed record ParameterSpec(string Name, string TypeName);

public sealed record ContractSpec(string Kind, string Predicate, bool IsExplicit);

public sealed record MethodSpec(
    string Name,
    string ReturnType,
    string BodyExpression,
    IReadOnlyList<ParameterSpec> Parameters,
    IReadOnlyList<ContractSpec> Contracts);

public sealed record DomainSpec(
    string Namespace,
    string ClassName,
    string Prompt,
    IReadOnlyList<MethodSpec> Methods,
    bool ClassAlreadyExists,
    string? ExistingClassPath);

public sealed record GeneratedArtifact(string RelativePath, string Content);

public sealed record GenerationOutput(
    IReadOnlyList<string> PlanSteps,
    IReadOnlyList<GeneratedArtifact> Artifacts,
    IReadOnlyList<string> Warnings);
