# ContractExpressions

## Description

ContractExpressions is a lightweight re-implementation of the design-by-contract functionality implemented in the .NET Framework and discontinued in the .NET Core.

It uses the original API of the `System.Diagnostics.Contracts` namespace, with a slightly different semantics.

Unlike a build-time code rewrite of the .NET Framework implementation the contracts are dynamically parsed and evaluated during the runtime. 
The contracts can only be defined per interface methods rather than per the concrete methods. This is an acceptable limitation, as contracts defined per concrete
methods have few advantages over exceptions.

## Usage

```csharp

#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;

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


var proxy = Dbc.Make<IMyList>(new MyList());

```



