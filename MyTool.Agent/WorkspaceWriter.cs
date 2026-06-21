namespace MyTool.Agent;

public sealed class WorkspaceWriter
{
    public void Write(string workspaceRoot, IReadOnlyList<GeneratedArtifact> artifacts)
    {
        foreach (GeneratedArtifact artifact in artifacts)
        {
            string fullPath = Path.Combine(workspaceRoot, artifact.RelativePath);
            string directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(fullPath, artifact.Content);
        }
    }
}
