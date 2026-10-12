# FsCheck 3 patterns for contract properties (C#)

FsCheck 3 with xUnit v3 (`FsCheck.Xunit.v3`). The C# API is in `FsCheck.Fluent` (`Gen`, `Arb`, `ArbMap`, `Prop`) –
FsCheck 2 names such as `Arb.Default.*` and `Arb.Generate<T>()` are gone; use `ArbMap.Default`.
Worked examples: `ContractCheck.Samples.PropertyTests` in the ContractCheck repository.

## Properties

```csharp
[Property]                                            // FsCheck generates the parameters
public Property Withdraw_keeps_the_contracts(float amount) =>
    DbcPropertyTest.Check(() => Dbc.Make<IAccount>(new Account(100)), account => account.Withdraw(amount));

[Property]                                            // explicit generator: no parameters, Prop.ForAll
public Property Max_keeps_the_contracts() =>
    Prop.ForAll(Arb.From(Gen.Choose(1, 5).SelectMany(n => Gen.ArrayOf(Gen.Choose(-9, 9), n))),
        values => DbcPropertyTest.Check(() => Dbc.Make<IMaxUtils>(new MaxUtils()), utils => utils.Max(values)));

[Property]                                            // stateful: one proxy, a generated call log
public Property Pushes_and_pops_keep_the_contracts(StackStep[] steps) =>
    DbcPropertyTest.CheckSequence(() => Dbc.Make<IStack>(new Stack()), steps.Select(s => (Action<IStack>)s.Apply));

[Property]                                            // stateful without step code: calls of every member
public Property Any_calls_keep_the_contracts() =>
    DbcPropertyTest.CheckCalls(() => Dbc.Make<IStack>(new Stack()),
        new CallOptions { ArbMap = ArbMap.Default.Merge<Generators>(), ExpectedExceptions = [typeof(InvalidOperationException)] });
```

- `DbcPropertyTest.Check` / `CheckAsync` (ContractCheck.FsCheck): an entry-check failure discards the case; a
  postcondition, invariant or any other exception fails it. `CheckSequence` / `CheckSequenceAsync`: a step rejected by
  an entry check is skipped and the sequence goes on; any other failure fails the case.
- `Check` awaits whatever the call returns (`Task`, `ValueTask`, `ValueTask<T>`); `CheckAsync` takes `Task`-returning
  delegates, so `async` lambdas bind to it on every C# version.
- `CheckCalls<T>` generates the sequence itself: methods, getters, setters and indexers of `T` and its base interfaces
  with arguments from `CallOptions.ArbMap`; returned tasks are awaited; calls throwing a `CallOptions.ExpectedExceptions`
  type count as normal (contract failures never do); `Exclude` drops members by name (indexers: `Item`). Members with
  `out`/`ref`/pointer parameters, open generic methods and types without a generator are skipped –
  `DbcPropertyTest.CallableMembers<T>(options)` lists the rest. FsCheck shrinks the sequence and the arguments; the
  counterexample prints as `[Push("a"), Pop(), Pop()]`.
- Settings: `[Property(MaxTest = 500, MaxRejected = 2000, Replay = "(seed,gamma)")]`; for a whole class
  `[Properties(Arbitrary = [typeof(Generators)], MaxTest = …)]`.
- In a `[Fact]`: `Check.One(Config.QuickThrowOnFailure.WithMaxTest(200), property)` throws on falsification; the message
  starts with `Falsifiable, after N tests (k shrinks) (seed,gamma)`, then `Replay directly at failing step with
  (seed,gamma,size)`, the `Original:` and (after shrinking) `Shrunk:` arguments, and the exception.
- `dotnet test` shows FsCheck's output only for failures. To see "Ok, passed 100 tests" run the xUnit v3 test
  executable: `bin/Debug/<tfm>/<Project>.PropertyTests -class <Namespace.Class> -showliveoutput`.

Pinning a known defect (only when the user decided to keep it for now):

```csharp
internal static class PropertyAssert
{
    public static void Falsified(Property property, string expected, int maxTest = 100)
    {
        Exception failure = Assert.ThrowsAny<Exception>(() => Check.One(Config.QuickThrowOnFailure.WithMaxTest(maxTest), property));
        Assert.Contains("Falsifiable", failure.Message, StringComparison.Ordinal);
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }
}

[Fact]   // fails once the defect is fixed – then delete it and the exclusion in the general property
public void Known_defect_max_of_an_empty_array_breaks_the_Exists_postcondition() =>
    PropertyAssert.Falsified(
        Prop.ForAll(Arb.From(Gen.Frequency((1, Gen.Constant(Array.Empty<int>())), (3, ArbMap.Default.GeneratorFor<int[]>()))),
            xs => DbcPropertyTest.Check(() => Dbc.Make<IMaxUtils>(new MaxUtils()), u => u.Max(xs))),
        "Postcondition failed");
```

## What the default generators produce

Measured with FsCheck 3.3.4 (size grows from 0 to 100 over a run):

| Type | Default | Consequence |
|---|---|---|
| `string` | never `null`; `""` ~1–17% | null-rejecting preconditions are never exercised – use `Gen.OrNull()` or a `null` case in `Gen.Frequency` |
| `int` | within ±size (≤ 100) | `int.MinValue`, `int.MaxValue`, overflow are never hit – use `IntWithMinMax` (~4.5% each) or `DoNotSize<int>` |
| `float`/`double` | +∞ ~2.3%, −∞ ~1.8%, NaN ~2.2%, also `MaxValue`; `0` rare | edge values appear, but rarely – targeted generator for a deterministic test; `NormalFloat` for finite doubles; add `0` yourself |
| `decimal` | uniform in ±size, ~50% negative, never `0`, never a whole number | division by zero, zero amounts and integral values are never tested – add them with `Gen.Frequency`; `Gen.Choose` + scaling for money |
| `T[]` | length 0..size; empty ~1–17% | for "empty" defects force it: `Gen.Frequency((1, Gen.Constant(Array.Empty<int>())), (3, …))` |

Wrappers: `PositiveInt`, `NonNegativeInt`, `NegativeInt`, `NonZeroInt`, `NonEmptyString`, `NonWhiteSpaceString`,
`StringNoNullChar`, `NonNull<T>`, `NonEmptyArray<T>`, `NonEmptySet<T>`, `FixedLengthArray<T>`, `IntWithMinMax`,
`DoNotSize<T>`, `NormalFloat`, `Interval`. Read the value with `.Get` (or `.Item`).

## Generators

```csharp
public static class Generators            // registered via [Property(Arbitrary = [typeof(Generators)])]
{
    // Domain values: finite money in cents; a shrinker makes counterexamples small.
    public static Arbitrary<Money> Money() =>
        Arb.From(Gen.Choose(1, 1_000_000).Select(c => new Money(c)), m => m.Cents > 1 ? [new Money(1), new Money(m.Cents / 2)] : []);

    // Valid objects only: follow the documented constructor rules, so creation invariants hold.
    public static Arbitrary<AccountSetup> AccountSetup() =>
        Arb.From(
            from overdraft in Gen.Elements(true, false)
            from limit in overdraft ? Gen.Choose(1, 100_000) : Gen.Constant(0)
            from opening in Gen.Choose(1 - limit, 1_000_000)
            select new AccountSetup(opening, overdraft, limit));

    // Steps for sequences: an enum + data, with ToString() so a counterexample reads like a call log.
    public static Arbitrary<StackStep> StackStep() =>
        Arb.From(
            from action in Gen.Elements(StackAction.Push, StackAction.Push, StackAction.Pop)
            from value in Gen.Frequency((1, Gen.Constant<string?>(null)), (7, ArbMap.Default.GeneratorFor<NonEmptyString>().Select(s => (string?)s.Get)))
            select new StackStep(action, value));
}
```

- Combinators: `Gen.Choose(lo, hi)`, `Gen.Elements(…)`, `Gen.Constant(x)`, `Gen.OneOf(…)`, `Gen.Frequency((w, gen), …)`,
  `Gen.ArrayOf(gen[, n])`, `Gen.ListOf`, `Gen.NonEmptyListOf`, `Gen.Shuffle`, `Gen.SubListOf`, `Gen.Sized(size => …)`,
  `gen.OrNull()`, `gen.Where(pred)` (filtering – keep it rare), LINQ `from … select`.
- Defaults for any type: `ArbMap.Default.GeneratorFor<T>()` / `ArbMap.Default.ArbFor<T>()`.
- Only `public static` methods returning `Arbitrary<T>` are picked up; keep helpers private.
- A registered `Arbitrary<T>` also supplies `T[]`, `List<T>`, `NonEmptyArray<T>` … of it.

## Pitfalls

- **Arguments exhausted**: more than `MaxRejected` cases were discarded (preconditions rejected them). Make the
  generator produce mostly valid inputs (small argument counts, valid indexes, values in range) and keep a minority of
  invalid ones so the rejection path still runs. Check the mix with `Prop.Classify(…)`/`Prop.Collect(…)`.
- **Rare edge cases make flaky tests**: a value hit with probability 1e-3 per case fails one run in ten. Either target it
  (dedicated generator, deterministic) or exclude it explicitly with a comment that points to the test covering it.
- **Shared state**: create the proxy inside the factory (`() => Dbc.Make…`) so each case starts fresh;
  `CheckSequence` shares one proxy across the steps of one case only.
- **Floats**: `x == old + amount` postconditions hold when the code computes the same expression; accumulated sums
  differ by rounding – prefer integers/decimals in the domain or tolerance in the clause.
- **Expected exceptions**: catch only the documented exception type, inside the step, after the proxy has checked
  `EnsuresOnThrow`; never catch `ContractViolationException`, nor a type that `Requires<TException>` clauses throw
  (`ArgumentException` and subclasses) – precondition rejections would vanish into the catch.
- **Masking**: a property that always fails on one finding never shows the next one. Split properties by input class
  and keep single-call properties next to the sequences.
- **State and correlated inputs**: generate the object's starting state (a setup record) rather than testing fresh
  objects only, and generate arguments that must fit each other or the state as one record with its own shrinker.
- **Nullable**: with nullable reference types enabled, pass generated nulls as `value!` (or declare step fields `string?`).
