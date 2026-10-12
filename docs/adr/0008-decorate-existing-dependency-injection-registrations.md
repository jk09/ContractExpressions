# 0008. Decorate existing dependency-injection registrations

- Status: Accepted
- Date: 2026-10-12
- Features: –
- Spec: canyon-tundra-ymf1vj

## Context
Using `Dbc.Make` with Microsoft.Extensions.DependencyInjection meant rewriting registrations as factories, losing the
container's activation of the implementation and, done naively, disposing it twice (once as the implementation, once
through the proxy of a disposable interface).

## Options considered
1. **Document the factory pattern** – no package; boilerplate per registration, easy to get disposal wrong.
2. **New registration methods** (`AddContractChecked<TService, TImpl>()`) – must mirror every `Add*` overload,
   lifetime and keyed variant.
3. **Decorate existing registrations** (`AddContractChecks<T>()`, `AddContractChecks()`) – works after any
   registration style; rewrites `ServiceDescriptor`s.
4. **Scrutor's `Decorate`** – a third-party dependency for one call.

## Decision
Option 3, chosen by the owner on 2026-10-11, in the package `ContractCheck.DependencyInjection`
(Microsoft.Extensions.DependencyInjection.Abstractions 8.0, keyed services). For a non-disposable interface the original
registration moves to a private key, so the container still creates and disposes the implementation; for a disposable
interface the proxy forwards `Dispose`, so the decorated factory creates the implementation itself; instance
registrations are wrapped at once. Calling it again doesn't wrap twice.

## Consequences
- Registrations added after the call, and open-generic registrations, are not wrapped.
- Other containers need their own adapters.
