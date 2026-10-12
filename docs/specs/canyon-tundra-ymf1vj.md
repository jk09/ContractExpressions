# ContractCheck – analyzer, checking levels, DI, CI, ref/out contracts, generated call sequences

## 1. Metadata

| Field | Value |
| --- | --- |
| **Feature name** | ContractCheck improvements: analyzer, checking levels, precondition-inheritance warning, DI + CI, ref/out contracts, generated call sequences |
| **Spec ID** | canyon-tundra-ymf1vj |
| **Status** | Active |
| **Author** | Claude (Claude Code session for jk09) |
| **Owner** | jk09 |
| **Reviewers** | jk09 |
| **Created on** | 2026-10-11 18:22 +00:00 |
| **Last updated** | 2026-10-12 02:15 +00:00 |
| **Affected features** | `ContractCheck/Features/Definitions` (ref/out placeholders, `Dbc.HasContracts`), `ContractCheck/Features/Enforcement` (checking levels, non-generic `Dbc.Make`, by-ref values); new projects `ContractCheck.Analyzers`, `ContractCheck.DependencyInjection`; `ContractCheck.FsCheck` (generated call sequences); `.github/workflows` (new) |
| **Target release** | ContractCheck 0.1.0 (first release). FR 4–7, 11 and 15–17 merged with jk09/ContractExpressions#5; the rest follows in a second pull request from the same branch |
| **Related links** | amber-harbor-v9p6ri (fusion spec); quartz-willow-x7kgtw (source-generated proxies, later) |

## 2. Summary

- **Problem statement:** Contract mistakes surface only at run time (first `Dbc.Make`), the `CONTRACTS_FULL` pitfalls are silent, every check always runs (no cheap production mode), derived interfaces can silently strengthen inherited preconditions, wiring proxies into DI is manual, nothing builds or publishes the packages, members with `ref`/`out` parameters can't have contracts, and property tests need hand-written step generators.
- **Desired outcome:** A Roslyn analyzer reports invalid definitions and the `CONTRACTS_FULL` and precondition-inheritance pitfalls at compile time; a checking level (CodeContracts' levels) chosen in code or runtimeconfig; `ContractCheck.DependencyInjection` decorates existing registrations; GitHub Actions builds and tests Debug/Release on .NET 8/9/10 and publishes tags to nuget.org; `ref`/`out` parameters get contracts through placeholder fields; `DbcPropertyTest.CheckCalls<T>` generates call sequences from an interface.

## 3. Background and Context

- **Current behavior:** Definition errors are `InvalidOperationException`s at the first `Dbc.Make`; `#define CONTRACTS_FULL` mistakes either fail to compile (CS0765), silently drop contract attributes, or – when defined project-wide – make `Contract.Requires` in method bodies terminate the process. All clauses run on every call. `Dbc.Def` selectors can't name `ref`/`out` parameters and `Contract.ValueAtReturn` is a definition error. `DbcPropertyTest.CheckSequence` needs explicit step records. No CI exists.
- **Motivation:** The improvements suggested at the end of amber-harbor-v9p6ri, approved by the owner on 2026-10-11 (all but source-generated proxies, which get their own spec).
- **Approved by the owner on 2026-10-11** (as written; both this spec and amber-harbor-v9p6ri stay Active until PR #5 merges).
- **Decisions taken with the owner (2026-10-11):** checking level = global (code + runtimeconfig/AppContext), CodeContracts' level names; precondition inheritance = analyzer warning only, runtime unchanged; DI = decorate existing registrations; publishing = tag `vX.Y.Z` → nuget.org with a `NUGET_API_KEY` secret.
- **References:** CodeContracts runtime checking levels (Full, Pre and Post, Preconditions, ReleaseRequires, None); CodeContracts error CC1033 (overrides can't add `Requires`); `System.Diagnostics.Contracts` conditional attributes; FsCheck 3 `IArbMap.ArbFor(Type)`.

## 4. Goals

- Catch contract-definition mistakes while typing, not at run time.
- Let production keep only cheap checks without code changes.
- Make the Liskov rule for preconditions visible.
- Wire ContractCheck into Microsoft.Extensions.DependencyInjection with one line.
- Build, test and publish automatically.
- Cover members with `ref`/`out` parameters.
- Property-test any contract interface without writing step generators.

## 5. Non-Goals

- Source-generated proxies / Native AOT (quartz-willow-x7kgtw).
- Purity analysis of clauses (CodeContracts CC1036) – not requested.
- Per-interface or per-assembly checking levels (only `[ContractOption("runtime","checking",false)]` per interface/member, as today).
- Rejecting strengthened preconditions at run time.
- DI containers other than Microsoft.Extensions.DependencyInjection; decorating open-generic registrations.
- Signing packages; publishing anywhere but nuget.org.

## 6. User Stories

- As a developer writing a contract class, I want invalid selectors and clauses underlined in the editor so that I don't find them at run time.
- As a developer, I want a warning when a file's contract attributes are compiled away, or when `Contract.Requires` would terminate the process.
- As an operator, I want to run with preconditions only in production by changing runtimeconfig, without a rebuild.
- As an interface designer, I want a warning when a derived interface adds preconditions to an inherited member.
- As an application developer, I want `services.AddContractChecks<IFoo>()` to wrap my existing registrations.
- As a maintainer, I want every push and PR built and tested on .NET 8, 9 and 10, and tags published to nuget.org.
- As a developer, I want contracts on `TryGetValue(key, out value)` and `Bump(ref counter)` members.
- As a test author, I want `DbcPropertyTest.CheckCalls(() => Dbc.Make<IFoo>(new Foo()))` to throw random call sequences at an interface.

## 7. Functional Requirements

### Analyzer (`ContractCheck.Analyzers`, shipped inside the `ContractCheck` package)
1. A Roslyn analyzer (netstandard2.0, Microsoft.CodeAnalysis.CSharp 4.8, so .NET 8 SDKs load it) is packed into `ContractCheck.nupkg` under `analyzers/dotnet/cs`; projects in this repo use it through a project reference, except `ContractCheck.Tests`, which deliberately defines invalid contracts to test run-time validation.
2. Diagnostics (IDs `CCK0001`–`CCK0008`, category `ContractCheck`, help links to the README):

   | ID | Severity | Reports |
   |---|---|---|
   | CCK0001 | Error | `Dbc.Def`/`Dbc.DefSetter` selector that `Dbc.Make` would reject: type argument not an interface, not a single member access/call on the first parameter, arguments that aren't the lambda's parameters in order (by-ref parameters: placeholders, FR 15), static/extension/`object` members, `DefSetter` on a member without a setter |
   | CCK0002 | Error | Clause that isn't a single `Contract.Requires`/`Requires<T>`/`Ensures`/`EnsuresOnThrow<T>`/`Invariant`/`Assert`/`Assume` call; a non-`Invariant` clause in the interface-invariant form |
   | CCK0003 | Error | `Contract.Result<T>()` outside `Ensures`, on a `void` member, or with `T` other than the return type (awaited type for `Task<T>`/`ValueTask<T>`) |
   | CCK0004 | Error | `Contract.OldValue` outside `Ensures`/`EnsuresOnThrow`, or on a literal |
   | CCK0005 | Error | `Contract.ValueAtReturn` not on an out/ref placeholder or outside postconditions; `Contract.EndContractBlock` in a clause |
   | CCK0006 | Warning | `[Pure]`, `[ContractOption]`, `[ContractRuntimeIgnored]`, `[ContractInvariantMethod]`, `[ContractClass]`, `[ContractClassFor]` in a file where `CONTRACTS_FULL` isn't defined (the compiler drops them; `[ContractClass]` in Release builds); code fix: add `#define CONTRACTS_FULL` |
   | CCK0007 | Error | `Contract.Requires`/`Requires<T>`/`Ensures`/`EnsuresOnThrow`/`Invariant` called outside an expression-tree lambda where `CONTRACTS_FULL` is defined (terminates the process at run time) |
   | CCK0008 | Warning | Entry check (`Requires`, `Requires<T>`, `Assert`, `Assume`) defined through a derived interface for a member declared in a base interface (precondition inheritance, improvement 4) |
3. The run-time validation stays authoritative and unchanged; the analyzer never reports a definition that `Dbc.Make` accepts (no false positives on the samples and this repo's valid tests).

### Checking levels
4. `public enum ContractCheckingLevel { None, ReleaseRequires, Preconditions, PreAndPost, Full }`:
   - `Full` (default): everything;
   - `PreAndPost`: `Requires`, `Requires<T>`, `Ensures`, `EnsuresOnThrow` – no invariants, no `Assert`/`Assume`;
   - `Preconditions`: `Requires`, `Requires<T>`;
   - `ReleaseRequires`: `Requires<T>` only;
   - `None`: no checks; calls are forwarded.
5. `Dbc.CheckingLevel { get; set; }` (thread-safe; a change applies to the next call). Its initial value comes from the AppContext switch / runtimeconfig property `ContractCheck.CheckingLevel` (level name, case-insensitive); an unknown name makes the first `Dbc.Make` or `Dbc.CheckingLevel` read throw `InvalidOperationException` naming the value.
6. Skipped kinds cost nothing: no old-value capture when postconditions are off, no creation check unless invariants are on, no task wrapping when nothing deferred remains.
7. Definition-time validation runs regardless of the level.

### Precondition inheritance (improvement 4)
8. CCK0008 (FR 2) is the only change; behaviour at run time is unchanged (spec amber-harbor-v9p6ri R14).

### Dependency injection (`ContractCheck.DependencyInjection`) and API additions
9. `services.AddContractChecks<TService>()` replaces every registration of `TService` (keyed and unkeyed, any lifetime) with one that resolves the original implementation and returns `Dbc.Make<TService>(implementation)`. The original stays registered under a private key, so the container still creates and disposes the implementation. Calling it again doesn't double-wrap; no registration of `TService` → `InvalidOperationException`; `TService` not an interface → `ArgumentException`.
10. `services.AddContractChecks()` does the same for every registration whose service type is a closed interface that has contracts; open-generic registrations are skipped.
11. New public API in `ContractCheck`: `Dbc.Make(Type interfaceType, object target)` (non-generic, same rules as `Make<T>`) and `Dbc.HasContracts(Type interfaceType)` (true if the interface or a base interface has a contract class or registered clauses).

### CI and publishing
12. `.github/workflows/ci.yml` on pushes to `main` and on pull requests: Debug and Release; restore, build, test on .NET 8, 9 and 10 (test projects multi-target when `TestAllFrameworks=true`), pack, upload the packages as artifacts; on pull requests also run `.claude/scripts/check-done.ps1 -Base origin/<base>`.
13. `.github/workflows/release.yml` on tags `v*`: build, test, pack with `Version` = the tag without `v`, push `.nupkg` and `.snupkg` to nuget.org with `secrets.NUGET_API_KEY` (`--skip-duplicate`). Packages: `ContractCheck` (with analyzer), `ContractCheck.FsCheck`, `ContractCheck.DependencyInjection`.
14. Shared package metadata, SourceLink and deterministic CI builds come from a root `Directory.Build.props`.

### ref/out contracts
15. Placeholders `Dbc.Out<T>.Value`/`Value2`/`Value3` and `Dbc.Ref<T>.Value`/`Value2`/`Value3` stand for by-ref parameters in selectors and clauses: `Dbc.Def(static (IDict d, string key) => d.TryGetValue(key, out Dbc.Out<int>.Value), static (IDict d, string key) => Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<int>.Value >= 0))`. `in` parameters take the lambda's own parameters, as before.
16. Semantics: a `Ref` placeholder in an entry check is the value on entry, in a postcondition the value at return, inside `Contract.OldValue` the value on entry; an `Out` placeholder is allowed in `Ensures` only (value at return); `Contract.ValueAtReturn(out <placeholder>)` is accepted as a synonym.
17. Definition errors: a by-ref parameter without its placeholder, a placeholder of the wrong kind or type, the same placeholder twice in a selector, a placeholder the selector doesn't use, an `Out` placeholder outside `Ensures`.

### Generated call sequences (`ContractCheck.FsCheck`)
18. `DbcPropertyTest.CheckCalls<T>(Func<T> proxyFactory, CallOptions? options = null)`: a property that generates sequences of calls to the members of `T` (methods incl. inherited ones, property getters and setters, indexers) with arguments from `options.ArbMap` (default `ArbMap.Default`), runs them on one proxy per case, and awaits returned tasks. Entry-check rejections skip the call; exceptions listed in `options.ExpectedExceptions` count as normal outcomes (after the proxy checked `EnsuresOnThrow`); anything else fails the case. Members with by-ref, pointer or open generic parameters, and types FsCheck can't generate, are left out; `options.Exclude` removes members by name.
19. Counterexamples print as a call log (`[Push("a"), Pop(), Pop()]`); FsCheck shrinks both the sequence and each argument.

### Docs
20. README, package READMEs, the dbc-property-tests skill references and the feature READMEs document all of the above; ADRs for the analyzer, checking levels, DI decoration, CI/publishing and ref/out placeholders.

## 8. Non-Functional Requirements

- Performance: the level check is one volatile read per call; with `None`, a call costs only the proxy dispatch. Generated call sequences reuse cached member metadata.
- Reliability: analyzer is incremental-friendly (no compilation-wide state), never throws; its tests compile snippets with Roslyn directly.
- Security: the release workflow uses the `NUGET_API_KEY` secret only in the push step; no other secrets.
- Compatibility: libraries keep `net8.0;net9.0;net10.0`; the analyzer loads in .NET 8 SDKs (Roslyn 4.8); DI package targets Microsoft.Extensions.DependencyInjection.Abstractions 8.0 (keyed services).

## 9. UX / UI Notes

- Analyzer messages name the offending expression and the fix, matching the run-time error texts.
- Configuration: `<ItemGroup><RuntimeHostConfigurationOption Include="ContractCheck.CheckingLevel" Value="Preconditions" /></ItemGroup>` in the app's csproj, or `"configProperties": { "ContractCheck.CheckingLevel": "Preconditions" }` in runtimeconfig.template.json.
- Edge cases: keyed `AnyKey` registrations; interfaces that only inherit contracts; by-ref members in generated call sequences (skipped).

## 10. Technical Notes

- Proposed approach: analyzer on `InvocationExpression` of `ContractCheck.Dbc.Def/DefSetter` (semantic model) plus syntax-tree checks for attributes and `Contract.*` calls; checking-level filter in `MemberContracts`/`ContractInvoker`; DI via `ServiceDescriptor` rewriting with keyed inner registrations; placeholders mapped to argument slots in `MemberSelector`/`ClauseCompiler`, by-ref values read from the argument array after the call; call sequences via reflection over interface members and `IArbMap.ArbFor(Type)`.
- Dependencies: Microsoft.CodeAnalysis.CSharp 4.8.0 (+ Workspaces for the code fix, Microsoft.CodeAnalysis.Analyzers as a private asset); Microsoft.Extensions.DependencyInjection.Abstractions 8.0.x (package) and Microsoft.Extensions.DependencyInjection (tests); GitHub Actions `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`.
- Risks / unknowns: analyzer packaging inside a multi-targeted package; FsCheck `ArbFor(Type)` coverage of parameter types; CI runtime of three frameworks × two configurations.
- Open questions: none (owner decisions in §3).

## 11. Acceptance Criteria

- [x] AC1 `ContractCheck.nupkg` contains `analyzers/dotnet/cs/ContractCheck.Analyzers.dll`; the samples build with the analyzer and get no diagnostics.
- [x] AC2 Each diagnostic CCK0001–CCK0008 has tests that report it on a bad snippet and stay silent on the corresponding valid snippet; the CCK0006 code fix inserts `#define CONTRACTS_FULL`.
- [x] AC3 Tests show each checking level runs exactly its clause kinds (incl. creation invariants, old values, async deferral), the runtimeconfig switch sets the initial level, and an unknown level name fails clearly.
- [x] AC4 CCK0008 fires for a `Requires` defined through a derived interface on an inherited member and not for the base interface's own contract or the derived interface's own members.
- [x] AC5 DI tests: transient/scoped/singleton, type/factory/instance and keyed registrations are wrapped; implementations are still disposed by the container; double registration doesn't double-wrap; errors for missing registrations and non-interfaces; `AddContractChecks()` wraps only interfaces with contracts.
- [x] AC6 `Dbc.Make(Type, object)` and `Dbc.HasContracts(Type)` are covered by tests.
- [x] AC7 `ci.yml` and `release.yml` exist; CI on PR #5 is green (Debug and Release, net8.0/net9.0/net10.0 tests).
- [x] AC8 ref/out: tests for out postconditions, ref entry/exit/old values, `ValueAtReturn`, two same-typed out parameters, `in` parameters, and every definition error in FR 17.
- [x] AC9 `CheckCalls` tests: finds an invariant broken by a call sequence, skips rejected calls, honours `ExpectedExceptions` and `Exclude`, awaits async members, leaves out unsupported members, prints a readable call log.
- [x] AC10 `dotnet build` with zero warnings and `dotnet test` green in Debug and Release.
- [x] AC11 README, package READMEs, skill references, feature READMEs and ADRs 0006+ updated; `docs/features.md` regenerated.

## 12. Testing / Verification

- Manual test plan: open a sample contract file with a broken selector and see CCK0001; run a sample app with `Preconditions` via runtimeconfig; resolve a decorated service from a container.
- Automated test coverage: `ContractCheck.Tests` gains `Analyzers/` (compiles snippets with Roslyn), `Enforcement/CheckingLevelTests`, `Clauses/ByRefParameterTests`, `DependencyInjection/`, `PropertyTesting/CheckCallsTests`.
- Regression considerations: all 413 + 32 existing tests keep passing; the default level is `Full`, so behaviour without configuration is unchanged.

## 13. Rollout / Follow-up

- Rollout plan: commits on `claude/magical-rubin-n6vlh7` (PR #5); after merge, tag `v0.1.0` once `NUGET_API_KEY` is set.
- Follow-up work: source-generated proxies (quartz-willow-x7kgtw); purity analysis; per-interface levels if needed.

## 14. Changes during implementation

- **Delivery:** the owner merged PR #5 on 2026-10-11 at d2d2dc0 (checking levels, `Dbc.Make(Type, object)` /
  `Dbc.HasContracts`, ref/out placeholders). `CheckCalls`, the DI package, the analyzer and CI were pushed after the
  merge and go into a second pull request; main was merged into the branch (a force-push of a rebase wasn't
  permitted). AC7 applies to that pull request, jk09/ContractExpressions#6, whose first CI run was green (Debug and
  Release, net8.0/net9.0/net10.0, docs check). This spec stays Active until it merges; amber-harbor-v9p6ri is Done.
- **Analyzer scope (FR 2):** CCK0006 leaves out `[ContractInvariantMethod]` – ContractCheck ignores it, so defining
  `CONTRACTS_FULL` changes nothing. CCK0007 also reports `Contract.Requires<TException>` outside clauses in files
  without `CONTRACTS_FULL`: it isn't `[Conditional]` and terminates the process in any build. Beyond the table, the
  analyzer reports `Requires<TException>` with an exception the run time can't create (CCK0002), a clause reading both
  a task and its awaited result (CCK0003), `OldValue` of lambda variables other than a `ForAll`/`Exists` index
  (CCK0004) and every placeholder misuse of FR 17 (CCK0005) – all with the run-time texts.
- **Analyzer vs. run time (FR 3):** `RuntimeAgreementTests` runs the analyzers on ContractCheck.Tests' own sources and
  requires errors exactly where the suite expects `Dbc.Def` to throw (warnings: CCK0008 only). The analyzer runs in
  the projects that define contracts (ContractCheck.Samples, ContractCheck.Samples.PropertyTests); the libraries define
  none. The sample interface files now `#define CONTRACTS_FULL`, so `[ContractClass]` survives Release builds.
- **Packaging:** ContractCheck.FsCheck and ContractCheck.DependencyInjection reference ContractCheck with
  `PrivateAssets="none"`, so the analyzer also reaches projects that only install them.
- **DI (FR 9):** the original registration moves to a private key only for non-disposable interfaces. For disposable
  interfaces (the proxy forwards `Dispose`) and `AnyKey` registrations the decorating factory creates the
  implementation itself, and instance registrations are wrapped at once – each implementation is disposed exactly once.
- **Checking levels (FR 5):** setting an undefined `ContractCheckingLevel` value throws `ArgumentOutOfRangeException`.
- **CheckCalls (FR 18–19):** also public: `DbcPropertyTest.CallableMembers<T>(options)` (the members it calls) and
  `GeneratedCall` (the counterexample values).
- **CI (FR 12–14):** `Directory.Build.props` defines `LibraryTargetFrameworks` and `TestTargetFrameworks`
  (`TestAllFrameworks=true` → net8.0;net9.0;net10.0). The session hooks (`active-spec.ps1`, `check-done.ps1`) now handle
  several active specs; the CI docs job runs `check-done.ps1 -Base origin/<base>`.
- **Docs:** the FsCheck reference no longer claims floats are never exactly 0 (`CheckCalls` produced 0 and -0).
