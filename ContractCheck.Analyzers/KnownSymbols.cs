using Microsoft.CodeAnalysis;

namespace ContractCheck.Analyzers;

/// <summary>The types the analyzers recognise, resolved once per compilation.</summary>
internal sealed class KnownSymbols
{
    private KnownSymbols(Compilation compilation, INamedTypeSymbol dbc, INamedTypeSymbol contract)
    {
        Dbc = dbc;
        Contract = contract;
        Out = compilation.GetTypeByMetadataName("ContractCheck.Dbc+Out`1");
        Ref = compilation.GetTypeByMetadataName("ContractCheck.Dbc+Ref`1");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    }

    public INamedTypeSymbol Dbc { get; }

    public INamedTypeSymbol Contract { get; }

    public INamedTypeSymbol? Out { get; }

    public INamedTypeSymbol? Ref { get; }

    public INamedTypeSymbol? TaskOfT { get; }

    public INamedTypeSymbol? ValueTaskOfT { get; }

    public static KnownSymbols? Create(Compilation compilation) =>
        compilation.GetTypeByMetadataName("ContractCheck.Dbc") is { } dbc
        && compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.Contract") is { } contract
            ? new KnownSymbols(compilation, dbc, contract)
            : null;
}
