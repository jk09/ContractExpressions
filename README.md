# ContractExpressions

## Description

The `ContractExpressions` project provides the Design by Contract (DbC) functionality in .NET developers. It is a lightweight replacement of the legacy project [Code contracts](https://learn.microsoft.com/en-us/dotnet/framework/debug-trace-profile/code-contracts).

The [source code](../ContractExpressions/CodeContracts/CodeContracts.sln) of the legacy framework is based on the compile-time rewriting of the program using the `System.Diagnostics.Contracts` API.

A `contract` is a predicate about the arguments and return values of object methods, and about the state of the object itself. The predicates are verified before and after a method call. If any such predicate returns false, the contract is `violated`. Violation of a contract indicates an inconsistent state of the program. 

There are three main kinds of the contracts:

- `preconditions`
- `postconditions` 
- `invariants`

### Preconditions

A precondition verifies if the arguments of a methods and the object fields satisfy  certain criteria. The examples of preconditions are:

- the argument is an integer greater than zero
- the argument is a string with length between 10 and 20 characters
- the first argument is an array and the second argument is an integer whose value is within the index bounds of the array
- the argument is an array whose size added to the size of the object buffer is less or equal to the maximum buffer size (both being defined by as the object's fields)

Any method can have attached to itself any number of preconditions. It is the responsibility of the method's callers to supply arguments to the method which satisfy all preconditions.

### Postconditions

A postcondition verifies if the return values of a method (possibly including the `ref` and `out` parameters) satisfy some criteria. The examples of postconditions are:

- the return value is an integer greater than zero
- if the return value of the method is `bool` and `true`, then the `out` parameter is non-`null`
- the new size of an array field is greater by the size of the array argument than its old size

Like preconditions, any method can have any number of postconditions. The method must ensure the validity of all the preconditions before it returns control to its caller.

### Invariants

An invariant verifies the consistent state of an object. It is a predicate about its fields. Examples:

- the size of the bounding box of a graphical widget is not less than its bounding size
- the abstract syntax tree of a Web page DOM is consistent
- adding a new element into a sorted binary tree keeps the tree sorted

Like preconditions and postconditions any object can have any number of invariants attached to it. The invariants are verified 

- after the object is created
- before and after each method call

## Implementation

The code contracts is implemented using the .NET API in the namespace `System.Diagnostics.Contracts` like the legacy framework. However, instead of the compile-time rewriting it utilized the 
.NET `System.Reflection.DispatchProxy` class to define contracts on a per-interface basis using method call interception. 

### API

A contract on an interface  is defined using `Dbc.Def(...)` method in the default constructor of a class decorated with the `System.Diagnostics.Contracts.ContractClassFor` attribute, which in this way is tied to the interface. 
The proxy class which intercepts the method calls on the interface is created using the `Dbc.Make<TInterface>(...)` static method.

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




### Limitations

One  limitation is that a contract cannot be attached to a single method; however, this is considered 
acceptable, since contracts should provide a common safety for multiple objects which implement the same API defined by their interface.

Another limitation is that contracts on `ref` and `out` parameters are not supported, due to the functional nature of the `System.Linq.Expressions` namespace.


