## Plan: Agentic Dbc Contract Generation Workflow

Define a reusable generation workflow that lets coding agents produce .NET domain code and Design-by-Contract artifacts in one pass, using ContractExpressions4 patterns from tmp examples: contracts are defined through Dbc.Def in a dedicated *.Contracts.cs file, wired via ContractClass/ContractClassFor attributes, and validated by xUnit + FsCheck tests. The workflow defaults to side-by-side placement with generated domain code, supports optional redirection to another project, uses conservative inference by default, and allows explicit contract overrides from agent input.

**Steps**
1. Phase 1: Input Contract Intent Model
2. Define the structured agent input schema the generator consumes for each type: target namespace, output folder/project override, interface name, class name, method signatures, and optional explicit contracts from user prompt. Include an "inference mode" flag defaulting to conservative. This step is foundational for all later steps.
3. Specify precedence rules: explicit contracts in prompt always win over inferred contracts; inferred contracts are only added when confidence is high; uncertain candidates are omitted from emitted runtime contracts and surfaced as generation diagnostics. Depends on step 1.
4. Phase 2: Deterministic File Layout and Naming
5. Define canonical file split per generated type, defaulting to side-by-side placement in the same folder as the generated class: Type.cs (interface + implementation), Type.Contracts.cs (contract class), and Type.Tests.cs (test class). Add a configurable output root to place these files into a separate project while preserving the same trio layout. Depends on step 1.
6. Enforce naming and attribute conventions from existing examples: interface IType annotated with ContractClass(typeof(TypeContracts)); contract class TypeContracts annotated with ContractClassFor(typeof(IType)); contract class constructor contains Dbc.Def registrations. Depends on step 5.
7. Phase 3: Conservative Contract Inference Rules
8. Implement conservative inference heuristics for preconditions and postconditions from signatures and method semantics hints (for example nullability, argument ranges, result monotonicity), but only materialize contracts with strong evidence. Depends on step 2.
9. Add invariant inference policy: prefer global invariant definitions via single-argument Dbc.Def when object-level state constraints are clear and stable across methods; do not infer weak invariants. Fallback is to emit no inferred invariant and keep diagnostics. Depends on step 8.
10. Normalize all generated contract clauses to supported ContractExpressions4 idioms: Contract.Requires, Contract.Ensures, Contract.Invariant, Contract.Result, and static lambdas for Dbc.Def. Depends on steps 8-9.
11. Phase 4: Code Emission Templates
12. Define reusable text templates (workflow-level spec, not implementation code) for each output file to guarantee required structure and ordering: using directives, namespace, attributes, constructor-based Dbc.Def calls, and test fixture setup. Depends on steps 5-10.
13. Require contract file separation rule: for Type.cs, contracts must always be emitted in Type.Contracts.cs and never inline in Type.cs. Depends on step 12.
14. Phase 5: Test Generation Policy (xUnit + FsCheck)
15. Generate deterministic xUnit tests for success and violation paths for every public method: valid inputs satisfy contracts, invalid inputs trigger ContractViolationException with expected ContractKind and member name. Depends on steps 10 and 12.
16. Generate at least one FsCheck property per public method using DbcPropertyTest.Check, with generators constrained to avoid accidental arithmetic overflow or invalid domains unless intentionally testing violations. Depends on step 15.
17. Include invocation policy in tests: always exercise behavior via Dbc.Make<TInterface>(implementation) proxy so contract runtime is active. Depends on step 15.
18. Phase 6: Validation and Agent Feedback Loop
19. Define compile/test validation sequence for generated artifacts: dotnet build for target solution or project, then targeted dotnet test filters by generated test class names. Depends on steps 12-17.
20. Define failure triage rules for agent reruns: if tests fail due to over-strong inferred contracts, relax/remap only inferred clauses; never weaken explicit user-provided clauses without an explicit user confirmation. Depends on steps 2, 3, and 19.
21. Define generation report output per run: files created/updated, contracts explicit vs inferred, omitted low-confidence candidates, and test results summary. Depends on all prior steps.

**Relevant files**
- /workspaces/ContractExpressions/ContractExpressions4/Dbc.cs — Public Dbc.Def and Dbc.Make API surface that generated contracts/tests must target.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/Multiplier.cs — Reference for interface + implementation pairing used by generated domain file.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/Multiplier.Contracts.cs — Canonical contract class wiring and constructor-based Dbc.Def calls.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/Multiplier.Tests.cs — Canonical xUnit + FsCheck (DbcPropertyTest.Check) style.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/TestA.cs — Additional tmp reference for naming/file split consistency.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/TestA.Contracts.cs — Additional contract-file reference.
- /workspaces/ContractExpressions/ContractExpressions4.Tests/tmp/TestA.Tests.cs — Additional test-generation reference.
- /workspaces/ContractExpressions/ContractExpressions4/Internal/ContractClassLoader.cs — Runtime loading path that depends on proper ContractClass/ContractClassFor linkage.
- /workspaces/ContractExpressions/ContractExpressions4/Internal/ContractDefinitionCompiler.cs — Contract expression compilation semantics that generated Dbc.Def clauses must satisfy.

**Verification**
1. Schema verification: run one dry generation request including explicit and inferred clauses; confirm precedence rules are reflected in generation report.
2. Layout verification: confirm each generated type produces exactly three files with required naming and side-by-side default placement unless output override is supplied.
3. Compile verification: run dotnet build on solution or affected projects and ensure no compile warnings/errors from generated attributes or Dbc.Def signatures.
4. Unit-path verification: run dotnet test filtered to generated xUnit tests and verify pass/fail expectations for valid and invalid inputs.
5. Property verification: run dotnet test filtered to generated FsCheck properties and verify each public method has at least one property-based test.
6. Runtime contract verification: confirm tests execute through Dbc.Make proxies and that reported violations map to expected ContractKind categories.

**Decisions**
- Included: reusable workflow/spec, not immediate implementation templates.
- Included: default placement side-by-side with agent-generated domain classes, with configurable relocation to separate project.
- Included: conservative inference as default, with explicit user-supplied contracts taking precedence.
- Included: prefer global invariant Dbc.Def definitions when confidence is high.
- Included: FsCheck coverage for every public method.
- Excluded: aggressive inference and auto-emission of low-confidence contracts.
- Excluded: refactoring existing demo/tmp examples; they remain reference-only.

**Further Considerations**
1. Recommendation: define a small confidence rubric (for example High only) in the generator settings so contract emission behavior stays deterministic across model versions.
2. Recommendation: add a per-run switch to treat omitted low-confidence candidates as warnings vs informational output, depending on CI strictness.
3. Recommendation: standardize generated test class naming as TypeTests to keep dotnet test filtering predictable for automation.