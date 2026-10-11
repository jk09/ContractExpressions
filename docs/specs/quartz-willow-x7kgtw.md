# ContractCheck – source-generated proxies (Native AOT, lower call overhead)

## 1. Metadata

| Field | Value |
| --- | --- |
| **Feature name** | Source-generated contract proxies |
| **Spec ID** | quartz-willow-x7kgtw |
| **Status** | Draft |
| **Author** | Claude (Claude Code session for jk09) |
| **Owner** | jk09 |
| **Reviewers** | jk09 |
| **Created on** | 2026-10-11 18:22 +00:00 |
| **Last updated** | 2026-10-11 18:22 +00:00 |
| **Affected features** | `ContractCheck/Features/Enforcement` (proxy factory selection); new project `ContractCheck.Generators` (or the analyzer assembly from canyon-tundra-ymf1vj) |
| **Target release** | Not scheduled – to be implemented later (owner's decision, 2026-10-11) |
| **Related links** | amber-harbor-v9p6ri (ContractCheck), canyon-tundra-ymf1vj (analyzer project this builds on) |

## 2. Summary

- **Problem statement:** `Dbc.Make` builds proxies with `System.Reflection.DispatchProxy`, which emits code at run time. That rules out Native AOT and trimming, costs an `object[]` allocation, boxing and a reflection `Invoke` per call, makes stack traces and debugging indirect, and limits contracts to interfaces.
- **Desired outcome:** A Roslyn source generator emits a typed proxy class for each contract interface used with `Dbc.Make`. `Dbc.Make` uses the generated proxy when one exists and falls back to `DispatchProxy` otherwise, with identical contract semantics. Apps can publish with Native AOT.

## 3. Background and Context

- **Current behavior:** `ContractAwareProxy : DispatchProxy` forwards every call to `ContractInvoker.Invoke(MethodInfo, object?[] args)`, which checks compiled clauses and calls the target through `MethodInfo.Invoke`. Clauses are expression trees compiled with `Expression.Compile` (interpreted under AOT).
- **Motivation:** Suggested improvement 2 after amber-harbor-v9p6ri; the owner asked for a spec now and implementation later.
- **Related references:** `DispatchProxy` is annotated `RequiresDynamicCode`; System.Text.Json and the configuration binder solved the same problem with source generators; the ContractCheck analyzer (canyon-tundra-ymf1vj) already parses `Dbc.Def` calls at compile time.

## 4. Goals

- Native AOT and trimming compatible `Dbc.Make` for interfaces with generated proxies.
- No `object[]` or reflection `Invoke` per call; clause evaluation without `Expression.Compile` where the generator can translate the clause.
- Same observable behaviour as the `DispatchProxy` path – every existing test passes on both paths.

## 5. Non-Goals

- Changing the `Dbc.Def` / `Dbc.Make` API or contract semantics.
- Removing the `DispatchProxy` fallback.
- Class (non-interface) proxies – possible follow-up once generated proxies exist.

## 6. User Stories

- As an app developer, I want to publish with `PublishAot=true` and keep my contracts checked.
- As a library author, I want contract checks cheap enough to leave on in hot paths.
- As a developer debugging a failure, I want the proxy frame in the stack trace to be readable C#.

## 7. Functional Requirements

1. An incremental source generator finds closed interface types used as `Dbc.Make<T>` type arguments (and interfaces marked `[ContractClass]` in the compilation) and emits `sealed class <Interface>ContractProxy : <Interface>` per interface, including inherited members, properties, indexers, events, default interface members, generic methods, `ref`/`out`/`in` parameters and async members.
2. Generated proxies register themselves through a `[ModuleInitializer]` in a factory table; `Dbc.Make<T>` uses the generated factory when present, otherwise `DispatchProxy`.
3. Each generated member calls the target directly and invokes the shared checking pipeline with typed arguments (no `object[]` on the hot path when no clause needs boxed arguments).
4. Clause translation: clauses of `Dbc.Def` calls the generator can see are emitted as C# methods (`Result`, `OldValue`, quantifiers, placeholders translated); clauses it can't translate keep the expression-tree path. Run-time validation still runs once per interface.
5. A project property `ContractCheckGenerateProxies` (default `true`) turns the generator off; an MSBuild warning explains when an interface can't be generated (e.g. inaccessible).
6. Failure messages, `ContractFailed` events, checking levels and async behaviour are identical on both paths.

## 8. Non-Functional Requirements

- Performance: a checked call with one precondition allocates nothing on the generated path; benchmark (BenchmarkDotNet) against the `DispatchProxy` path in the repo.
- Compatibility: generated code compiles with C# 12 (net8.0); generator built against Roslyn 4.8.
- Reliability: generator never throws; generated code has `#nullable enable` and `[GeneratedCode]`.

## 9. UX / UI Notes

- No new user-facing API beyond the opt-out property; generated sources visible under Analyzers in the IDE.
- Edge cases: interfaces from referenced assemblies (generate in the consuming compilation), internal interfaces (`InternalsVisibleTo` not needed when generated in the same assembly), generic interfaces (generic proxy class), explicit interface implementations, `ref struct` parameters (fall back to `DispatchProxy`).

## 10. Technical Notes

- Proposed approach: build on the analyzer's `Dbc.Def` parsing; emit proxies with `IndentedTextWriter`/raw strings; share a typed `ContractInvoker` core (`Invoke<TResult>(…)` with a pre-bound member descriptor) used by both paths.
- Dependencies: Microsoft.CodeAnalysis.CSharp 4.8 (already used by the analyzer); BenchmarkDotNet (benchmarks project only).
- Risks / unknowns: semantic drift between the two paths (mitigate: run the whole test suite twice, once with generated proxies disabled); translating expression trees to C# text faithfully (quantifiers, `OldValue` captures); size of generated code for large interfaces.
- Open questions: generate only for `Dbc.Make<T>` call sites, or for every interface with contracts? Ship the generator in the `ContractCheck` package or separately?

## 11. Acceptance Criteria

- [ ] AC1 A sample app with contracts publishes with `PublishAot=true` without AOT/trim warnings from ContractCheck and checks its contracts at run time.
- [ ] AC2 The full test suite passes with generated proxies and again with `ContractCheckGenerateProxies=false`.
- [ ] AC3 A benchmark shows a checked call on the generated path allocates nothing for a member with one precondition and is faster than the `DispatchProxy` path.
- [ ] AC4 Interfaces the generator can't handle fall back to `DispatchProxy` with a diagnostic, never a build break.
- [ ] AC5 README and ADR document the two proxy paths.

## 12. Testing / Verification

- Manual test plan: publish the AOT sample; inspect generated sources in the IDE.
- Automated test coverage: the existing suite on both paths; generator snapshot tests; benchmarks in CI (non-blocking).
- Regression considerations: identical messages and events on both paths.

## 13. Rollout / Follow-up

- Rollout plan: behind the default-on property; one release with both paths before considering `DispatchProxy` deprecation for AOT apps.
- Follow-up work: class proxies (generated subclasses overriding virtual members).

## 14. Changes during implementation

- _None yet._
