#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Inventory;

[ContractClassFor(typeof(IInventory))]
internal sealed class InventoryContracts
{
    public InventoryContracts()
    {
        Dbc.Def(static (IInventory x, string sku) => x.Available(sku),
            static (IInventory x, string sku) => Contract.Requires(!string.IsNullOrEmpty(sku)),
            static (IInventory x, string sku) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IInventory x, string sku, int quantity) => x.RestockAsync(sku, quantity),
            static (IInventory x, string sku, int quantity) => Contract.Requires(!string.IsNullOrEmpty(sku)),
            static (IInventory x, string sku, int quantity) => Contract.Requires(quantity > 0),
            static (IInventory x, string sku, int quantity) => Contract.Ensures(x.Available(sku) == Contract.OldValue(x.Available(sku)) + quantity));

        Dbc.Def(static (IInventory x, string sku, int quantity) => x.ReserveAsync(sku, quantity),
            static (IInventory x, string sku, int quantity) => Contract.Requires(!string.IsNullOrEmpty(sku)),
            static (IInventory x, string sku, int quantity) => Contract.Requires(quantity > 0),
            static (IInventory x, string sku, int quantity) => Contract.Ensures(Contract.Result<int>() == Contract.OldValue(x.Available(sku)) - quantity),
            static (IInventory x, string sku, int quantity) => Contract.Ensures(x.Available(sku) == Contract.Result<int>()),
            static (IInventory x, string sku, int quantity) => Contract.EnsuresOnThrow<InvalidOperationException>(
                x.Available(sku) == Contract.OldValue(x.Available(sku)), "a failed reservation must not change the stock"));
    }
}
