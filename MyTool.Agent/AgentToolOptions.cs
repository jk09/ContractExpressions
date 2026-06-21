namespace MyTool.Agent;

public sealed record AgentToolOptions(string Prompt, string WorkspaceRoot, string OutputDirectory, bool AugmentExisting)
{
    public static AgentToolOptions Parse(string[] args)
    {
        string workspaceRoot = Directory.GetCurrentDirectory();
        string outputDirectory = "generated";
        bool augmentExisting = false;
        var promptTokens = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];
            if (string.Equals(current, "agent", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(current, "--workspace", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                workspaceRoot = args[++i];
                continue;
            }

            if (string.Equals(current, "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                outputDirectory = args[++i];
                continue;
            }

            if (string.Equals(current, "--augment-existing", StringComparison.OrdinalIgnoreCase))
            {
                augmentExisting = true;
                continue;
            }

            promptTokens.Add(current);
        }

        string prompt = string.Join(" ", promptTokens).Trim();
        const string prefix = "agent>>";
        int prefixIndex = prompt.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIndex >= 0)
        {
            prompt = prompt[(prefixIndex + prefix.Length)..].Trim();
        }

        return new AgentToolOptions(prompt, Path.GetFullPath(workspaceRoot), outputDirectory, augmentExisting);
    }
}
