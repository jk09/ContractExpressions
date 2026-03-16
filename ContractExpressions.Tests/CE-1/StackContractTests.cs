#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests.CE1;

/// <summary>
/// Tests inspired by CodeContracts/Demo/Stack/NonNullStack.cs.
/// Covers: state-dependent preconditions (!IsEmpty), Ensures on Result non-null,
/// OldValue on Count for Push/Pop, Requires non-null on Push.
/// </summary>
public class StackContractTests : IClassFixture<ContractFailureUnwindFixture>
{
    private readonly IStack _proxy;

    public StackContractTests()
    {
        _proxy = Dbc.Make<IStack>(new SimpleStack());
    }

    [Fact]
    public void Push_HappyPath_IncreasesCount()
    {
        _proxy.Push("item1");
        Assert.Equal(1, _proxy.Count);
    }

    [Fact]
    public void Push_NullItem_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Push(null!));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Pop_HappyPath_ReturnsLastPushed()
    {
        _proxy.Push("a");
        _proxy.Push("b");
        var result = _proxy.Pop();
        Assert.Equal("b", result);
    }

    [Fact]
    public void Pop_WhenEmpty_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Pop());
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Pop_ResultIsNotNull()
    {
        _proxy.Push("item");
        var result = _proxy.Pop();
        Assert.NotNull(result);
    }

    [Fact]
    public void Push_CountIncreasesByOne()
    {
        _proxy.Push("first");
        Assert.Equal(1, _proxy.Count);
        _proxy.Push("second");
        Assert.Equal(2, _proxy.Count);
    }

    [Fact]
    public void Pop_CountDecreasesByOne()
    {
        _proxy.Push("a");
        _proxy.Push("b");
        Assert.Equal(2, _proxy.Count);
        _proxy.Pop();
        Assert.Equal(1, _proxy.Count);
    }

    [Fact]
    public void Push_ThenPop_Roundtrip()
    {
        _proxy.Push("x");
        _proxy.Push("y");
        _proxy.Push("z");
        Assert.Equal("z", _proxy.Pop());
        Assert.Equal("y", _proxy.Pop());
        Assert.Equal("x", _proxy.Pop());
        Assert.Equal(0, _proxy.Count);
    }

    [Fact]
    public void IsEmpty_WhenNoItems_ReturnsTrue()
    {
        Assert.True(_proxy.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterPush_ReturnsFalse()
    {
        _proxy.Push("item");
        Assert.False(_proxy.IsEmpty);
    }
}

[ContractClass(typeof(StackContracts))]
interface IStack
{
    int Count { get; }
    bool IsEmpty { get; }
    void Push(object item);
    object Pop();
}

class SimpleStack : IStack
{
    private readonly List<object> _items = new();

    public int Count => _items.Count;
    public bool IsEmpty => _items.Count == 0;

    public void Push(object item)
    {
        _items.Add(item);
    }

    public object Pop()
    {
        var item = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        return item;
    }
}

[ContractClassFor(typeof(IStack))]
class StackContracts
{
    public StackContracts()
    {
        // Push: item must not be null; count increases by one
        Dbc.Def(static (IStack x, object item) => x.Push(item),
                static (IStack x, object item) => Contract.Requires(item != null),
                static (IStack x, object item) => Contract.Ensures(x.Count == Contract.OldValue<int>(x.Count) + 1));

        // Pop: stack must not be empty; result is not null; count decreases by one
        Dbc.Def(static (IStack x) => x.Pop(),
                static (IStack x) => Contract.Requires(!x.IsEmpty),
                static (IStack x) => Contract.Ensures(Contract.Result<object>() != null),
                static (IStack x) => Contract.Ensures(x.Count == Contract.OldValue<int>(x.Count) - 1));
    }
}
