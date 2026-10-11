using ContractCheck.Samples.Inventory;
using SampleInventory = ContractCheck.Samples.Inventory.Inventory;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of the async <see cref="IInventory"/>: <c>DbcPropertyTest</c> awaits the returned tasks, so postconditions
/// that ContractCheck checks on task completion – and <c>EnsuresOnThrow</c> on faulted tasks – take part.
/// </summary>
[Properties(Arbitrary = [typeof(Generators)])]
public class InventoryProperties
{
    private static IInventory NewInventory() => Dbc.Make<IInventory>(new SampleInventory());

    [Property]
    public Property Restock_keeps_the_contracts(NonEmptyString sku, int quantity) =>
        DbcPropertyTest.CheckAsync(NewInventory, inventory => inventory.RestockAsync(sku.Get, quantity));

    /// <summary>A reservation beyond the stock throws <see cref="InvalidOperationException"/> (documented), after its <c>EnsuresOnThrow</c> is checked.</summary>
    [Property]
    public Property Reserve_after_a_restock_keeps_the_contracts(PositiveInt stock, int quantity) =>
        DbcPropertyTest.CheckAsync(
            () =>
            {
                IInventory inventory = NewInventory();
                inventory.RestockAsync("apple", stock.Get).GetAwaiter().GetResult();
                return inventory;
            },
            async inventory =>
            {
                try
                {
                    await inventory.ReserveAsync("apple", quantity);
                }
                catch (InvalidOperationException)
                {
                }
            });

    /// <summary>Random calls generated from the interface; a short reservation's documented exception is an expected outcome.</summary>
    [Property]
    public Property Any_calls_keep_the_contracts() =>
        DbcPropertyTest.CheckCalls(NewInventory, new CallOptions { ExpectedExceptions = [typeof(InvalidOperationException)] });

    [Property]
    public Property Restocks_reservations_and_queries_keep_the_contracts(InventoryStep[] steps) =>
        DbcPropertyTest.CheckSequenceAsync(NewInventory, steps.Select(step => (Func<IInventory, Task>)step.Apply));
}
