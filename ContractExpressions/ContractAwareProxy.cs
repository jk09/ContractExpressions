using System.Diagnostics.Contracts;
using System.Reflection;

public sealed class ContractViolationException : Exception
{
    public ContractFailureKind ContractFailureKind { get; init; }

    public ContractViolationException(ContractFailureKind kind, string? message = null) : base(message)
    {
        ContractFailureKind = kind;
    }
}

internal class ContractAwareProxy<TIntf> : DispatchProxy where TIntf : class
{
    private TIntf _target = null!;
    private static readonly ContractDelegates _contracts;

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
        else
        {
            _contracts = ContractDelegates.Empty;
        }
    }


    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

        var ctx = new ContractContext();

        if (_contracts.Preconditions.TryGetValue(targetMethod, out var preconditions))
        {
            foreach (var p in preconditions)
            {
                var preconditionArgs = new List<object?>
                {
                    _target
                };

                if (args != null)
                {
                    preconditionArgs.AddRange(args);
                }

                p.DynamicInvoke(preconditionArgs.ToArray());

            }

        }

        var oldValuesCollectors = _contracts.OldValueCollectors;
        var oldValues = new Dictionary<MemberInfo, object?>();

        foreach (var (property, propertyCollector) in oldValuesCollectors)
        {
            var oldValue = propertyCollector.DynamicInvoke(_target);
            oldValues.Add(property, oldValue);
        }

        ctx.OldValues = oldValues;
        object? result;


        try
        {
            result = targetMethod.Invoke(_target, args);

            ctx.Result = result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }

        var postconditions = _contracts.Postconditions[targetMethod];
        foreach (var p in postconditions)
        {
            var postconditionArgs = new List<object?>
            {
                _target
            };
            if (args != null)
            {
                postconditionArgs.AddRange(args);
            }
            postconditionArgs.Add(ctx);

            p.DynamicInvoke(postconditionArgs.ToArray());
        }

        return result;
    }

    public static TIntf Make(TIntf target)
    {
        object proxy = Create<TIntf, ContractAwareProxy<TIntf>>();
        var dispatcher = (ContractAwareProxy<TIntf>)proxy;
        dispatcher._target = target;

        return (TIntf)proxy;
    }
}
