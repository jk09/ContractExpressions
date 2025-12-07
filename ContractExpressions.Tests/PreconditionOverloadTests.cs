#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests;

public class PreconditionOverloadTests
{

    [Fact]
    public void Add_NullItem_ThrowsException()
    {
        var proxy = Dbc.Make<IPreconditionOverloadClass1>(new PreconditionOverloadClass());
        var ex = Assert.ThrowsAny<Exception>(() => proxy.DoSomething(null!));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Add_NullItem_ThrowsArgumentNullException()
    {
        var proxy = Dbc.Make<IPreconditionOverloadClass2>(new PreconditionOverloadClass());
        var ex = Assert.Throws<ArgumentNullException>(() => proxy.DoSomething(null!));
    }

    [Fact]
    public void Add_NullItem_ThrowsExceptionWithMessage()
    {
        var proxy = Dbc.Make<IPreconditionOverloadClass3>(new PreconditionOverloadClass());
        var ex = Assert.ThrowsAny<Exception>(() => proxy.DoSomething(null!));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
        Assert.Contains("Object cannot be null", ex.Message);
    }
    [Fact]
    public void Add_NullItem_ThrowsArgumentNullExceptionWithMessage()
    {
        var proxy = Dbc.Make<IPreconditionOverloadClass4>(new PreconditionOverloadClass());
        var ex = Assert.Throws<ArgumentNullException>(() => proxy.DoSomething(null!));
        Assert.Contains("Object cannot be null", ex.Message);
    }


}

[ContractClass(typeof(PreconditionOverloadContracts1))]
interface IPreconditionOverloadClass1
{
    void DoSomething(object obj);
}

[ContractClass(typeof(PreconditionOverloadContracts2))]
interface IPreconditionOverloadClass2
{
    void DoSomething(object obj);
}
[ContractClass(typeof(PreconditionOverloadContracts3))]
interface IPreconditionOverloadClass3
{
    void DoSomething(object obj);
}
[ContractClass(typeof(PreconditionOverloadContracts4))]
interface IPreconditionOverloadClass4
{
    void DoSomething(object obj);
}

[ContractClassFor(typeof(IPreconditionOverloadClass1))]
class PreconditionOverloadContracts1
{
    public PreconditionOverloadContracts1()
    {
        Dbc.Def(static (IPreconditionOverloadClass1 x, object obj) => x.DoSomething(obj),
                static (IPreconditionOverloadClass1 x, object obj) => Contract.Requires(obj != null));
    }
}

[ContractClassFor(typeof(IPreconditionOverloadClass2))]
class PreconditionOverloadContracts2
{
    public PreconditionOverloadContracts2()
    {
        Dbc.Def(static (IPreconditionOverloadClass2 x, object obj) => x.DoSomething(obj),
                static (IPreconditionOverloadClass2 x, object obj) => Contract.Requires<ArgumentNullException>(obj != null));
    }
}
[ContractClassFor(typeof(IPreconditionOverloadClass3))]
class PreconditionOverloadContracts3
{
    public PreconditionOverloadContracts3()
    {
        Dbc.Def(static (IPreconditionOverloadClass3 x, object obj) => x.DoSomething(obj),
                static (IPreconditionOverloadClass3 x, object obj) => Contract.Requires(obj != null, "Object cannot be null"));
    }
}
[ContractClassFor(typeof(IPreconditionOverloadClass4))]
class PreconditionOverloadContracts4
{
    public PreconditionOverloadContracts4()
    {
        Dbc.Def(static (IPreconditionOverloadClass4 x, object obj) => x.DoSomething(obj),
                static (IPreconditionOverloadClass4 x, object obj) => Contract.Requires<ArgumentNullException>(obj != null, "Object cannot be null"));
    }
}

class PreconditionOverloadClass : IPreconditionOverloadClass1, IPreconditionOverloadClass2, IPreconditionOverloadClass3, IPreconditionOverloadClass4
{
    public void DoSomething(object obj)
    {
    }
}


