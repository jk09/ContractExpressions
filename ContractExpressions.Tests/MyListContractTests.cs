#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;

namespace ContractExpressions.Tests;

public class MyListContractTests
{
    private readonly IMyList _proxy;
    public MyListContractTests()
    {
        _proxy = Dbc.Make<IMyList>(new MyList());

    }

    [Fact]
    public void Test1()
    {
        var index = _proxy.Add(new object());
        Assert.Equal(0, index);
    }
}


[ContractClass(typeof(ListContracts))]
interface IMyList : IList
{

}

class MyList : ArrayList, IMyList
{
}

[ContractClassFor(typeof(IMyList))]
class ListContracts
{
    public ListContracts()
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
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(array != null),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex >= 0),
            static (IMyList x, Array array, int arrayIndex) => Contract.Requires(arrayIndex + x.Count <= array.Length));


    }
}


