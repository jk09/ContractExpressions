using MyTool.Agent;

namespace MyTool.Agent.Tests;

public class AgenticCodeGenerationToolTests
{
    [Fact]
    public void Generate_DivisionPrompt_CreatesClassContractsAndTests()
    {
        string root = CreateTempDirectory();
        try
        {
            var options = new AgentToolOptions(
                Prompt: "Generate a program for division of two integers. Precondition: the divisor != 0",
                WorkspaceRoot: root,
                OutputDirectory: "generated",
                AugmentExisting: false);

            var tool = new AgenticCodeGenerationTool(new SemanticKernelRuntime());

            GenerationOutput output = tool.Generate(options);

            Assert.Contains(output.Artifacts, artifact => artifact.RelativePath.EndsWith("Division.cs", StringComparison.Ordinal));
            Assert.Contains(output.Artifacts, artifact => artifact.RelativePath.EndsWith("Division.Contracts.cs", StringComparison.Ordinal));
            Assert.Contains(output.Artifacts, artifact => artifact.RelativePath.EndsWith("Division.Tests.cs", StringComparison.Ordinal));

            string contracts = File.ReadAllText(Path.Combine(root, "generated", "Division.Contracts.cs"));
            Assert.Contains("Dbc.Def", contracts);
            Assert.Contains("Contract.Requires(b != 0)", contracts);
            Assert.DoesNotContain("the b", contracts);

            string tests = File.ReadAllText(Path.Combine(root, "generated", "Division.Tests.cs"));
            Assert.Contains("[Fact]", tests);
            Assert.Contains("[Property", tests);
            Assert.Contains("DbcPropertyTest.Check", tests);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Generate_WhenClassExists_SkipsClassFileAndAddsSidecars()
    {
        string root = CreateTempDirectory();
        try
        {
            string existingDir = Path.Combine(root, "src");
            Directory.CreateDirectory(existingDir);
            File.WriteAllText(Path.Combine(existingDir, "Division.cs"), "namespace GeneratedContracts; public class Division { public int Divide(int a, int b) => a / b; }");

            var options = new AgentToolOptions(
                Prompt: "Generate a program for division of two integers. Precondition: the divisor != 0",
                WorkspaceRoot: root,
                OutputDirectory: "generated",
                AugmentExisting: true);

            var tool = new AgenticCodeGenerationTool(new SemanticKernelRuntime());

            GenerationOutput output = tool.Generate(options);

            Assert.DoesNotContain(output.Artifacts, artifact => artifact.RelativePath.EndsWith("Division.cs", StringComparison.Ordinal));
            Assert.Contains(output.Artifacts, artifact => artifact.RelativePath.EndsWith(Path.Combine("src", "Division.Contracts.cs"), StringComparison.Ordinal));
            Assert.Contains(output.Artifacts, artifact => artifact.RelativePath.EndsWith(Path.Combine("src", "Division.Tests.cs"), StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Property]
    public bool Parse_RemovesAgentPrefix(string tail)
    {
        AgentToolOptions options = AgentToolOptions.Parse(["agent>>", tail]);
        return options.Prompt == tail.Trim();
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mytool-agent-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
