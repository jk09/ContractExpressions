using FsCheck;
using FsCheck.Fluent;

namespace ContractExpressions4.Testing;

/// <summary>
/// FsCheck-based property test helpers for Design-by-Contract proxies.
///
/// Intended for use with the [Property] attribute from FsCheck.Xunit.v3.
/// Method parameters are generated randomly by FsCheck; pass them into Check(…).
///
/// Per test case:
///   - Calls proxyFactory() — validates invariants at creation (failure = test fails).
///   - Calls invoke(proxy) with the random argument(s).
///   - Precondition violation → discards the test case (inconclusive, not a failure).
///   - Postcondition or invariant violation → test fails.
///   - No exception → test passes.
/// </summary>
public static class DbcPropertyTest
{
    // For methods with a return value (the return value itself is not checked here;
    // contract correctness is asserted by the proxy's postcondition evaluation).
    public static Property Check<T, TResult>(Func<T> proxyFactory, Func<T, TResult> invoke)
        where T : class
        => RunCheck(proxyFactory, proxy => { invoke(proxy); });

    // For void methods.
    public static Property Check<T>(Func<T> proxyFactory, Action<T> invoke)
        where T : class
        => RunCheck(proxyFactory, invoke);

    private static Property RunCheck<T>(Func<T> proxyFactory, Action<T> invoke)
        where T : class
    {
        // proxyFactory() calls Dbc.Make<T> which validates invariants at creation.
        // A ContractViolationException here means the implementation is broken → test fails.
        T proxy = proxyFactory();

        bool preconditionMet = true;
        try
        {
            invoke(proxy);
        }
        catch (ContractViolationException ex) when (ex.Kind == ContractKind.Precondition)
        {
            // Random args didn't satisfy the precondition → discard this test case.
            preconditionMet = false;
        }
        // Any other ContractViolationException (postcondition, invariant after the call)
        // propagates and FsCheck marks the test as failed — correct behaviour.

        return FsCheck.Fluent.Prop.Implies(preconditionMet, true);
    }
}

