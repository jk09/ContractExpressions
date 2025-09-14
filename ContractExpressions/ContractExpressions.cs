#define CONTRACTS_FULL


var proxy = Dbc.Make<IMyList>(new MyList());
proxy.Add("");

[ContractClass(typeof(ListContracts))]
interface IMyList : IList
{

}

class MyList : IMyList
{
    public object? this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool IsFixedSize => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsSynchronized => throw new NotImplementedException();

    public object SyncRoot => throw new NotImplementedException();

    public int Add(object? value)
    {
        return 1;
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(object? value)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(object? value)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, object? value)
    {
        throw new NotImplementedException();
    }

    public void Remove(object? value)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }
}

[ContractClassFor(typeof(IMyList))]
class ListContracts
{
    public ListContracts()
    {
        Dbc.Def(static (IMyList x, object a) => x.Add(a),
                static (IMyList x, object a) => Contract.Requires(a is string ? !string.IsNullOrEmpty(a as string) : a != null),
                static (IMyList x, object a) => Contract.Ensures(Contract.Result<int>() > 0 && x.Count > Contract.OldValue<int>(x.Count)));

    }
}


public static class Dbc
{
    public static void Def<TIntf, TPar1, TRet>(Expression<Func<TIntf, TPar1, TRet>> method, params Expression<Action<TIntf, TPar1>>[] contractDefs)
    {
        var contracts = new Contracts();

        foreach (var def in contractDefs)
        {
            var visitor = new DbcDefVisitor(typeof(TIntf));
            visitor.Visit(def);

            //contracts.Preconditions.Merge(visitor.Preconditions);
            //contracts.Postconditions.Merge(visitor.Postconditions);

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

static class ContractRegistry
{
    private static Dictionary<Type, Contracts> Contracts { get; } = new();

    public static void Add(Type intfType, Contracts contracts)
    {
        Contracts.Add(intfType, contracts);
    }

    public static Contracts Get(Type intfType)
    {
        return Contracts[intfType];
    }
}

class Contracts
{
    public readonly Dictionary<MethodInfo, IList<Delegate>> Preconditions = new();
    public readonly Dictionary<MethodInfo, IList<Delegate>> Postconditions = new();
    public readonly Dictionary<PropertyInfo, Delegate> OldValueCollectors = new();
}

static class TypeExtensions
{
    public static bool IsContractClassFor(this Type cls, Type typeContractsAreFor)
    {
        return cls.GetCustomAttributesData().Any(a => a.AttributeType == typeof(ContractClassForAttribute)
                                                && a.ConstructorArguments[0].ArgumentType == typeContractsAreFor);
    }

}

class ContractAwareProxy<TIntf> : DispatchProxy where TIntf : class
{
    private TIntf _target = null!;
    private static readonly Contracts? _contracts;

    static ContractAwareProxy()
    {
        var contractClassType = typeof(TIntf).GetCustomAttributesData()
            .FirstOrDefault(x => x.AttributeType == typeof(ContractClassAttribute))?.ConstructorArguments[0].Value as Type;


        if (contractClassType != null)
        {
            // run Dbc.Def(...) in ctor
            var contractClass = Activator.CreateInstance(contractClassType);
            _contracts = ContractRegistry.Get(typeof(TIntf));

        }
    }


    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var preconditions = _contracts.Preconditions[targetMethod];

        return null;
    }

    public static TIntf Make(TIntf target)
    {
        object proxy = Create<TIntf, ContractAwareProxy<TIntf>>();
        var dispatcher = (ContractAwareProxy<TIntf>)proxy;
        dispatcher._target = target;

        return (TIntf)proxy;
    }
}

static class ContractPatch
{
    public static T Result<T>(ContractContext context)
    {
        throw new NotImplementedException();
    }

    public static T OldValue<T>(PropertyInfo property, ContractContext context)
    {
        throw new NotImplementedException();

    }
}

class ContractResultPatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractResultPatchVisitor(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Result")
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("Result", 1, new Type[] { typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var e = Expression.Call(null, patchMethod, _contractContextArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}

class ContractOldValuePatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractOldValuePatchVisitor(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "OldValue")
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("OldValue", 1, new Type[] { typeof(PropertyInfo), typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var valueExpr = node.Arguments[0];

            if (valueExpr is MemberExpression oldValueMemberExpr && oldValueMemberExpr.Member is PropertyInfo propertyInfo)
            {
                var e = Expression.Call(null, patchMethod, Expression.Constant(propertyInfo, typeof(PropertyInfo)), _contractContextArg);
                return e;

            }
            else
            {
                throw new NotImplementedException($"Cannot handle old value expression {valueExpr}");
            }
        }

        return base.VisitMethodCall(node);
    }
}

class ContractOldValueVisitor : ExpressionVisitor
{
    public List<PropertyInfo> OldValueProperties { get; } = new();

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "OldValue")
        {
            node.Dump("old value");

            var valueExpr = node.Arguments[0];

            if (valueExpr is MemberExpression oldValueMemberExpr && oldValueMemberExpr.Member is PropertyInfo propertyInfo)
            {
                OldValueProperties.Add(propertyInfo);

            }
            else
            {
                throw new NotImplementedException($"Cannot handle old value expression {valueExpr}");
            }
        }
        return base.VisitMethodCall(node);
    }

}

class ContractContext
{
    public object Result { get; set; }

    public Dictionary<MemberInfo, object> OldValues { get; set; }
}

static class CollectionExtensions
{
    public static void AddItem<TKey, TItemValue>(this IDictionary<TKey, IList<TItemValue>> dict, TKey key, TItemValue item)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, new List<TItemValue>());
        }

        dict[key].Add(item);
    }

    public static void Merge<TKey, TItemValue>(this IDictionary<TKey, IList<TItemValue>> dict, IDictionary<TKey, IList<TItemValue>> dict2)
    {
        foreach (var (k, lst) in dict2)
        {
            foreach (var v in lst)
            {
                dict.AddItem(k, v);
            }
        }
    }


}

class DbcDefVisitor : ExpressionVisitor
{
    private IList<ParameterExpression>? _contractParameters;
    private readonly Type _contractType;

    public readonly List<Delegate> Preconditions = new();
    public readonly List<Delegate> Postconditions = new();
    public readonly Dictionary<PropertyInfo, Delegate> OldValueCollectors = new();

    public DbcDefVisitor(Type contractType)
    {
        _contractType = contractType;
    }

    public override Expression? Visit(Expression? node)
    {
        if (_contractParameters == null && node is LambdaExpression lambda)
        {
            _contractParameters = lambda.Parameters;
            //node.Dump("node");
        }

        return base.Visit(node);

    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract))
        {
            if (node.Method.Name == "Requires")
            {
                var contractBody = node.Arguments[0];

                var contract = Expression.Lambda(contractBody, $"Requires_1", _contractParameters);

                var preconditionDlg = contract.Compile();

                Preconditions.Add(preconditionDlg);
            }
            else if (node.Method.Name == "Ensures")
            {
                var contractBody = node.Arguments[0];

                var oldValueVisitor = new ContractOldValueVisitor();
                oldValueVisitor.Visit(contractBody);

                foreach (var oldValueMember in oldValueVisitor.OldValueProperties)
                {
                    if (!OldValueCollectors.ContainsKey(oldValueMember))
                    {
                        var thisParamExpr = Expression.Parameter(_contractType, "thisContract");
                        var collector = Expression.Lambda(Expression.Property(thisParamExpr, oldValueMember), thisParamExpr);
                        var dlg = collector.Compile();

                        OldValueCollectors.Add(oldValueMember, dlg);
                    }
                }

                var contractContextParam = Expression.Parameter(typeof(ContractContext), "contractContext");

                var resultPatcher = new ContractResultPatchVisitor(contractContextParam);
                var contractBodyPatch1 = resultPatcher.Visit(contractBody);

                var oldValuePatcher = new ContractOldValuePatchVisitor(contractContextParam);
                var contractBodyPatch2 = oldValuePatcher.Visit(contractBodyPatch1);


                var postconditionParams = new List<ParameterExpression>();
                postconditionParams.AddRange(_contractParameters!);
                postconditionParams.Add(contractContextParam);

                var postcondition = Expression.Lambda(contractBodyPatch2, $"Ensures_1", postconditionParams);
                var postconditionDlg = postcondition.Compile();

                Postconditions.Add(postconditionDlg);

            }
        }

        OldValueCollectors.Dump("old value collectors");
        return base.VisitMethodCall(node);
    }


}
