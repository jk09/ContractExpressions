using Microsoft.SemanticKernel;

namespace MyTool.Agent;

public sealed class SemanticKernelRuntime
{
    public SemanticKernelRuntime()
    {
        Kernel = Kernel.CreateBuilder().Build();
    }

    public Kernel Kernel { get; }
}
