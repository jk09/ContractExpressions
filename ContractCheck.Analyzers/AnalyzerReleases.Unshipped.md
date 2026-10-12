; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CCK0001 | ContractCheck | Error | ContractDefinitionAnalyzer: invalid Dbc.Def/Dbc.DefSetter selector
CCK0002 | ContractCheck | Error | ContractDefinitionAnalyzer: invalid contract clause
CCK0003 | ContractCheck | Error | ContractDefinitionAnalyzer: Contract.Result misused
CCK0004 | ContractCheck | Error | ContractDefinitionAnalyzer: Contract.OldValue misused
CCK0005 | ContractCheck | Error | ContractDefinitionAnalyzer: out/ref value misused
CCK0006 | ContractCheck | Warning | ConditionalContractsAnalyzer: contract attribute compiled away
CCK0007 | ContractCheck | Error | ConditionalContractsAnalyzer: Contract call that terminates the process
CCK0008 | ContractCheck | Warning | ContractDefinitionAnalyzer: precondition added to an inherited member
