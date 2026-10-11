#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Compatibility.MyList;

/// <summary>
/// Ported from ContractExpressions.Tests/MyListContractTests.cs and ContractExpressions4.Tests/MyListContractTests.cs
/// (identical scenarios). Contracts on <c>IList</c> members, defined through the derived <c>IMyList</c>.
/// Change: violations assert <c>ContractViolationException</c> with <c>Kind == Precondition</c> instead of the BCL's
/// internal <c>ContractException</c> (resolution R1).
/// </summary>
public class MyListContractTests
{
    private readonly IMyList proxy = Dbc.Make<IMyList>(new MyList());

    [Fact]
    public void Add() => Assert.Equal(0, proxy.Add(new object()));

    [Fact]
    public void Clear()
    {
        proxy.Add(new object());
        proxy.Add(new object());

        proxy.Clear();

        Assert.Empty(proxy);
    }

    [Fact]
    public void Contains_when_item_exists_returns_true()
    {
        var item = new object();
        proxy.Add(item);

        Assert.True(proxy.Contains(item));
    }

    [Fact]
    public void Contains_when_item_does_not_exist_returns_false() => Assert.False(proxy.Contains(new object()));

    [Fact]
    public void IndexOf_when_item_exists_returns_its_index()
    {
        var item = new object();
        proxy.Add(new object());
        proxy.Add(item);

        Assert.Equal(1, proxy.IndexOf(item));
    }

    [Fact]
    public void IndexOf_when_item_does_not_exist_returns_minus_one() => Assert.Equal(-1, proxy.IndexOf(new object()));

    [Fact]
    public void Insert()
    {
        proxy.Add(new object());
        var item = new object();

        proxy.Insert(0, item);

        Assert.Equal(2, proxy.Count);
        Assert.Equal(item, proxy[0]);
    }

    [Fact]
    public void Remove_when_item_exists()
    {
        var item = new object();
        proxy.Add(item);

        proxy.Remove(item);

        Assert.Empty(proxy);
    }

    [Fact]
    public void RemoveAt()
    {
        proxy.Add(new object());

        proxy.RemoveAt(0);

        Assert.Empty(proxy);
    }

    [Fact]
    public void Indexer_get()
    {
        var item = new object();
        proxy.Add(item);

        Assert.Equal(item, proxy[0]);
    }

    [Fact]
    public void CopyTo()
    {
        var item1 = new object();
        var item2 = new object();
        proxy.Add(item1);
        proxy.Add(item2);
        var array = new object[5];

        proxy.CopyTo(array, 1);

        Assert.Equal(item1, array[1]);
        Assert.Equal(item2, array[2]);
    }

    [Fact]
    public void Indexer_get_with_negative_index_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy[-1]);
    }

    [Fact]
    public void Indexer_get_with_index_out_of_range_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy[1]);
    }

    [Fact]
    public void Insert_with_negative_index_violates_precondition() =>
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Insert(-1, new object()));

    [Fact]
    public void Insert_with_index_greater_than_count_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Insert(2, new object()));
    }

    [Fact]
    public void RemoveAt_with_negative_index_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.RemoveAt(-1));
    }

    [Fact]
    public void RemoveAt_with_index_out_of_range_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.RemoveAt(1));
    }

    [Fact]
    public void CopyTo_with_negative_array_index_violates_precondition()
    {
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.CopyTo(new object[5], -1));
    }

    [Fact]
    public void CopyTo_with_insufficient_array_space_violates_precondition()
    {
        proxy.Add(new object());
        proxy.Add(new object());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.CopyTo(new object[2], 1));
    }

    [Fact]
    public void Broken_Add_violates_its_postcondition()
    {
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IMyList>(new ForgetfulList()).Add(new object()));
    }
}

[ContractClass(typeof(MyListContracts))]
public interface IMyList : IList
{
}

public class MyList : ArrayList, IMyList
{
}

public sealed class ForgetfulList : MyList
{
    public override int Add(object? value) => 0;
}

[ContractClassFor(typeof(IMyList))]
internal sealed class MyListContracts
{
    public MyListContracts()
    {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
            static (IMyList x, object a) => Contract.Ensures(Contract.Result<int>() >= 0),
            static (IMyList x, object a) => Contract.Ensures(x.Count == 1 + Contract.OldValue(x.Count)));

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
            static (IMyList x, int index, object value) => Contract.Ensures(x.Count == 1 + Contract.OldValue(x.Count)));

        Dbc.Def(static (IMyList x, object value) => x.Remove(value),
            static (IMyList x, object value) => Contract.Ensures(x.Count <= Contract.OldValue(x.Count)));

        Dbc.Def(static (IMyList x, int index) => x.RemoveAt(index),
            static (IMyList x, int index) => Contract.Requires(index >= 0 && index < x.Count),
            static (IMyList x, int index) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) - 1));

        Dbc.Def(static (IMyList x, Array array, int arrayIndex) => x.CopyTo(array, arrayIndex),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex >= 0),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex + x.Count <= array.Length));
    }
}
