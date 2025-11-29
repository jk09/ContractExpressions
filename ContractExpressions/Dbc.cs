using System.Linq.Expressions;
using System.Reflection;

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

            var contractRegistry = ContractRegistry.Instance;

            foreach (var p in visitor.Preconditions)
            {
                contractRegistry.Preconditions.AddSafe(method, new Invokable(expr, p));
            }

            foreach (var p in visitor.Postconditions)
            {
                contractRegistry.Postconditions.AddSafe(method, new Invokable(expr, p));
            }

            if (visitor.OldValueCollectors.Count > 0)
            {
                if (!contractRegistry.OldValueCollectors.ContainsKey(method))
                {
                    contractRegistry.OldValueCollectors[method] = new Dictionary<PropertyInfo, Delegate>();
                }
                foreach (var kvp in visitor.OldValueCollectors)
                {
                    contractRegistry.OldValueCollectors[method][kvp.Key] = kvp.Value;
                }
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
