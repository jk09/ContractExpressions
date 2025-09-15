#define CONTRACTS_FULL


using System.Diagnostics.Contracts;

var proxy = Dbc.Make<IMyList>(new MyList());
proxy.Add("");

[ContractClass(typeof(ListContracts))]
interface IMyList : IList<object>
{
    int Add(object x);
}

class MyList : List<object>, IMyList
{
    int IMyList.Add(object x)
    {
        return (this as IList).Add(x);
    }
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
