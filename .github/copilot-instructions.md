# Project Guidelines

## Architecture

ContractExpressions is a runtime Design-by-Contract library for .NET 9. It replaces the discontinued .NET Framework CodeContracts IL-rewriting approach with dynamic expression-tree evaluation via `DispatchProxy`.

**Core flow:** Contracts are defined as `Expression<Action<TIntf>>` lambdas in `[ContractClassFor]` constructors using `Dbc.Def(...)` → parsed by `DbcDefVisitor` (ExpressionVisitor) → compiled delegates stored in `ContractRegistry` (singleton) → enforced at runtime by `ContractAwareProxy<T>` which intercepts every interface call.

**Proxy invocation order:** Preconditions → CollectOldValues → target method → Invariants → Postconditions (on exception: PostconditionsOnThrow instead of Invariants+Postconditions).

Only two public APIs exist: `Dbc.Def(...)` (register contracts) and `Dbc.Make<T>(target)` (create proxy). Everything else is `internal`.

## Code Style

- C# 12, .NET 9.0, file-scoped namespaces, nullable enabled, implicit usings
- Primary constructors where appropriate (see `DbcDefVisitor(Type contractType)`)
- One class per file, filename matches class name
- New types should be `internal` unless they extend the public API
- Expression visitors follow naming: `Contract*Patcher` for rewriters, `*Visitor` for readers
- Contract definitions use `static` lambdas to avoid closures

## Build and Test

```bash
dotnet build                  # Builds ContractExpressions + ContractExpressions.Tests
dotnet test                   # Runs all 25 xUnit v3 tests
dotnet test --filter "FullyQualifiedName~MyList"  # Subset
```

## Project Conventions

**Contract definition pattern** — always in `[ContractClassFor]` constructors:
```csharp
[ContractClassFor(typeof(IMyList))]
class ListContracts {
    public ListContracts() {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
                static (IMyList x, object a) => Contract.Requires(a != null),
                static (IMyList x, object a) => Contract.Ensures(Contract.Result<int>() >= 0));
    }
}
```

**Expression visitor pipeline** (postconditions): raw expression → `ContractResultPatcher` → `ContractOldValuePatcher` → `ContractValueAtReturnPatcher` → compiled delegate. Each patcher rewrites `Contract.*` calls into `ContractPatch.*` calls that read from `ContractContext`.

**Test files** must start with `#define CONTRACTS_FULL`. Test classes exercising contract failures must use `IClassFixture<ContractFailureUnwindFixture>` which subscribes `Contract.ContractFailed` with `e.SetUnwind()`. Each test file defines its own interfaces, implementations, and contract classes—self-contained.

**`Dbc.Def` overloads** cover 0–7 parameters × void/return (16 overloads in `Dbc.cs`). When adding parameter support beyond 7, add both `Action` and `Func` variants.

## Key Files

| File | Role |
|------|------|
| `ContractExpressions/Dbc.cs` | Public API — `Def` overloads + `Make<T>` |
| `ContractExpressions/ContractAwareProxy.cs` | DispatchProxy — intercepts calls, runs contracts |
| `ContractExpressions/DbcDefVisitor.cs` | Parses contract expressions into compiled delegates |
| `ContractExpressions/ContractPatch.cs` | Runtime evaluation replacing `Contract.*` methods |
| `ContractExpressions/ContractRegistry.cs` | Singleton storing all registered contracts |
| `ContractExpressions/ContractContext.cs` | Carries Result/OldValues/ValuesAtReturn through evaluation |
| `.specs/` | Development specifications and task backlog |
| `CodeContracts/` | Git submodule — original Microsoft CodeContracts (reference only) |
