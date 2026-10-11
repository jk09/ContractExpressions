---
name: dbc-property-tests
description: Infer Design-by-Contract contracts (ContractCheck `Dbc.Def`) for .NET code changed in a task and verify them with FsCheck property tests in a separate `<Project>.PropertyTests` project. Use after adding or changing C# interfaces/classes, or when asked to infer contracts, add property-based tests, or check that contracts hold. Reports code that violates contracts and conflicts with existing tests instead of hiding them.
argument-hint: "[project or types to cover; default: the C# code changed in this task]"
---

# DbC property tests

Existing contracts and tests are the specification you verify, not something to bend until the build is green.
A failing property is a finding to report.

Read first: [reference/contractcheck.md](reference/contractcheck.md) (the API and its runtime rules) and
[reference/fscheck-patterns.md](reference/fscheck-patterns.md) (FsCheck 3 C# patterns and generator pitfalls).

## Ground rules

1. **Never weaken, delete or reorder an existing contract clause, and never edit or delete an existing test**, to make
   anything pass. Report it (step 9) and let the user decide.
2. **Never change production behaviour** to satisfy a property unless the user asked for that fix. Report it instead.
3. Add a new clause only with **high confidence** (step 3). Medium and low confidence clauses are proposals in the report.
4. Generators may be **biased** for coverage (small key pools so calls hit existing entries, edge values) and limited
   to the **documented domain** (constructor rules, documented ranges) – say why in a comment. Never exclude an input
   because it fails, and never wrap a failing property in `Prop.When` to make it pass.
5. Property tests go in `<Project>.PropertyTests` only. Existing test projects are read, run and reported on, never changed.
6. Follow the repository's workflow (`CLAUDE.md`, spec and commit rules). This skill doesn't commit, and failing
   properties are not committed until the user has decided what to do about each finding (step 8).

## Step 0 – Scope (before anything creates files)

- Default: C# files changed in this task – `git diff --name-only <base>` plus `git ls-files --others --exclude-standard`,
  where `<base>` is the commit the task started from (`git merge-base HEAD origin/<default branch>` for a branch);
  leave out test projects. Otherwise: what the user named.
- ContractCheck checks **interfaces** (`Dbc.Make<IFoo>`). For a class without an interface:
  - class created in this task → add `IFoo` next to it (conventions in step 4);
  - existing class → don't introduce an interface without asking; list it under *Not covered*.
- Out of reach of the runtime checker (list under *Not covered*): `ref`/`out` parameters, static members, constructors
  (express constructor rules as invariants, which `Dbc.Make` checks), facts the interface can't observe (e.g. "every
  key has a price" when the interface can't list its keys), calls that bypass the proxy.

## Step 1 – Set up the property-test project

```
pwsh .claude/skills/dbc-property-tests/scripts/init-property-tests.ps1 -ProjectUnderTest <path/to/Project.csproj>
```

Creates `<Project>.PropertyTests` next to the project (xUnit v3, FsCheck.Xunit.v3, references to the project,
`ContractCheck` and `ContractCheck.FsCheck`), adds it to the solution and builds it. Idempotent: re-running only adds
what is missing; each item is reported as `created`, `added`, `exists`, `built`, `note` or `skipped`. Switches:
`-Solution`, `-ContractCheckSource project|package` (package mode needs the ContractCheck packages on a feed you can
restore from), `-GrantInternalsAccess` (adds `InternalsVisibleTo` for internal types), `-SkipBuild`. Without
PowerShell 7, create the same project by hand; the script's csproj and package versions are the template.

## Step 2 – Read what already exists

- Contracts: `[ContractClass]` on the interface → its `[ContractClassFor]` class (usually `Foo.Contracts.cs`) with
  `Dbc.Def` / `Dbc.DefSetter` calls; base-interface contracts apply too.
- Tests: every test that touches the type (`grep` the type and member names in all test projects), including existing
  property tests. Note what each asserts – values, exceptions, state changes – and whether it goes through `Dbc.Make`.
- Callers: which inputs they pass and which results they rely on.

## Step 3 – Infer contracts, with evidence and confidence

| Evidence | Clause |
|---|---|
| Guard: `ArgumentNullException.ThrowIfNull(x)`, `if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x))` | `Requires<TException>(cond, nameof(x))` – keeps the exception type and `ParamName` callers and tests expect |
| Guard throwing another exception (`InvalidOperationException` on a state check) | `Requires<TException>(cond, "message")` |
| Library code that throws on inputs no guard rejects (`Dictionary` indexer → `KeyNotFoundException`, null keys → `ArgumentNullException`, `/ 0`) | missing precondition: propose `Requires(…)` (medium); the property will show it |
| `<exception cref="T">` doc for a state the member must keep when it fails | `EnsuresOnThrow<T>(… == Contract.OldValue(…))` |
| `<returns>` doc, non-nullable return, `Count`/`Length` facts | `Ensures(Contract.Result<T>() …)` |
| Mutators | the exact change the code makes: `x.Count == Contract.OldValue(x.Count) + 1` for a list `Add`; for keyed collections `+ (Contract.OldValue(x.Contains(k)) ? 0 : 1)` |
| Read-only members | `Ensures(x.Count == Contract.OldValue(x.Count))` for the state they must not change |
| Facts true after every constructor and every mutator | interface invariant `Dbc.Def(static (IFoo x) => Contract.Invariant(…))` |
| Unit-test assertions that hold for all inputs | candidate `Ensures` (generalise only what the code guarantees) |
| Nullable annotations only (`string` vs `string?`), names only | medium or low – propose, don't add |

Confidence: **high** = the code enforces or guarantees it (also when only arithmetic overflow or rounding could break
it – that is exactly what the property should probe), or the docs state it and the code agrees; **medium** =
docs/tests/names suggest it but the code doesn't enforce it; **low** = plausible only. Record `file:line` evidence.

Compare every inferred clause with the existing clauses of the member and the interface invariants. Judge implication
with C# semantics (wrapping `int` arithmetic, floating point, `NaN`), not school maths:

| Class | Meaning | Action |
|---|---|---|
| New | nothing existing covers it | add if high confidence |
| Duplicate / Weaker | an existing clause says the same, or implies it | skip |
| Stronger | it implies an existing clause | postcondition/invariant: add **beside** the existing clause if high confidence; precondition: never add (breaks callers) – propose |
| Contradicts | on some input the preconditions admit, both can't hold | add nothing; report under *Conflicts with existing contracts* |

## Step 4 – Write the contracts

Conventions (as in `ContractCheck.Samples` in the ContractCheck repository):

- `IFoo.cs`: the interface with `[ContractClass(typeof(FooContracts))]`. `Foo.cs`: the implementation.
- `Foo.Contracts.cs`: `[ContractClassFor(typeof(IFoo))] internal sealed class FooContracts` whose constructor calls
  `Dbc.Def` once per member, with `static` lambdas whose parameter names match the member's.
- `#define CONTRACTS_FULL` as the first line of every file that calls `Contract.*` (without it the clause lambdas fail
  with CS0765) and of the interface's file (`[ContractClass]`, `[Pure]` are conditional too). Never project-wide.
- Append to an existing contract class; never rewrite existing `Dbc.Def` calls. A new clause for a member that already
  has one goes into an additional `Dbc.Def` call for that member (both apply).
- Setters and indexer setters: `Dbc.DefSetter`. Interface invariants: `Dbc.Def(static (IFoo x) => Contract.Invariant(…), …)`.
- Clauses run against the implementation, not the proxy: members they call are not checked and don't recurse.
- Building doesn't validate clauses – `Dbc.Make` does. Run the first property right away (step 6) and fix definition
  errors (`InvalidOperationException` naming the clause) in the clauses you wrote.

## Step 5 – Write the properties

In `<Project>.PropertyTests`: `FooProperties.cs` for `IFoo`; generators plus setup and step records in `Generators.cs`.

- **Single calls.** `DbcPropertyTest.Check(() => Dbc.Make<IFoo>(BuildFoo(setup)), foo => foo.Bar(x, y))` with the
  arguments as `[Property]` parameters. Generate the object's state too (a setup record such as initial contents):
  a fresh object makes most properties trivial. Arguments that must fit each other or the state (prices covering the
  cart's items) come from one generated record with its own shrinker. `CheckAsync` for `Task` members and `async`
  lambdas; `Check` awaits returned tasks and `ValueTask`s too.
- **Sequences.** One per stateful interface: `CheckSequence` / `CheckSequenceAsync` over an array of step records
  (`record FooStep(…) { void Apply(IFoo foo); override ToString() }`), so invariants are checked across state changes
  and counterexamples read like a call log. A step rejected by an entry check is skipped; the sequence goes on.
- **Outcomes.** In `Check`/`CheckAsync` an entry-check failure discards the case; a postcondition, invariant or any
  other exception fails it. Documented exceptions (e.g. `InvalidOperationException` "stock is short") are caught inside
  the step, after the proxy has checked `EnsuresOnThrow` – catch only the documented type, and never a type that typed
  preconditions throw (`ArgumentException` and subclasses), or precondition rejections disappear into the catch.
- **One finding must not hide another.** Split properties by input class (`Total_with_priced_items`,
  `Total_with_unpriced_items`) and keep single-call properties next to the sequence, so a failure in one class
  doesn't mask others.
- **Generators.** FsCheck's defaults never produce `null` strings, `0` or whole `decimal`s/`double`s, or `int`s beyond
  ±size (no `int.MinValue`); floats are ±∞/NaN only ~2% each. Add edge values on purpose (`IntWithMinMax`,
  `Gen.Frequency`, `Gen.OrNull`) – see the reference. Keep most cases inside the preconditions, or the property
  exhausts its discards (*Arguments exhausted*). With nullable enabled, pass generated nulls as `value!`.
- Names: `<Member>_keeps_the_contracts`, `…_at_the_extremes`, `<Actions>_keep_the_contracts` for sequences.

## Step 6 – Run and triage

```
dotnet test <Project>.PropertyTests
```

Run it three times (fresh seeds). For every failure take the counterexample – `Shrunk:`, or `Original:` when FsCheck
reports 0 shrinks – and classify:

| Failure | Classification | Action |
|---|---|---|
| An existing clause fails | **existing contract violated** | report with the counterexample; don't touch the contract or the code |
| A clause you added fails | inference wrong, or a bug | strong evidence → report as a bug and keep the clause; else remove it and report it as rejected |
| Non-contract exception on an input the preconditions accept (`DivideByZeroException`, `KeyNotFoundException`) | **missing precondition** | report; propose the `Requires` |
| Invariant fails at creation | generator builds invalid objects | fix the generator to the documented constructor domain |
| *Arguments exhausted* | generator too wide | make the generator hit the precondition domain |
| Fails only sometimes | rare input | reproduce with `[Property(Replay = "(…,…)")]` using the pair printed after *Falsifiable, after N tests*, then classify |
| Same finding every run | it may hide others | look further in a throwaway experiment (e.g. exclude that input locally, never committed) and report what else you find |

## Step 7 – Check the existing tests

```
dotnet test <solution>
```

- a failing existing test → **conflict** between the test and a clause;
- a passing test that asserts behaviour a clause – new **or existing** – forbids (it calls the implementation
  directly, with an input a precondition rejects, or expects a result a postcondition excludes) → **latent conflict**.

Don't edit the tests. Offer: keep the clause and update the test, change the clause, or fix the code.

## Step 8 – Decide with the user

Failing properties stay failing until the user decides per finding: fix the code, change the contract (their call,
never yours), or pin the defect. A pinned defect becomes a `Known_defect_…` fact that asserts the property is
falsified (`PropertyAssert.Falsified`, in the FsCheck reference), plus a commented exclusion of exactly that input from
the general property. Repositories that require green tests get no failing property committed without that decision.

## Step 9 – Report

End with these four tables (write "none" in an empty one), then the other items below.

**Inferred contracts**

| Member | Clause | Kind | Status: added / proposed (medium, low, stronger precondition, missing precondition) / skipped (duplicate, weaker, contradicts) / rejected | Confidence | Evidence |

**Conflicts with existing contracts**

| Member | Existing clause | Inferred clause | Why they conflict | Suggested resolution |

**Contract violations found**

| Member | Clause (existing / added in this task / missing precondition) | Counterexample | Observed | Suggested fix |

**Conflicts with existing tests**

| Test | Clause | Test expects | Kind (failing / latent) | Options |

Then: the property tests added (file, property, result of each run – `dotnet test` doesn't show FsCheck's counts for
passing properties), *Not covered* members and why, pinned known defects, and the commands to re-run.
