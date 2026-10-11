#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Async;

/// <summary>
/// Async members (resolution R20). For <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c> and <c>ValueTask&lt;T&gt;</c> members,
/// <c>Contract.Result&lt;T&gt;()</c> is the awaited value; postconditions, member invariants and interface invariants are checked
/// when the task completes; <c>EnsuresOnThrow</c> covers synchronous throws, faulted tasks and cancellations.
/// Scenarios ported from CodeContracts Foxtrot/Tests/AsyncPostconditions.
/// </summary>
public class AsyncTests
{
    [Fact]
    public async Task Awaited_result_satisfies_the_postcondition_ported_from_AsyncPostconditionsInInterface()
    {
        Assert.Equal(42, await Dbc.Make<IAsyncService>(new AsyncService()).FooAsync(42));
    }

    [Fact]
    public async Task Awaited_result_violating_the_postcondition_faults_the_task_ported_from_AsyncPostconditionsInInterface()
    {
        Task<int> call = Dbc.Make<IAsyncService>(new AsyncService()).FooAsync(43);

        ContractViolationException exception = await Expect.ViolationAsync(ContractFailureKind.Postcondition, () => call);
        Assert.Equal("Should be 42!", exception.UserMessage);
    }

    [Fact]
    public async Task Postcondition_on_state_is_checked_after_completion_not_at_return()
    {
        // The state changes after the first await; checking at synchronous return would fail spuriously.
        IAsyncService proxy = Dbc.Make<IAsyncService>(new AsyncService());

        await proxy.IncrementAsync();

        Assert.Equal(1, proxy.Count);
    }

    [Fact]
    public async Task Broken_async_state_change_is_detected()
    {
        await Expect.ViolationAsync(ContractFailureKind.Postcondition, () => Dbc.Make<IAsyncService>(new AsyncService { Broken = true }).IncrementAsync());
    }

    [Fact]
    public async Task ValueTask_members_are_checked()
    {
        IAsyncService proxy = Dbc.Make<IAsyncService>(new AsyncService());

        Assert.Equal(2, await proxy.DoubleAsync(1));
        await proxy.ResetAsync();
        await Expect.ViolationAsync(ContractFailureKind.Postcondition, async () => await Dbc.Make<IAsyncService>(new AsyncService { Broken = true }).DoubleAsync(1));
        await Expect.ViolationAsync(ContractFailureKind.Postcondition, async () => await Dbc.Make<IAsyncService>(new AsyncService { Broken = true }).ResetAsync());
    }

    [Fact]
    public async Task Faulted_task_does_not_check_postconditions_ported_from_NormalAsyncPostcondition()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Dbc.Make<IAsyncService>(new AsyncService()).ThrowAfterAwaitAsync(correct: true));
    }

    [Fact]
    public async Task Faulted_task_checks_EnsuresOnThrow_ported_from_MixedAsyncPostcondition()
    {
        ContractViolationException exception = await Expect.ViolationAsync(
            ContractFailureKind.PostconditionOnException,
            () => Dbc.Make<IAsyncService>(new AsyncService()).ThrowAfterAwaitAsync(correct: false));

        Assert.Equal("Ooops!", exception.UserMessage);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task Cancellation_does_not_trigger_unrelated_EnsuresOnThrow_ported_from_CancelledAsyncPostcondition()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        Task call = Dbc.Make<IAsyncService>(new AsyncService()).WaitAsync(cancelled.Token, strict: false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
        Assert.True(call.IsCanceled);
    }

    [Fact]
    public async Task Cancellation_is_checked_by_EnsuresOnThrow_of_OperationCanceledException()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Expect.ViolationAsync(
            ContractFailureKind.PostconditionOnException,
            () => Dbc.Make<IAsyncService>(new AsyncService()).WaitAsync(cancelled.Token, strict: true));
    }

    [Fact]
    public void Preconditions_of_async_members_are_checked_synchronously()
    {
        IAsyncService proxy = Dbc.Make<IAsyncService>(new AsyncService());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.FooAsync(-1));
    }

    [Fact]
    public void Synchronous_throw_of_an_async_member_checks_EnsuresOnThrow()
    {
        Expect.Violation(ContractFailureKind.PostconditionOnException, () => Dbc.Make<IAsyncService>(new AsyncService()).ThrowSynchronously());
    }

    [Fact]
    public void Postcondition_on_the_returned_task_itself_is_checked_at_return()
    {
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IAsyncService>(new AsyncService()).NullTask());
    }

    [Fact]
    public async Task Interface_invariants_are_checked_when_the_task_completes()
    {
        // TemporarilyBreak sets Count to -1 before its await and restores it after: no failure, because the
        // invariant is checked on completion. LeaveBroken does not restore it.
        IAsyncService proxy = Dbc.Make<IAsyncService>(new AsyncService());

        await proxy.TemporarilyBreakAsync();
        await Expect.ViolationAsync(ContractFailureKind.Invariant, () => proxy.LeaveBrokenAsync());
    }

    [Fact]
    public async Task Async_members_without_deferred_checks_return_the_original_task()
    {
        var target = new AsyncService();

        Task task = Dbc.Make<IAsyncService>(target).UncheckedAsync();

        Assert.Same(target.LastUncheckedTask, task);
        await task;
    }
}

[ContractClass(typeof(AsyncServiceContracts))]
public interface IAsyncService
{
    int Count { get; }

    Task<int> FooAsync(int result);

    Task IncrementAsync();

    ValueTask<int> DoubleAsync(int value);

    ValueTask ResetAsync();

    Task<int> ThrowAfterAwaitAsync(bool correct);

    Task WaitAsync(CancellationToken token, bool strict);

    Task ThrowSynchronously();

    Task NullTask();

    Task TemporarilyBreakAsync();

    Task LeaveBrokenAsync();

    [Pure]
    Task UncheckedAsync();
}

public sealed class AsyncService : IAsyncService
{
    public bool Broken { get; init; }

    public int Count { get; private set; }

    public Task? LastUncheckedTask { get; private set; }

    public async Task<int> FooAsync(int result)
    {
        await Task.Delay(1);
        return result;
    }

    public async Task IncrementAsync()
    {
        await Task.Yield();
        Count += Broken ? 2 : 1;
    }

    public async ValueTask<int> DoubleAsync(int value)
    {
        await Task.Yield();
        return Broken ? value : value * 2;
    }

    public async ValueTask ResetAsync()
    {
        await Task.Yield();
        Count = Broken ? 7 : 0;
    }

    public async Task<int> ThrowAfterAwaitAsync(bool correct)
    {
        await Task.Delay(1);
        throw new InvalidOperationException("thrown after await");
    }

    public async Task WaitAsync(CancellationToken token, bool strict) => await Task.Delay(4200, token);

    public Task ThrowSynchronously() => throw new NotSupportedException("thrown before any task exists");

    public Task NullTask() => null!;

    public async Task TemporarilyBreakAsync()
    {
        Count = -1;
        await Task.Yield();
        Count = 0;
    }

    public async Task LeaveBrokenAsync()
    {
        await Task.Yield();
        Count = -1;
    }

    public Task UncheckedAsync() => LastUncheckedTask = Task.CompletedTask;
}

[ContractClassFor(typeof(IAsyncService))]
internal sealed class AsyncServiceContracts
{
    public AsyncServiceContracts()
    {
        Dbc.Def(static (IAsyncService x, int result) => x.FooAsync(result),
            static (IAsyncService x, int result) => Contract.Requires(result >= 0),
            static (IAsyncService x, int result) => Contract.Ensures(Contract.Result<int>() == 42, "Should be 42!"));

        Dbc.Def(static (IAsyncService x) => x.IncrementAsync(),
            static (IAsyncService x) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1));

        Dbc.Def(static (IAsyncService x, int value) => x.DoubleAsync(value),
            static (IAsyncService x, int value) => Contract.Ensures(Contract.Result<int>() == 2 * value));

        Dbc.Def(static (IAsyncService x) => x.ResetAsync(),
            static (IAsyncService x) => Contract.Ensures(x.Count == 0));

        Dbc.Def(static (IAsyncService x, bool correct) => x.ThrowAfterAwaitAsync(correct),
            static (IAsyncService x, bool correct) => Contract.Ensures(Contract.Result<int>() == 42),
            static (IAsyncService x, bool correct) => Contract.EnsuresOnThrow<InvalidOperationException>(correct, "Ooops!"));

        Dbc.Def(static (IAsyncService x, CancellationToken token, bool strict) => x.WaitAsync(token, strict),
            static (IAsyncService x, CancellationToken token, bool strict) => Contract.EnsuresOnThrow<InvalidOperationException>(false),
            static (IAsyncService x, CancellationToken token, bool strict) => Contract.EnsuresOnThrow<OperationCanceledException>(!strict));

        Dbc.Def(static (IAsyncService x) => x.ThrowSynchronously(),
            static (IAsyncService x) => Contract.EnsuresOnThrow<NotSupportedException>(false));

        Dbc.Def(static (IAsyncService x) => x.NullTask(),
            static (IAsyncService x) => Contract.Ensures(Contract.Result<Task>() != null));

        Dbc.Def(static (IAsyncService x) => Contract.Invariant(x.Count >= 0));
    }
}
