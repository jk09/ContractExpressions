namespace MyTool.Agent;

public static class WorkspaceClassLocator
{
    public static string? FindClassFile(string workspaceRoot, string className)
    {
        foreach (string file in Directory.EnumerateFiles(workspaceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("/bin/") || file.Contains("/obj/"))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (text.Contains($"class {className}", StringComparison.Ordinal))
            {
                return file;
            }
        }

        return null;
    }
}
