#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests;

public class PostconditionOverloadTests
{

    [Fact]
    public void GetValue_ReturnsNull_ThrowsException()
    {
        var proxy = Dbc.Make<IPostconditionOverloadClass1>(new PostconditionOverloadClass());
        Assert.Throws<ContractViolationException>(() => proxy.GetValue());
    }


    [Fact]
    public void GetValue_ReturnsNull_ThrowsExceptionWithMessage()
    {
        var proxy = Dbc.Make<IPostconditionOverloadClass3>(new PostconditionOverloadClass());
        var ex = Assert.Throws<ContractViolationException>(() => proxy.GetValue());
        Assert.Equal("Result cannot be null", ex.Message);
    }


}

[ContractClass(typeof(PostconditionOverloadContracts1))]
interface IPostconditionOverloadClass1
{
    object GetValue();
}

[ContractClass(typeof(PostconditionOverloadContracts3))]
interface IPostconditionOverloadClass3
{
    object GetValue();
}



[ContractClassFor(typeof(IPostconditionOverloadClass1))]
class PostconditionOverloadContracts1
{
    public PostconditionOverloadContracts1()
    {
        Dbc.Def(static (IPostconditionOverloadClass1 x) => x.GetValue(),
                static (IPostconditionOverloadClass1 x) => Contract.Ensures(Contract.Result<object>() != null));
    }
}


[ContractClassFor(typeof(IPostconditionOverloadClass3))]
class PostconditionOverloadContracts3
{
    public PostconditionOverloadContracts3()
    {
        Dbc.Def(static (IPostconditionOverloadClass3 x) => x.GetValue(),
                static (IPostconditionOverloadClass3 x) => Contract.Ensures(Contract.Result<object>() != null, "Result cannot be null"));
    }
}

class PostconditionOverloadClass : IPostconditionOverloadClass1, IPostconditionOverloadClass3
{
    public object GetValue()
    {
        return null!;
    }
}
