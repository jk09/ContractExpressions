using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

internal class DbcDefVisitor : ExpressionVisitor
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
            if (node.Method.Name == nameof(Contract.Requires))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));
                var exceptionType = node.Method.GetGenericArguments().Length > 0
                    ? Expression.Constant(node.Method.GetGenericArguments()[0], typeof(Type))
                    : Expression.Constant(null, typeof(Type));

                var preconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Requires), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;

                var contract = Expression.Lambda(Expression.Call(null, preconditionPatch, condition, message, exceptionType), $"Requires_1", _contractParameters);

                var contractDlg = contract.Compile();

                Preconditions.Add(contractDlg);
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

                var ensuresExpr = Expression.Call(null, typeof(ContractPatch).GetMethod("Ensures")!, contractBodyPatch2, Expression.Constant(null, typeof(string)));
                var postcondition = Expression.Lambda(ensuresExpr, $"Ensures_1", postconditionParams);
                var postconditionDlg = postcondition.Compile();

                Postconditions.Add(postconditionDlg);

            }
        }

        return base.VisitMethodCall(node);
    }


}
