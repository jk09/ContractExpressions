#define CONTRACTS_FULL

var listc = new ListContracts() as IList;

Dbc.Def(static (IList x, object a) => x.Add(a),
        static (IList x, object a) => Contract.Requires(a is string ? !string.IsNullOrEmpty(a as string) : a != null),
        static (IList x, object a) => Contract.Ensures(Contract.Result<int>() > 0 && x.Count > Contract.OldValue<int>(x.Count)));

class ListContracts : IList
{
    object? IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    bool IList.IsFixedSize => throw new NotImplementedException();

    bool IList.IsReadOnly => throw new NotImplementedException();

    int ICollection.Count => throw new NotImplementedException();

    bool ICollection.IsSynchronized => throw new NotImplementedException();

    object ICollection.SyncRoot => throw new NotImplementedException();

    int IList.Add(object? value1)
    {
        Contract.Requires(value1 != null);
        Contract.Ensures(Contract.Result<int>() > 0);

        return 0;
    }

    void IList.Clear()
    {
        throw new NotImplementedException();
    }

    bool IList.Contains(object? value)
    {
        throw new NotImplementedException();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    int IList.IndexOf(object? value)
    {
        throw new NotImplementedException();
    }

    void IList.Insert(int index, object? value)
    {
        throw new NotImplementedException();
    }

    void IList.Remove(object? value)
    {
        throw new NotImplementedException();
    }

    void IList.RemoveAt(int index)
    {
        throw new NotImplementedException();
    }
}

static class Dbc
{
    public static void Def<TContract, TPar1, TRet>(Expression<Func<TContract, TPar1, TRet>> method, params Expression<Action<TContract, TPar1>>[] contracts)
    {
        foreach (var contract in contracts)
        {
            var visitor = new DbcDefVisitor(typeof(TContract));
            visitor.Visit(contract);

        }
    }
}

class ContractResultVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Result")
        {
            node.Dump("result");
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
                var contract = Expression.Lambda(contractBody, $"Ensures_1", _contractParameters);
                var postconditionDlg = contract.Compile();

                Postconditions.Add(postconditionDlg);

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


            }
        }

        OldValueCollectors.Dump("old value collectors");
        return base.VisitMethodCall(node);
    }


}
