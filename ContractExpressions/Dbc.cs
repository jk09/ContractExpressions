using System.Linq.Expressions;

namespace ContractExpressions;

public static class Dbc
{
    public static void Def<TIntf>(Expression<Action<TIntf>> methodExpr, params Expression<Action<TIntf>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TRet>(Expression<Func<TIntf, TRet>> methodExpr, params Expression<Action<TIntf>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2>(Expression<Action<TIntf, TPar1, TPar2>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TRet>(Expression<Func<TIntf, TPar1, TPar2, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3>(Expression<Action<TIntf, TPar1, TPar2, TPar3>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>>[] contractDefExprs) where TIntf : class
    {
        RegisterContracts<TIntf>(methodExpr, contractDefExprs);
    }
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

        foreach (var expr in contractDefExprs)
        {
            var visitor = new DbcDefVisitor(typeof(TIntf));
            visitor.Visit(expr);

            foreach (var p in visitor.Preconditions)
            {
                ContractRegistry.AddPreconditions(typeof(TIntf), method, new List<Invokable> { new Invokable { Expression = expr, Delegate = p } });
            }

            foreach (var p in visitor.Postconditions)
            {
                ContractRegistry.AddPostconditions(typeof(TIntf), method, new List<Invokable> { new Invokable { Expression = expr, Delegate = p } });
            }

            foreach (var (k, v) in visitor.OldValueCollectors)
            {
                ContractRegistry.AddOldValueCollector(typeof(TIntf), k, v);
            }
        }
    }

    public static T Make<T>(T target) where T : class
    {
        if (!typeof(T).IsInterface) throw new ArgumentException($"Type {typeof(T)} must be an interface");

        var proxy = ContractAwareProxy<T>.Make(target);
        return proxy;
    }
}
