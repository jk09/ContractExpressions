# ContractCheck – fuse ContractExpressions and ContractExpressions4

## 1. Metadata

| Field | Value |
| --- | --- |
| **Feature name** | ContractCheck – unified Design-by-Contract library |
| **Spec ID** | amber-harbor-v9p6ri |
| **Status** | Active |
| **Author** | Claude (Claude Code session for jk09) |
| **Owner** | jk09 |
| **Reviewers** | jk09 |
| **Created on** | 2026-10-11 12:05 +00:00 |
| **Last updated** | 2026-10-11 18:17 +00:00 |
| **Affected features** | `ContractCheck/Features/Definitions`, `ContractCheck/Features/Enforcement`, `ContractCheck/Features/Failures` (all new); removed projects: `ContractExpressions`, `ContractExpressions4`, `ContractExpressions4.Check`, `MyTool.Agent` (+ their test projects) |
| **Target release** | ContractCheck 0.1.0 |
| **Related links** | Legacy reference: `CodeContracts` submodule (Foxtrot rewriter, `Foxtrot/Tests`, `Demo`) |

## 2. Summary

- **Problem statement:** The repo has two independent implementations of the same `Dbc.Def` / `Dbc.Make` API – `ContractExpressions` (CE, human) and `ContractExpressions4` (CE4, AI). They are **not** semantically equivalent (see §3), each covers only part of `System.Diagnostics.Contracts`, CE4 fails 11 of its own 133 tests, and the test suites are near-duplicates.
- **Desired outcome:** One library, `ContractCheck`, that keeps the `Dbc.Def` / `Dbc.Make` API of both projects, resolves every semantic conflict explicitly (legacy CodeContracts as tie-breaker), covers all of `System.Diagnostics.Contracts` that a functional, expression-tree API can express, and ships with one complete test suite, an FsCheck integration, a Claude skill that infers contracts and verifies them with property tests, and a public README.

## 3. Background and Context

- **Current behavior:** Baseline on 2026-10-11 (.NET 9.0.20): CE tests 70/70 pass; CE4 tests 122/133 pass (Assert/Assume/EnsuresOnThrow rejected at registration, `Requires<TException>` ignores `TException`); MyTool.Agent tests 3/3. A differential probe (same scenarios through both libraries) shows the conflicts below.
- **Motivation:** Remove redundancy, fix the defects found, and make the library complete enough to publish.
- **Related issues or references:** CodeContracts rewriter semantics – `Foxtrot/Driver/Rewriting/RuntimeContractMethods.cs` (typed preconditions), `Foxtrot/Foxtrot/Rewriter.cs` (exit order, invariants, pure methods), `Foxtrot/Foxtrot/Extraction/ExtractorVisitor.cs` (async postconditions), `Foxtrot/Tests/CheckerTests` (definition errors CC1000–CC1023).

### Semantic comparison and conflict resolution

Probe IDs (Sxx) refer to the differential probe run during spec writing.

| # | Topic | CE (human) | CE4 (AI) | Legacy CodeContracts | ContractCheck resolution |
|---|---|---|---|---|---|
| R1 | Failure signal | BCL internal `ContractException` via `ContractHelper`; subscribes a new `SetUnwind` handler on **every** check (leak; `SetHandled` can never suppress) (S16) | public `ContractViolationException`; `Contract.ContractFailed` never raised (S16) | raise `ContractFailed`; unhandled → throw `ContractException` (throw-on-failure mode) | Raise `Contract.ContractFailed` (`SetHandled` suppresses, unwind/handler errors honoured), then throw public `ContractViolationException`. No handler leak, never `FailFast`. |
| R2 | Failure kinds | `ContractFailureKind` (6) | own `ContractKind` (3) | `ContractFailureKind` | `ContractFailureKind`; `ContractKind` is dropped. |
| R3 | `Requires<TException>` | throws `TException` with raw message, `ParamName` null (S02, S30) | ignores `TException`, throws `ContractViolationException` (S02) | raise event, then always throw `TException`; `(paramName, message)` ctors get the user message as `ParamName` | Legacy construction rules; always `TException`; failure kind attached to `Exception.Data`. |
| R4 | Message / condition text | user message only, no condition (S01, S03) | full lambda text | condition text + user message | BCL display format (`Precondition failed: value > 0  msg`) + member name; condition printed from the expression tree. |
| R5 | `OldValue` | only `OldValue(x.Prop)`; two clauses with the same property crash (S04); argument members (S05) and composite expressions (S06) unsupported | any expression | any expression, captured after preconditions | Any expression (CE4 approach); capture failures surface only if the value is read; `OldValue` over `ForAll/Exists(int,int,…)` variables supported. |
| R6 | `Result<T>` type mismatch | silently `default(T)` (S07) | `InvalidCastException` at call (S07) | build error CC1002 | Definition-time error. |
| R7 | `Result`/`OldValue` outside `Ensures` | silently `default` (S28) | silently `default` (S28) | CC1001 / CC1023 | Definition-time error. |
| R8 | Interface invariants `Dbc.Def(x => Contract.Invariant(..))` | silently ignored (S10) | checked at creation, on entry and on exit of every member (S22) | end of constructor + exit of public, non-pure methods; not on entry | At creation (`Dbc.Make`) and after every successful non-pure member; property getters and `[Pure]` members skipped; not on entry, not on exceptional exit. |
| R9 | `Invariant` inside a member `Def` | checked after that member only (S08) | promoted to an interface invariant (S08, S09) | – | Member-scoped: checked on normal exit of that member, reported as `Invariant`. |
| R10 | `Assert` / `Assume` | entry checks, kinds Assert/Assume (S11, S12) | rejected at registration | body statements | Entry checks in definition order, kinds Assert/Assume (CE). |
| R11 | `EnsuresOnThrow<T>` | evaluated for **any** exception type (S14) | rejected at registration | only when `T` is thrown | Every clause whose `T` matches the thrown exception; failure wraps the original exception; otherwise the original is rethrown. |
| R12 | Evaluation order | pre → old → call → inv → post (S37) | inv → pre → old → call → inv → post (S37) | pre → old → call → post → inv | pre → old → call → post → inv (legacy), plus creation check. |
| R13 | Member selection | last member visited: `x.Add(x.Count)` contracts land on `get_Count` and crash (S36) | outermost call | – | Outermost call, validated to be a member of the interface. |
| R14 | Scoping / inheritance | global by `MethodInfo`: leaks across sibling interfaces (S18); base contracts only after the base was proxied once (S17b) | per interface; base-interface contracts never applied (S17) | contracts inherited from base types | Per interface **plus** contracts of all base interfaces (each loaded from its own `[ContractClass]`), deterministic. |
| R15 | Contract-class loading | once per interface; validates `[ContractClassFor]`; errors surface as `TypeInitializationException` (S25) | races run the constructor N times → N× duplicated contracts (S35); no validation | CC1019 | Exactly once per interface (thread-safe), validated, open generic contract classes closed over the interface type arguments (S20), clear `InvalidOperationException`. |
| R16 | `Def` with no clauses | no-op | error (S26) | – | No-op. |
| R17 | `Make(null)` | late `TargetException` (S19) | `ArgumentNullException` | – | `ArgumentNullException`. |
| R18 | Target exceptions | stack trace lost (S15) | stack trace lost (S15) | – | Rethrown with `ExceptionDispatchInfo`, stack preserved. |
| R19 | Overloads | 0–7 parameters | 0–7 parameters | – | 0–15 parameters, plus `DefSetter` for property/indexer setters. |
| R20 | Async members | synchronous only | synchronous only | async postconditions for `Task<T>` | `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` supported (FR-A). |
| R21 | Evaluation | `DynamicInvoke` per clause | compiled `Func<ctx,bool>` | IL | Compiled delegates (CE4). |

Behaviour both projects already share (kept): interface-only proxies, `[ContractClass]`/`[ContractClassFor]` wiring, `Contract.ForAll/Exists` usable in conditions, exceptions thrown by a condition propagate unchanged (S21), invariants not checked on exceptional exit (S23).

## 4. Goals

- Goal 1: A single `ContractCheck` library replaces CE and CE4; every `Dbc.Def` / `Dbc.Make` usage from either project compiles unchanged except for namespace and the intentional changes listed in §3.
- Goal 2: Cover every `System.Diagnostics.Contracts` member that can be expressed through expression-tree `Dbc.Def` clauses (§7, API matrix); document the rest.
- Goal 3: One test suite that covers the whole public API, built from both source suites and ported CodeContracts scenarios.
- Goal 4: A reusable Claude skill + init script that infers contracts and verifies them with FsCheck property tests in a separate test project.
- Goal 5: Public-ready README and docs.

## 5. Non-Goals

- Contracts on `ref`/`out` parameters and `Contract.ValueAtReturn` (not expressible in expression-tree selectors; user agreed to skip).
- IL rewriting, static verification, class (non-interface) proxies, static members, event accessors.
- Publishing to NuGet and CI pipelines (package metadata only; CI is a follow-up).
- `Contract.EndContractBlock`, `[ContractInvariantMethod]`, `[ContractAbbreviator]`, `[ContractArgumentValidator]`, `[ContractPublicPropertyName]`, `[ContractReferenceAssembly]`, `[ContractVerification]` (no runtime meaning for expression-tree contracts; documented with alternatives).

## 6. User Stories

- As a .NET developer, I want to declare preconditions, postconditions and invariants for an interface in one contract class so that every implementation wrapped with `Dbc.Make` is checked at runtime.
- As a developer porting CodeContracts code, I want the familiar `Contract.*` vocabulary and failure semantics (`ContractFailed`, `Requires<TException>`) so that migration is mechanical.
- As a test author, I want FsCheck properties that use contracts as test oracles so that random inputs find contract violations.
- As a Claude Code user, I want a skill that infers contracts for the code I just changed, adds property tests, and tells me plainly when code violates an existing contract or when inferred contracts contradict existing tests.

## 7. Functional Requirements

### Projects
1. `ContractCheck` (library, `net8.0;net9.0;net10.0`), `ContractCheck.FsCheck` (library, same targets, depends on FsCheck), `ContractCheck.Samples` (CodeContracts demos as sample code, `net10.0`), `ContractCheck.Tests` (xUnit v3, `net10.0`), `ContractCheck.Samples.PropertyTests` (created by the skill's init script). Old projects (`ContractExpressions*`, `MyTool.Agent*`) are deleted from disk and the solution.
2. Library source is organised in feature folders `ContractCheck/Features/{Definitions,Enforcement,Failures}`; public types live in namespace `ContractCheck`.

### Public API
3. `Dbc.Def` overloads for members with 0–15 parameters, `void` (`Expression<Action<T,…>>`) and value-returning (`Expression<Func<T,…,TResult>>`) selectors; clause lambdas take the same parameters as the selector (CE/CE4 shape).
4. Interface-invariant form: `Dbc.Def(static (T x) => Contract.Invariant(...), more invariant clauses...)`; any non-`Invariant` clause in this form is a definition error.
5. `Dbc.DefSetter` for property setters and indexer setters (0–14 index parameters); clause lambdas receive the selector parameters plus the assigned `value` last.
6. `Dbc.Make<T>(T target)`: `T` must be an interface (`ArgumentException`), `target` non-null (`ArgumentNullException`); wrapping an existing ContractCheck proxy of `T` returns it unchanged; interface invariants are checked before returning.
7. `ContractViolationException` (public, sealed): `Kind` (`ContractFailureKind`), `Method` (member name, `"<creation>"` for creation checks), `Condition`, `UserMessage`, `ContractText`, `InterfaceType`, `Member`. Message = BCL display message + ` (in Interface.Member)`.
8. `exception.GetContractFailureKind()` extension: returns the kind for `ContractViolationException` and for exceptions thrown by `Requires<TException>`, otherwise `null`.

### API matrix (`System.Diagnostics.Contracts`)

| Member | Support |
|---|---|
| `Requires(bool)`, `Requires(bool,string)` | Entry check, kind Precondition |
| `Requires<TEx>(bool)`, `Requires<TEx>(bool,string)` | Entry check; throws `TEx` (R3) |
| `Ensures(bool)`, `Ensures(bool,string)` | Exit check, kind Postcondition |
| `EnsuresOnThrow<TEx>(bool)`, `EnsuresOnThrow<TEx>(bool,string)` | Checked when `TEx` (or derived) is thrown, kind PostconditionOnException |
| `Invariant(bool)`, `Invariant(bool,string)` | Interface form (R8) or member-scoped (R9) |
| `Assert(bool)`, `Assert(bool,string)`, `Assume(bool)`, `Assume(bool,string)` | Entry checks, kinds Assert/Assume |
| `Result<T>()` | `Ensures` only; `T` = return type, or awaited type for async members |
| `OldValue<T>(T)` | `Ensures`/`EnsuresOnThrow` only; any non-literal expression |
| `ForAll`/`Exists` (4 overloads) | Anywhere in conditions, including with `Result`/`OldValue` |
| `ValueAtReturn<T>(out T)`, `EndContractBlock()` | Definition error with explanation |
| `Contract.ContractFailed`, `ContractFailedEventArgs` (`SetHandled`, `SetUnwind`) | Raised for every failure (R1) |
| `ContractFailureKind` | Used for all failures |
| `[ContractClass]`, `[ContractClassFor]` | Contract-class discovery and validation (incl. open generics) |
| `[Pure]` | Members (or interfaces) marked `[Pure]`, and property getters, skip interface-invariant checks |
| `[ContractRuntimeIgnored]` | Clauses referencing such members/types are not checked at runtime |
| `[ContractOption("runtime","checking",false)]` | On interface, member or assembly: no runtime checks |
| `[ContractOption("contract","inheritance",false)]` | On interface: base-interface contracts are not inherited |
| Other attributes, `ContractHelper.TriggerFailure` | Not applicable; documented |

### Semantics
9. Evaluation order per call: entry checks (`Requires`, `Requires<TEx>`, `Assert`, `Assume`) in definition order → old values → target → on exception: matching `EnsuresOnThrow` clauses, then rethrow → `Ensures` → member-scoped invariants → interface invariants (unless pure).
10. Contracts for a member apply from the proxied interface and all its base interfaces (base first); sibling interfaces never share contracts; `[ContractOption("contract","inheritance",false)]` turns inheritance off.
11. Definition-time errors (`InvalidOperationException`, message names the clause): selector not a member of the interface; clause body not a `Contract` call; `Result` outside `Ensures` or with the wrong type or on a `void` member; `OldValue` outside `Ensures`/`EnsuresOnThrow`, on a literal, nested in `OldValue`/`Result`, or over an unsupported quantifier variable; `ValueAtReturn`; `EndContractBlock`; non-invariant clause in the interface form; `[ContractClass]` target that is not a class or lacks a matching `[ContractClassFor]`.
12. Failure reporting: raise `Contract.ContractFailed`; if a handler calls `SetHandled()` (and no unwind) execution continues; otherwise throw `ContractViolationException` (inner = original exception for `EnsuresOnThrow`, or the BCL `ContractException` when a handler unwound or threw). `Requires<TEx>` always throws `TEx`.
13. User messages may be any string expression over the clause parameters; they are evaluated only on failure.

### Async (FR-A)
14. For members returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`: `Contract.Result<T>()` (awaited type) is allowed; `Ensures` clauses (except those that reference `Result<TTask>()`, the task itself), member-scoped invariants and interface invariants are checked when the task completes successfully; `EnsuresOnThrow` is checked for synchronous throws, faulted tasks and cancelled tasks (as `OperationCanceledException`). The proxy returns a task that faults with `ContractViolationException` on failure and otherwise mirrors the original outcome.

### FsCheck integration (`ContractCheck.FsCheck`)
15. `DbcPropertyTest.Check` (sync, CE4 signatures), `CheckAsync` (task-returning members) and `CheckSequence` (random operation sequences on one proxy): entry-check failures (incl. `Requires<TEx>`) discard the case; postcondition/invariant failures fail the property; creation-time invariant failures fail the property.

### Claude skill
16. `.claude/skills/dbc-property-tests/SKILL.md`: for code changed in a task, read existing contracts and tests, infer contracts with evidence and confidence, add only high-confidence clauses, generate FsCheck properties in the property-test project, run them, and report in fixed tables: inferred contracts, conflicts with existing contracts, code that violates existing contracts (with counterexamples – never weaken an existing contract to make tests pass), and conflicts with existing tests.
17. `.claude/skills/dbc-property-tests/scripts/init-property-tests.ps1` (PowerShell 7): creates `<Project>.PropertyTests` (xUnit v3 + FsCheck.Xunit.v3, same TFM as the project under test), references the project under test, `ContractCheck` and `ContractCheck.FsCheck` (project references when found in the repo, otherwise NuGet), adds it to the solution, builds it; idempotent.

### Docs and tooling
18. README rewritten for a public repo; ADRs for the main decisions; feature READMEs; `docs/features.md` regenerated; hook script and doc skills point to `ContractCheck/Features`; Copilot assets in `.github/` updated to ContractCheck; devcontainer on the .NET 10 SDK image; `global.json` requiring SDK ≥ 10.0.100.

## 8. Non-Functional Requirements

- Performance: clause evaluation uses compiled delegates; member contract lookup is cached per (interface, member); no reflection `DynamicInvoke` on the hot path.
- Reliability: contract registration is thread-safe and happens exactly once per interface; no global event-handler accumulation.
- Security: no dynamic code beyond `Expression.Compile`; no network access in the library.
- Accessibility: n/a.
- Compatibility: `net8.0`, `net9.0`, `net10.0`; builds with the .NET 10 SDK; library builds warning-free with nullable enabled and XML docs for the public API.

## 9. UX / UI Notes

- User flow: define `[ContractClass]` interface + `[ContractClassFor]` class with `Dbc.Def` calls → `var proxy = Dbc.Make<IFoo>(new Foo())` → use the proxy.
- Visual considerations: failure messages follow the BCL format and name the interface member.
- Edge cases: generic interfaces with open generic contract classes, generic methods, struct targets, internal interfaces.

## 10. Technical Notes

- Proposed approach: Start from CE4's architecture (per-interface registry, compiled `Func<context,bool>` predicates, expression rewriting of `Result`/`OldValue`/parameters) and add CE's clause kinds (Assert, Assume, EnsuresOnThrow, typed Requires) and BCL failure integration; add validation, inheritance, open-generic loading, async wrappers, setter contracts.
- Dependencies: BCL only for `ContractCheck`; FsCheck 3.3.x for `ContractCheck.FsCheck`; tests on xUnit v3 3.2.x + FsCheck.Xunit.v3 3.3.x (FsCheck.Xunit.v3 3.4 requires xUnit v3 4.x, released too recently).
- Risks / unknowns: DispatchProxy behaviour for generic methods and default interface methods; FsCheck 3 C# API details.
- Open questions: none blocking (decisions taken by the user on 2026-10-11: delete old projects incl. MyTool.Agent; event + public exception; net8/9/10 library with net10 tests; async in scope).

## 11. Acceptance Criteria

- [x] AC1 `ContractCheck` and `ContractCheck.FsCheck` build for `net8.0`, `net9.0` and `net10.0` with zero warnings.
- [x] AC2 `ContractExpressions`, `ContractExpressions.Tests`, `ContractExpressions4`, `ContractExpressions4.Tests`, `ContractExpressions4.Check`, `MyTool.Agent`, `MyTool.Agent.Tests` are removed from disk and from `ContractExpressions.sln`.
- [x] AC3 `dotnet build ContractExpressions.sln` and `dotnet test ContractExpressions.sln` succeed with all tests passing.
- [x] AC4 Every scenario of both source test suites exists in `ContractCheck.Tests` (or the samples property tests), adapted only where §3 changes behaviour, with the change noted in the test.
- [x] AC5 Each row of the API matrix (§7) has at least one passing and one failing-contract test (or a definition-error test for unsupported members).
- [x] AC6 Each resolution R1–R21 is covered by at least one named test.
- [x] AC7 Tests ported from the CodeContracts submodule name their origin file and test.
- [x] AC8 `DbcPropertyTest.Check`, `CheckAsync` and `CheckSequence` discard entry-check failures (including `Requires<TEx>`) and fail on postcondition and invariant violations – covered by tests.
- [x] AC9 Running `init-property-tests.ps1 -ProjectUnderTest ContractCheck.Samples/ContractCheck.Samples.csproj` creates `ContractCheck.Samples.PropertyTests`, adds it to the solution and builds it; a second run changes nothing; its property tests pass.
- [x] AC10 `.claude/skills/dbc-property-tests/SKILL.md` specifies inference, the four report tables, and the rule that existing contracts are never weakened to make tests pass.
- [x] AC11 README covers install, quick start, API matrix, semantics (order, invariants, async, failures), FsCheck usage, the skill, limitations, migration from CE/CE4 (R1–R21), build/test, license.
- [x] AC12 Devcontainer uses `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble`; `global.json` requires SDK ≥ 10.0.100.
- [x] AC13 Feature READMEs for Definitions, Enforcement, Failures; `docs/features.md` regenerated; ADRs drafted with Status Proposed; `check-done.ps1`, `document-feature` and `audit-features` point to `ContractCheck/Features`.
- [x] AC14 `.github/` Copilot instructions, agent and skill reference ContractCheck instead of CE/CE4.

## 12. Testing / Verification

- Manual test plan: run the init script on `ContractCheck.Samples` twice; read a few failure messages for clarity.
- Automated test coverage: `ContractCheck.Tests` organised by Definitions / Clauses / Enforcement / Failures / Compatibility (ported CE+CE4) / Legacy (ported CodeContracts) / Samples / PropertyTesting; `ContractCheck.Samples.PropertyTests` for FsCheck properties over the samples.
- Regression considerations: `Contract.ContractFailed` is process-global – tests that subscribe handlers run in a non-parallel collection.

## 13. Rollout / Follow-up

- Rollout plan: single PR from `claude/magical-rubin-n6vlh7`; version 0.1.0.
- Follow-up work: CI workflow; NuGet publishing; Roslyn analyzer (purity, `CONTRACTS_FULL`, clause validation at compile time); `ref`/`out` support via placeholder fields; source-generated proxies (AOT).

## 14. Changes during implementation

- **Release builds dropped contracts (found while verifying AC3 in Release).** `ContractClassAttribute` is
  `[Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]`, so Release builds removed it from interface files without
  `#define CONTRACTS_FULL` (all samples) and nothing was checked. `ContractClassLoader` now falls back to the class marked
  `[ContractClassFor]` in the interface's assembly (ambiguity is a definition error); tests in `ContractClassLoadingTests`;
  README, skill and Copilot docs explain which attributes are conditional and why `CONTRACTS_FULL` must never be defined
  project-wide (`Contract.Requires` in method bodies then calls `Environment.FailFast`).
- **`[Pure]` on interfaces is impossible** (CS0592: `PureAttribute` doesn't target interfaces). The API-matrix row
  "members (or interfaces) marked `[Pure]`" is implemented for members and property getters only.
- **`ContractCheck.FsCheck`:** `async` lambdas were ambiguous between `Task` and `ValueTask` overloads of `CheckAsync`
  (CS0121), and `[OverloadResolutionPriority]` only helps C# 13+ (net8.0 projects default to C# 12). `CheckAsync` now
  takes `Task`-returning delegates only; `Check` awaits any returned `Task`, `ValueTask` or `ValueTask<T>`. Added
  `CheckSequenceAsync` and the public `IsEntryCheckFailure()` extension beyond FR8/FR15. Verified on net8.0 and net9.0
  by running both test projects retargeted (the committed test projects target net10.0).
- **Typed preconditions:** `Requires<ArgumentException>` receives the failure message (its two-string constructor is
  `(message, paramName)`), as in the legacy test `NegativeRequiresExceptionTest3`.
- **Tests:** CodeContracts ports live in the topic folders (Clauses, Definitions, Enforcement) with `…_ported_from_<test>`
  names and the origin file in the class summary, instead of a separate `Legacy` folder (§12). The ported
  ContractExpressions4 `tmp/Divider` contract gained `Requires(x != int.MinValue || y != -1)`: its property could hit the
  overflow case. Added tests for repeated `Def` calls (`RepeatedDefinitionTests`), R21 (`CompilationTests`) and async
  lambdas in `DbcPropertyTestTests`.
- **Samples:** besides the CodeContracts demos, `ContractCheck.Samples` keeps the ContractExpressions4 demos (Addition,
  CreditDebit) and adds an async `Inventory` sample. `ContractCheck.Samples.PropertyTests` pins four contract defects of
  the samples as `Known_defect_…` facts (BankAccount withdraw boundary, CreditDebit infinite amounts, Max of an empty
  array, division by zero).
- **Init script:** also supports `.slnx`, projects targeting `netstandard`/.NET Framework (tests use the SDK's TFM),
  package mode and `-GrantInternalsAccess`; verified idempotent in project and package mode.
- **Skill trial:** a separate agent ran the skill on a scratch project with three planted defects. It found all three
  (an existing postcondition the code violates, a missing precondition, a unit test that contradicts a contract) plus
  two real bugs (int overflow, null keys). Its 24 friction points were folded into SKILL.md and the references: scope is
  fixed before the init script runs, implication is judged with C# semantics, findings must not mask each other, the
  third report table became "Contract violations found" (existing clauses, added clauses, missing preconditions), and
  failing properties wait for the user's decision before anything is committed.
- **Port audit (AC4):** a separate agent mapped all 197 tests of the two old suites to their ports; none was missing.
  Weakened assertions it found (member names, per-element checks, a merged theory) were restored, and the one changed
  outcome (Devscovery `AbsNetStyle(int.MinValue)` now throws `OverflowException`, as in the legacy demo) is explained in
  the test.
- **Repository:** `plans/Plan_Agentic_Dbc_Contract_Generation.md` (described the removed MyTool.Agent) was deleted at the
  owner's request; `dotnet/runfile-discovery/*` (tracked tool caches) was left untouched.
- **ADRs:** 0001–0005 were drafted as Proposed and accepted by the owner on 2026-10-11.
