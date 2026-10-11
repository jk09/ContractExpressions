using System.Globalization;
using ContractCheck.Samples.BankAccount;
using ContractCheck.Samples.CreditDebit;
using ContractCheck.Samples.Stack;
using SampleInventory = ContractCheck.Samples.Inventory;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Custom FsCheck generators for the property tests. Register them per test with
/// [Property(Arbitrary = [typeof(Generators)])] or per class with [Properties(Arbitrary = [typeof(Generators)])];
/// each public static method returning Arbitrary&lt;T&gt; supplies T.
/// </summary>
public static class Generators
{
    /// <summary>Money from 0.01 to 10 000.00 in whole cents: finite and positive, unlike FsCheck's floats. Shrinks towards 0.01.</summary>
    public static Arbitrary<Money> Money() =>
        Arb.From(Gen.Choose(1, 1_000_000).Select(cents => new Money(cents)), money => ShrinkCents(money.Cents).Select(cents => new Money(cents)));

    /// <summary>
    /// Accounts whose <see cref="IAccount"/> invariants hold at creation: without overdraft the limit is 0, with it 0.01–1000,
    /// and the opening amount is above minus the limit.
    /// </summary>
    public static Arbitrary<AccountSetup> AccountSetup() =>
        Arb.From(
            from supportsOverdraft in Gen.Elements(true, false)
            from limitCents in supportsOverdraft ? Gen.Choose(1, 100_000) : Gen.Constant(0)
            from openingCents in Gen.Choose(1 - limitCents, 1_000_000)
            select new AccountSetup(openingCents, supportsOverdraft, limitCents));

    /// <summary>Deposits and withdrawals of <see cref="Money"/>.</summary>
    public static Arbitrary<AccountStep> AccountStep() =>
        Arb.From(
            from deposit in Gen.Elements(true, false)
            from amount in Money().Generator
            select new AccountStep(deposit, amount));

    /// <summary>Credits and debits of <see cref="Money"/>.</summary>
    public static Arbitrary<SimpleAccountStep> SimpleAccountStep() =>
        Arb.From(
            from credit in Gen.Elements(true, false)
            from amount in Money().Generator
            select new SimpleAccountStep(credit, amount));

    /// <summary>Pushes, pops and updates – one value in eight is <see langword="null"/>, which FsCheck's strings never are.</summary>
    public static Arbitrary<StackStep> StackStep()
    {
        Gen<string?> values = Gen.Frequency(
            (1, Gen.Constant<string?>(null)),
            (7, ArbMap.Default.GeneratorFor<NonEmptyString>().Select(text => (string?)text.Get)));
        return Arb.From(
            from action in Gen.Elements(StackAction.Push, StackAction.Push, StackAction.Pop, StackAction.UpdateAt)
            from value in values
            from index in Gen.Choose(-1, 5)
            select new StackStep(action, value, index));
    }

    /// <summary>Restocks, reservations and stock queries on three SKUs; quantities include invalid ones (≤ 0).</summary>
    public static Arbitrary<InventoryStep> InventoryStep() =>
        Arb.From(
            from action in Gen.Elements(InventoryAction.Restock, InventoryAction.Reserve, InventoryAction.Available)
            from sku in Gen.Elements("apple", "pear", "plum")
            from quantity in Gen.Choose(-2, 30)
            select new InventoryStep(action, sku, quantity));

    private static IEnumerable<int> ShrinkCents(int cents)
    {
        if (cents > 1)
        {
            yield return 1;
            yield return cents / 2;
        }
    }
}

/// <summary>An amount of money in whole cents.</summary>
public readonly record struct Money(int Cents)
{
    /// <summary>The amount as the samples' <see cref="float"/>.</summary>
    public float Value => Cents / 100f;

    /// <inheritdoc />
    public override string ToString() => (Cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>Constructor arguments of an <see cref="Account"/>, in cents.</summary>
public sealed record AccountSetup(int OpeningCents, bool SupportsOverdraft, int OverdraftLimitCents)
{
    /// <summary>Creates the contract-checked account.</summary>
    public IAccount Create() => Dbc.Make<IAccount>(new Account(OpeningCents / 100f, SupportsOverdraft, OverdraftLimitCents / 100f));
}

/// <summary>One generated call on an <see cref="IAccount"/>.</summary>
public sealed record AccountStep(bool Deposit, Money Amount)
{
    /// <summary>Makes the call.</summary>
    public void Apply(IAccount account)
    {
        if (Deposit)
        {
            account.Deposit(Amount.Value);
        }
        else
        {
            account.Withdraw(Amount.Value);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{(Deposit ? nameof(IAccount.Deposit) : nameof(IAccount.Withdraw))}({Amount})";
}

/// <summary>One generated call on an <see cref="ISimpleAccount"/>.</summary>
public sealed record SimpleAccountStep(bool Credit, Money Amount)
{
    /// <summary>Makes the call.</summary>
    public void Apply(ISimpleAccount account)
    {
        if (Credit)
        {
            account.Credit(Amount.Value);
        }
        else
        {
            account.Debit(Amount.Value);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{(Credit ? nameof(ISimpleAccount.Credit) : nameof(ISimpleAccount.Debit))}({Amount})";
}

/// <summary>The calls a <see cref="StackStep"/> can make.</summary>
public enum StackAction
{
    /// <summary><see cref="INonNullStack{T}.Push"/>.</summary>
    Push,

    /// <summary><see cref="INonNullStack{T}.Pop"/>.</summary>
    Pop,

    /// <summary><see cref="INonNullStackWithUpdates{T}.UpdateAt"/>.</summary>
    UpdateAt,
}

/// <summary>One generated call on an <see cref="INonNullStackWithUpdates{T}"/> of strings.</summary>
public sealed record StackStep(StackAction Action, string? Value, int Index)
{
    /// <summary>Makes the call.</summary>
    public void Apply(INonNullStackWithUpdates<string> stack)
    {
        switch (Action)
        {
            case StackAction.Push:
                stack.Push(Value!);
                break;
            case StackAction.Pop:
                stack.Pop();
                break;
            default:
                stack.UpdateAt(Index, Value!);
                break;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Action switch
    {
        StackAction.Push => $"Push({Quote(Value)})",
        StackAction.Pop => "Pop()",
        _ => $"UpdateAt({Index}, {Quote(Value)})",
    };

    private static string Quote(string? value) => value is null ? "null" : $"\"{value}\"";
}

/// <summary>The calls an <see cref="InventoryStep"/> can make.</summary>
public enum InventoryAction
{
    /// <summary><see cref="SampleInventory.IInventory.RestockAsync"/>.</summary>
    Restock,

    /// <summary><see cref="SampleInventory.IInventory.ReserveAsync"/>.</summary>
    Reserve,

    /// <summary><see cref="SampleInventory.IInventory.Available"/>.</summary>
    Available,
}

/// <summary>One generated call on an <see cref="SampleInventory.IInventory"/>.</summary>
public sealed record InventoryStep(InventoryAction Action, string Sku, int Quantity)
{
    /// <summary>
    /// Makes the call. A reservation beyond the stock throws <see cref="InvalidOperationException"/> – documented behaviour,
    /// so it is caught here; its <c>EnsuresOnThrow</c> clause has already been checked by the proxy.
    /// </summary>
    public async Task Apply(SampleInventory.IInventory inventory)
    {
        switch (Action)
        {
            case InventoryAction.Restock:
                await inventory.RestockAsync(Sku, Quantity);
                break;
            case InventoryAction.Reserve:
                try
                {
                    await inventory.ReserveAsync(Sku, Quantity);
                }
                catch (InvalidOperationException)
                {
                }

                break;
            default:
                inventory.Available(Sku);
                break;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Action == InventoryAction.Available ? $"Available(\"{Sku}\")" : $"{Action}(\"{Sku}\", {Quantity})";
}
