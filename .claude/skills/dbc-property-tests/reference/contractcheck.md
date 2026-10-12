# ContractCheck in one page

Runtime Design-by-Contract for .NET interfaces with the `System.Diagnostics.Contracts` vocabulary. Contracts are
expression-tree lambdas registered with `Dbc.Def`; `Dbc.Make<T>` wraps an implementation in a `DispatchProxy` that
checks them on every call. Full documentation: the repository README.

## Shape

```csharp
#define CONTRACTS_FULL                      // first line of every file that calls Contract.* or uses a contract attribute
using System.Diagnostics.Contracts;
using ContractCheck;

[ContractClass(typeof(StackContracts))]
public interface IStack
{
    int Count { get; }
    void Push(string item);
    string Pop();
}

[ContractClassFor(typeof(IStack))]
internal sealed class StackContracts
{
    public StackContracts()                 // runs once, at the first Dbc.Make<IStack>
    {
        Dbc.Def(static (IStack s, string item) => s.Push(item),
            static (IStack s, string item) => Contract.Requires<ArgumentNullException>(item != null, nameof(item)),
            static (IStack s, string item) => Contract.Ensures(s.Count == Contract.OldValue(s.Count) + 1));

        Dbc.Def(static (IStack s) => s.Pop(),
            static (IStack s) => Contract.Requires(s.Count > 0, "stack is empty"),
            static (IStack s) => Contract.Ensures(Contract.Result<string>() != null));

        Dbc.Def(static (IStack s) => Contract.Invariant(s.Count >= 0));   // interface invariant
    }
}

IStack stack = Dbc.Make<IStack>(new Stack());   // checks the invariants, then returns the checking proxy
```

- Selector: `(IFoo x, params…) => x.Member(params…)` – the member and its parameters, in order. Clause lambdas take
  the same parameters. Properties: `static (IFoo x) => x.Prop`; indexers: `static (IFoo x, int i) => x[i]`.
- Setters: `Dbc.DefSetter(static (IFoo x) => x.Prop, static (IFoo x, int value) => Contract.Requires(value >= 0))`
  (index parameters first, the assigned `value` last).
- Overloads exist for 0–15 parameters (`DefSetter`: 0–14 index parameters).
- Several `Dbc.Def` calls for one member add up; base-interface contracts apply to derived interfaces.
- Clauses are evaluated against the implementation the proxy wraps: members called inside a clause (`x.Count`) are not
  contract-checked and don't recurse.
- Generic interfaces: a generic contract class (`[ContractClass(typeof(FooContracts<>))]`) is closed over the
  interface's type arguments.
- `out`/`ref` parameters: the selector passes a placeholder field, `static (IFoo x, string k) => x.TryGet(k, out
  Dbc.Out<int>.Value)`, and clauses read it: `Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<int>.Value >= 0)`.
  `Dbc.Ref<T>.Value` is the value passed in in entry checks and inside `OldValue`, the value at return in
  postconditions; `Dbc.Out<T>.Value` only exists in `Ensures`. `Value2`/`Value3` for more parameters of one type;
  `Contract.ValueAtReturn(out Dbc.Out<int>.Value)` is a synonym. `in` parameters are ordinary lambda parameters.
- `Dbc.Make(typeof(IFoo), target)` for an interface known at run time; `Dbc.HasContracts(typeof(IFoo))` tells whether
  any contract class or clause applies.
- The contract attributes are `[Conditional("CONTRACTS_FULL")]`: `[Pure]`, `[ContractOption]`, `[ContractRuntimeIgnored]`
  vanish from files without the `#define` (`[ContractClass]` survives in Debug builds only). `Dbc.Make` falls back to
  the class marked `[ContractClassFor(typeof(IFoo))]`, but `[Pure]` & co. are silently lost – define the symbol in
  the interface's file too. Never define it project-wide (`Contract.Requires` in method bodies would `FailFast`;
  `Requires<TEx>` does even without it). The package's analyzer reports both (CCK0006 with a code fix, CCK0007).

## Clauses

| Clause | Checked | Failure |
|---|---|---|
| `Requires(cond[, msg])` | on entry | `ContractViolationException`, kind `Precondition` |
| `Requires<TEx>(cond[, msg])` | on entry | throws `TEx`: a `(paramName, message)` constructor gets `msg` as `ParamName`, else `(message)` gets the failure text |
| `Assert` / `Assume` | on entry | kinds `Assert` / `Assume` |
| `Ensures(cond[, msg])` | on normal exit; async members: when the task succeeds | kind `Postcondition` |
| `EnsuresOnThrow<TEx>(cond[, msg])` | when the member throws `TEx` or a subtype (sync throw, faulted or cancelled task) | kind `PostconditionOnException`; inner = the original exception |
| `Invariant(cond[, msg])` – interface form | after `Dbc.Make` and after every non-pure member | kind `Invariant` |
| `Invariant(…)` inside a member's `Dbc.Def` | at that member's exit | kind `Invariant` |

- Order per call: entry checks (definition order) → old values captured → target runs → `EnsuresOnThrow` if it threw →
  `Ensures` → member invariants → interface invariants. Invariants are not checked on entry.
- Pure members skip the interface-invariant check: property getters and methods marked `[Pure]`.
- `Contract.Result<T>()` only in `Ensures`; `T` = the return type, or the awaited type for `Task<T>`/`ValueTask<T>`.
- `Contract.OldValue(expr)` only in `Ensures`/`EnsuresOnThrow`; any non-literal expression, captured on entry (a capture
  that throws only fails if the clause is evaluated). Also inside `ForAll(lo, hi, i => … OldValue(a[i]) …)`.
- `Contract.ForAll` / `Contract.Exists` (collection and `int` range forms) anywhere in a condition.
- Messages may be any string expression over the parameters; evaluated only on failure.
- Definition errors (`InvalidOperationException` naming the clause): selector not an interface member, clause not a
  `Contract` call, `Result` outside `Ensures` or of the wrong type, `OldValue` outside postconditions or on a literal,
  misused `out`/`ref` placeholders or `ValueAtReturn`, `EndContractBlock`, non-invariant clause in the invariant form.
  With the ContractCheck package, the analyzer reports them at build time with the same texts (CCK0001–CCK0005), and
  warns about preconditions added to inherited members through a derived interface (CCK0008).

## Failures

- Every failure raises `Contract.ContractFailed`. A handler calling `e.SetHandled()` makes the call continue;
  otherwise `ContractViolationException` is thrown.
- `ContractViolationException`: `Kind` (`ContractFailureKind`), `Method` (`"<creation>"` for `Dbc.Make`), `Condition`,
  `UserMessage`, `ContractText`, `InterfaceType`, `Member`. Message: `Precondition failed: value >= 0  msg (in IFoo.Bar)`.
- `exception.GetContractFailureKind()` → the kind for `ContractViolationException` and `Requires<TEx>` exceptions, else
  `null`. `exception.IsEntryCheckFailure()` → `true` for preconditions (also typed), `Assert`, `Assume`.

## Switches

- `[ContractOption("runtime", "checking", false)]` on the interface, a member or the assembly: no runtime checks.
- `[ContractOption("contract", "inheritance", false)]` on an interface: base-interface contracts don't apply.
- `[ContractRuntimeIgnored]` on a method or property: clauses that use it aren't checked at runtime.
- `Dbc.CheckingLevel` / runtimeconfig `ContractCheck.CheckingLevel`: `Full` (default), `PreAndPost` (no invariants,
  `Assert`, `Assume`), `Preconditions`, `ReleaseRequires` (`Requires<TEx>` only), `None`. Process-wide; property tests
  need `Full`, so don't change it in test projects.

## Not supported

- Classes (proxies are interface-only), static members, constructors (use invariants – checked by `Dbc.Make`).
- `Contract.EndContractBlock` / legacy if-then-throw blocks: write `Requires<TException>`.
