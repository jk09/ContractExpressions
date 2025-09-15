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

        return base.VisitMethodCall(node);
    }


}
