using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

public static class Dbc
{
    public static void Def<TIntf>(Expression<Action<TIntf>> methodExpr, params Expression<Action<TIntf>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TRet>(Expression<Func<TIntf, TRet>> methodExpr, params Expression<Action<TIntf>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2>(Expression<Action<TIntf, TPar1, TPar2>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TRet>(Expression<Func<TIntf, TPar1, TPar2, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3>(Expression<Action<TIntf, TPar1, TPar2, TPar3>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>(Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7, TRet>(Expression<Func<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7, TRet>> methodExpr, params Expression<Action<TIntf, TPar1, TPar2, TPar3, TPar4, TPar5, TPar6, TPar7>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }
    public static void Def<TIntf, TPar1>(Expression<Action<TIntf, TPar1>> methodExpr, params Expression<Action<TIntf, TPar1>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);
    }

    public static void Def<TIntf, TPar1, TRet>(Expression<Func<TIntf, TPar1, TRet>> methodExpr, params Expression<Action<TIntf, TPar1>>[] contractDefExprs) where TIntf : class
    {
        AddContractsToRegistry<TIntf>(methodExpr, contractDefExprs);

    }

    private static void AddContractsToRegistry<TIntf>(Expression methodExpr, IEnumerable<Expression> contractDefExprs) where TIntf : class
    {
        var selVisitor = new MethodSelectVisitor();
        selVisitor.Visit(methodExpr);

        var contractRegistry = ContractRegistry.Instance;

        var method = selVisitor.Method;
        if (!contractRegistry.Preconditions.ContainsKey(method))
        {
            contractRegistry.Preconditions[method] = new List<Invokable>();
        }
        if (!contractRegistry.Postconditions.ContainsKey(method))
        {
            contractRegistry.Postconditions[method] = new List<Invokable>();
        }
        if (!contractRegistry.OldValueCollectors.ContainsKey(method))
        {
            contractRegistry.OldValueCollectors[method] = new Dictionary<PropertyInfo, Delegate>();
        }


        foreach (var expr in contractDefExprs)
        {
            var visitor = new DbcDefVisitor(typeof(TIntf));
            visitor.Visit(expr);

            foreach (var precondition in visitor.Preconditions)
            {
                contractRegistry.Preconditions[method].Add(new Invokable(expr, precondition));
            }

            foreach (var postcondition in visitor.Postconditions)
            {
                contractRegistry.Postconditions[method].Add(new Invokable(expr, postcondition));
            }

            if (visitor.OldValueCollectors.Count > 0)
            {
                foreach (var (property, collectorDelegate) in visitor.OldValueCollectors)
                {
                    contractRegistry.OldValueCollectors[method].Add(property, collectorDelegate);
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
