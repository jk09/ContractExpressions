# ContractCheck.DependencyInjection

[Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection) integration for [ContractCheck](../README.md): resolved services are `Dbc.Make` proxies that check their contracts.

```csharp
services.AddScoped<IAccount, Account>();
services.AddSingleton<IClock>(new SystemClock());
services.AddKeyedTransient<IPricing, RetailPricing>("retail");

services.AddContractChecks<IAccount>();   // wrap the registrations of one interface
services.AddContractChecks();             // or: every registered interface that has contracts
```

- Call it **after** the registrations it should wrap; lifetimes and keys stay as registered.
- Type, factory, instance and keyed registrations are wrapped; open-generic registrations can't be and are skipped.
- The implementation is disposed exactly once: for a non-disposable interface the container keeps creating and disposing
  it under a private key; a disposable interface's proxy forwards `Dispose`. Instance registrations are wrapped at once
  and, as before, not disposed by the container.
- Calling it twice doesn't wrap twice. `Dbc.CheckingLevel` (or the runtimeconfig setting `ContractCheck.CheckingLevel`)
  decides what the proxies check.
