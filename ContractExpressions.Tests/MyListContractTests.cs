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
    }
}


