#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Failures.Events;

/// <summary>
/// Every failure raises the BCL <c>Contract.ContractFailed</c> event first (resolution R1), with the CodeContracts semantics:
/// <c>SetHandled()</c> lets the call continue, <c>SetUnwind()</c> or a throwing handler still ends in a
/// <c>ContractViolationException</c>, and <c>Requires&lt;TException&gt;</c> always throws <c>TException</c>.
/// These tests subscribe to the process-wide event, so they run in a non-parallel collection.
/// </summary>
[Collection(ContractFailedHandlers.Name)]
public class ContractFailedEventTests
{
    [Fact]
    public void Handler_receives_the_failure_details()
    {
        ContractFailedEventArgs? received = null;
        using (Subscribe((_, e) => received = e))
        {
            Assert.Throws<ContractViolationException>(() => Dbc.Make<IEvented>(new Evented()).Withdraw(-1));
        }

        Assert.NotNull(received);
        Assert.Equal(ContractFailureKind.Precondition, received.FailureKind);
        Assert.Equal("amount > 0", received.Condition);
        Assert.Equal("Precondition failed: amount > 0  amount must be positive", received.Message);
        Assert.Null(received.OriginalException);
    }

    [Fact]
    public void Handler_receives_the_thrown_exception_for_EnsuresOnThrow()
    {
        ContractFailedEventArgs? received = null;
        using (Subscribe((_, e) => received = e))
        {
            Assert.Throws<ContractViolationException>(() => Dbc.Make<IEvented>(new Evented()).Explode());
        }

        Assert.Equal(ContractFailureKind.PostconditionOnException, received!.FailureKind);
        Assert.IsType<TimeoutException>(received.OriginalException);
    }

    [Fact]
    public void SetHandled_lets_the_call_continue()
    {
        var target = new Evented();
        int handled = 0;
        using (Subscribe((_, e) =>
        {
            handled++;
            e.SetHandled();
        }))
        {
            IEvented proxy = Dbc.Make<IEvented>(target);

            proxy.Withdraw(-1); // the precondition fails and is handled: the member still runs
            proxy.Withdraw(5);
            Assert.Equal(-4, proxy.Balance()); // the postcondition fails and is handled: the result is returned
        }

        Assert.Equal(2, handled);
        Assert.Equal(-4, target.Balance());
    }

    [Fact]
    public void SetHandled_on_EnsuresOnThrow_rethrows_the_original_exception()
    {
        using (Subscribe((_, e) => e.SetHandled()))
        {
            Assert.Throws<TimeoutException>(() => Dbc.Make<IEvented>(new Evented()).Explode());
        }
    }

    [Fact]
    public void SetUnwind_still_throws_ContractViolationException()
    {
        using (Subscribe((_, e) => e.SetUnwind()))
        {
            ContractViolationException exception = Assert.Throws<ContractViolationException>(() => Dbc.Make<IEvented>(new Evented()).Withdraw(-1));

            Assert.Equal("System.Diagnostics.Contracts.ContractException", exception.InnerException?.GetType().FullName);
        }
    }

    [Fact]
    public void Throwing_handler_still_ends_in_ContractViolationException()
    {
        using (Subscribe((_, _) => throw new FormatException("handler failed")))
        {
            ContractViolationException exception = Assert.Throws<ContractViolationException>(() => Dbc.Make<IEvented>(new Evented()).Withdraw(-1));

            Assert.IsType<FormatException>(exception.InnerException?.InnerException);
        }
    }

    [Fact]
    public void Typed_precondition_raises_the_event_but_cannot_be_handled()
    {
        int raised = 0;
        using (Subscribe((_, e) =>
        {
            raised++;
            e.SetHandled();
        }))
        {
            Assert.Throws<ArgumentNullException>(() => Dbc.Make<IEvented>(new Evented()).Name(null!));
        }

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Typed_precondition_throws_its_exception_even_when_a_handler_unwinds()
    {
        using (Subscribe((_, e) => e.SetUnwind()))
        {
            Assert.Throws<ArgumentNullException>(() => Dbc.Make<IEvented>(new Evented()).Name(null!));
        }
    }

    private static Subscription Subscribe(EventHandler<ContractFailedEventArgs> handler) => new(handler);

    private sealed class Subscription : IDisposable
    {
        private readonly EventHandler<ContractFailedEventArgs> handler;

        public Subscription(EventHandler<ContractFailedEventArgs> handler)
        {
            this.handler = handler;
            Contract.ContractFailed += handler;
        }

        public void Dispose() => Contract.ContractFailed -= handler;
    }
}

[ContractClass(typeof(EventedContracts))]
public interface IEvented
{
    int Balance();

    void Withdraw(int amount);

    void Explode();

    void Name(string name);
}

public sealed class Evented : IEvented
{
    private int balance;

    public int Balance() => balance;

    public void Withdraw(int amount) => balance -= amount;

    public void Explode() => throw new TimeoutException();

    public void Name(string name)
    {
    }
}

[ContractClassFor(typeof(IEvented))]
internal sealed class EventedContracts
{
    public EventedContracts()
    {
        Dbc.Def(static (IEvented x, int amount) => x.Withdraw(amount),
            static (IEvented x, int amount) => Contract.Requires(amount > 0, "amount must be positive"),
            static (IEvented x, int amount) => Contract.Ensures(x.Balance() == Contract.OldValue(x.Balance()) - amount));

        Dbc.Def(static (IEvented x) => x.Balance(),
            static (IEvented x) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IEvented x) => x.Explode(),
            static (IEvented x) => Contract.EnsuresOnThrow<TimeoutException>(false));

        Dbc.Def(static (IEvented x, string name) => x.Name(name),
            static (IEvented x, string name) => Contract.Requires<ArgumentNullException>(name != null, "name"));
    }
}
