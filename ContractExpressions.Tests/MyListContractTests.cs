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
        _proxy.Add("");

    }
}


[ContractClass(typeof(ListContracts))]
interface IMyList : IList<object>
{

}

class MyList : List<object>, IMyList
{
}

[ContractClassFor(typeof(IMyList))]
class ListContracts
{
    public ListContracts()
    {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
                static (IMyList x, object a) => Contract.Requires(a is string ? !string.IsNullOrEmpty(a as string) : a != null),
                static (IMyList x, object a) => Contract.Ensures(Contract.Result<int>() >= 0 && x.Count > Contract.OldValue<int>(x.Count)));

    }
}
