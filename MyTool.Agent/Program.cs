using MyTool.Agent;

AgentToolOptions options = AgentToolOptions.Parse(args);
if (string.IsNullOrWhiteSpace(options.Prompt))
{
    Console.Error.WriteLine("Usage: mytool agent \"<prompt>\" [--output <dir>] [--workspace <dir>] [--augment-existing]");
    return 1;
}

var runtime = new SemanticKernelRuntime();
var generator = new AgenticCodeGenerationTool(runtime);
GenerationOutput output = generator.Generate(options);

Console.WriteLine("Plan:");
foreach (string step in output.PlanSteps)
{
    Console.WriteLine($"- {step}");
}

Console.WriteLine();
Console.WriteLine("Generated files:");
foreach (GeneratedArtifact artifact in output.Artifacts)
{
    Console.WriteLine($"- {artifact.RelativePath}");
}

if (output.Warnings.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Warnings:");
    foreach (string warning in output.Warnings)
    {
        Console.WriteLine($"- {warning}");
    }
}

return 0;
