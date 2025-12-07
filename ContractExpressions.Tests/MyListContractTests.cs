#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests;

public class MyListContractTests : IClassFixture<ContractFailedFixture>
{
    private readonly IMyList _proxy;

    public MyListContractTests()
    {
        _proxy = Dbc.Make<IMyList>(new MyList());
    }

    [Fact]
    public void Add()
    {
        var index = _proxy.Add(new object());
        Assert.Equal(0, index);
    }

    [Fact]
    public void Clear()
    {
        _proxy.Add(new object());
        _proxy.Add(new object());
        _proxy.Clear();
        Assert.Empty(_proxy);
    }

    [Fact]
    public void Contains_WhenItemExists_ReturnsTrue()
    {
        var item = new object();
        _proxy.Add(item);
        Assert.True(_proxy.Contains(item));
    }

    [Fact]
    public void Contains_WhenItemDoesNotExist_ReturnsFalse()
    {
        var item = new object();
        Assert.False(_proxy.Contains(item));
    }

    [Fact]
    public void IndexOf_WhenItemExists_ReturnsCorrectIndex()
    {
        var item = new object();
        _proxy.Add(new object());
        _proxy.Add(item);
        Assert.Equal(1, _proxy.IndexOf(item));
    }

    [Fact]
    public void IndexOf_WhenItemDoesNotExist_ReturnsMinusOne()
    {
        var item = new object();
        Assert.Equal(-1, _proxy.IndexOf(item));
    }

    [Fact]
    public void Insert()
    {
        _proxy.Add(new object());
        var item = new object();
        _proxy.Insert(0, item);
        Assert.Equal(2, _proxy.Count);
        Assert.Equal(item, _proxy[0]);
    }

    [Fact]
    public void Remove_WhenItemExists()
    {
        var item = new object();
        _proxy.Add(item);
        _proxy.Remove(item);
        Assert.Empty(_proxy);
    }

    [Fact]
    public void RemoveAt()
    {
        var item = new object();
        _proxy.Add(item);
        _proxy.RemoveAt(0);
        Assert.Empty(_proxy);
    }

    [Fact]
    public void Indexer_Get()
    {
        var item = new object();
        _proxy.Add(item);
        Assert.Equal(item, _proxy[0]);
    }

    [Fact]
    public void CopyTo()
    {
        var item1 = new object();
        var item2 = new object();
        _proxy.Add(item1);
        _proxy.Add(item2);

        var array = new object[5];
        _proxy.CopyTo(array, 1);

        Assert.Equal(item1, array[1]);
        Assert.Equal(item2, array[2]);
    }

    [Fact]
    public void Indexer_Get_WithNegativeIndex_ThrowsException()
    {
        var item = new object();
        _proxy.Add(item);
        Assert.Throws<ContractViolationException>(() => _proxy[-1]);
    }

    [Fact]
    public void Indexer_Get_WithIndexOutOfRange_ThrowsException()
    {
        var item = new object();
        _proxy.Add(item);
        Assert.Throws<ContractViolationException>(() => _proxy[1]);
    }

    [Fact]
    public void Insert_WithNegativeIndex_ThrowsException()
    {
        Assert.Throws<ContractViolationException>(() => _proxy.Insert(-1, new object()));
    }

    [Fact]
    public void Insert_WithIndexGreaterThanCount_ThrowsException()
    {
        _proxy.Add(new object());
        Assert.Throws<ContractViolationException>(() => _proxy.Insert(2, new object()));
    }

    [Fact]
    public void RemoveAt_WithNegativeIndex_ThrowsException()
    {
        _proxy.Add(new object());
        Assert.Throws<ContractViolationException>(() => _proxy.RemoveAt(-1));
    }

    [Fact]
    public void RemoveAt_WithIndexOutOfRange_ThrowsException()
    {
        _proxy.Add(new object());
        Assert.Throws<ContractViolationException>(() => _proxy.RemoveAt(1));
    }


    [Fact]
    public void CopyTo_WithNegativeArrayIndex_ThrowsException()
    {
        _proxy.Add(new object());
        var array = new object[5];
        Assert.Throws<ContractViolationException>(() => _proxy.CopyTo(array, -1));
    }

    [Fact]
    public void CopyTo_WithInsufficientArraySpace_ThrowsException()
    {
        _proxy.Add(new object());
        _proxy.Add(new object());
        var array = new object[2];
        Assert.Throws<ContractViolationException>(() =>
        _proxy.CopyTo(array, 1)
        );
    }
}


[ContractClass(typeof(MyListContracts))]
interface IMyList : IList
{

}

class MyList : ArrayList, IMyList
{
}

[ContractClassFor(typeof(IMyList))]
class MyListContracts
{
    public MyListContracts()
    {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
                static (IMyList x, object a) => Contract.Ensures(Contract.Result<int>() >= 0),
                static (IMyList x, object a) => Contract.Ensures(x.Count == 1 + Contract.OldValue<int>(x.Count)));

        Dbc.Def(static (IMyList x, int index) => x[index],
            static (IMyList x, int index) => Contract.Requires(index >= 0 && index < x.Count));


        Dbc.Def(static (IMyList x) => x.Clear(),
            static (IMyList x) => Contract.Ensures(x.Count == 0));

        Dbc.Def(static (IMyList x, object value) => x.Contains(value),
            static (IMyList x, object value) => Contract.Ensures(Contract.Result<bool>() == (x.IndexOf(value) >= 0)));

        Dbc.Def(static (IMyList x, object value) => x.IndexOf(value),
            static (IMyList x, object value) => Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < x.Count));

        Dbc.Def(static (IMyList x, int index, object value) => x.Insert(index, value),
            static (IMyList x, int index, object value) => Contract.Requires(index >= 0 && index <= x.Count),
            static (IMyList x, int index, object value) => Contract.Ensures(x.Count == 1 + Contract.OldValue<int>(x.Count)));

        Dbc.Def(static (IMyList x, object value) => x.Remove(value),
            static (IMyList x, object value) => Contract.Ensures(x.Count <= Contract.OldValue<int>(x.Count)));

        Dbc.Def(static (IMyList x, int index) => x.RemoveAt(index),
            static (IMyList x, int index) => Contract.Requires(index >= 0 && index < x.Count),
            static (IMyList x, int index) => Contract.Ensures(x.Count == Contract.OldValue<int>(x.Count) - 1));

        Dbc.Def(static (IMyList x, Array array, int arrayIndex) => x.CopyTo(array, arrayIndex),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex >= 0),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex + x.Count <= array.Length));


    }
}
