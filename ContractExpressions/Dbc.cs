using System.Linq.Expressions;

public static class Dbc
{
    public static void Def<TIntf, TPar1>(Expression<Action<TIntf, TPar1>> methodExpr, params Expression<Action<TIntf, TPar1>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TRet>(Expression<Func<TIntf, TPar1, TRet>> methodExpr, params Expression<Action<TIntf, TPar1>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);

    }

    private static void RegisterContracts<TIntf>(Expression methodExpr, IEnumerable<Expression> contractDefExprs) where TIntf : class
    {
        var selVisitor = new MethodSelectVisitor();
        selVisitor.Visit(methodExpr);

        var method = selVisitor.Method;

        var contracts = new ContractDelegates();

        foreach (var def in contractDefExprs)
        {
            var visitor = new DbcDefVisitor(typeof(TIntf));
            visitor.Visit(def);

            foreach (var p in visitor.Preconditions)
                contracts.Preconditions.AddItem(method, p);

            foreach (var p in visitor.Postconditions)
                contracts.Postconditions.AddItem(method, p);


            foreach (var (k, v) in visitor.OldValueCollectors)
            {
                contracts.OldValueCollectors.Add(k, v);
            }
        }

        ContractRegistry.Add(typeof(TIntf), contracts);
    }

    public static T Make<T>(T target) where T : class
    {
        if (!typeof(T).IsInterface) throw new ArgumentException($"Type {typeof(T)} must be an interface");

        var proxy = ContractAwareProxy<T>.Make(target);
        return proxy;
    }
}
