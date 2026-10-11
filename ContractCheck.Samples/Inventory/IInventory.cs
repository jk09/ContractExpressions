#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Inventory;

/// <summary>
/// Stock per SKU with async updates – shows async postconditions: clauses of <see cref="Task"/>-returning members are
/// checked when the task completes, and <c>EnsuresOnThrow</c> also covers faulted tasks.
/// </summary>
[ContractClass(typeof(InventoryContracts))]
public interface IInventory
{
    int Available(string sku);

    Task RestockAsync(string sku, int quantity);

    /// <summary>Reserves <paramref name="quantity"/> items and returns what is left; throws <see cref="InvalidOperationException"/> when stock is short.</summary>
    Task<int> ReserveAsync(string sku, int quantity);
}

public sealed class Inventory : IInventory
{
    private readonly Dictionary<string, int> stock = [];

    public int Available(string sku) => stock.GetValueOrDefault(sku);

    public async Task RestockAsync(string sku, int quantity)
    {
        await Task.Yield();
        stock[sku] = Available(sku) + quantity;
    }

    public async Task<int> ReserveAsync(string sku, int quantity)
    {
        await Task.Yield();
        int available = Available(sku);
        if (available < quantity)
        {
            throw new InvalidOperationException($"Only {available} of {sku} in stock.");
        }

        stock[sku] = available - quantity;
        return available - quantity;
    }
}
