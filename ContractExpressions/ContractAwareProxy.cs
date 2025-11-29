using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractExpressions;

internal class ContractAwareProxy<TIntf> : DispatchProxy where TIntf : class
{
    private TIntf _target = null!;

    static ContractAwareProxy()
    {
        var contractClassAttr = typeof(TIntf).GetCustomAttribute<ContractClassAttribute>();

        if (contractClassAttr != null)
        {
            var typeContainingContracts = contractClassAttr.TypeContainingContracts;

            var contractClassForAttr = typeContainingContracts.GetCustomAttribute<ContractClassForAttribute>();
            if (contractClassForAttr == null || contractClassForAttr.TypeContractsAreFor != typeof(TIntf))
            {
                throw new InvalidOperationException($"Type '{typeContainingContracts.FullName}' is marked with ContractClassAttribute for '{typeof(TIntf).FullName}', but it does not have ContractClassForAttribute for that interface.");
            }

            CollectContracts(typeContainingContracts);
        }
    }

    private static void CollectContracts(Type type)
    {
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        Debug.Assert(ctor != null, $"Type {type.FullName} must have a default constructor");
        _ = ctor!.Invoke(null);
    }

    private static void InvokeContract(Invokable contractInvokable, object?[] args, MethodInfo targetMethod)
    {
        try
        {
            contractInvokable.Delegate.DynamicInvoke(args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ContractViolationException innerEx)
        {
            innerEx.AddContractData($"'{targetMethod.DeclaringType?.FullName}::{targetMethod.Name}'; {contractInvokable.Expression}");
            throw innerEx;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }


    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

        var ctx = new ContractContext();

        var contractRegistry = ContractRegistry.Instance;


        if (contractRegistry.Preconditions.TryGetValue(targetMethod, out var preconditions))
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

                ContractAwareProxy<TIntf>.InvokeContract(p, preconditionArgs.ToArray(), targetMethod);

            }

        }


        var oldValues = new Dictionary<MemberInfo, object?>();

        if (contractRegistry.OldValueCollectors.ContainsKey(targetMethod))
        {
            var oldValuesCollectors = contractRegistry.OldValueCollectors[targetMethod];
            foreach (var oldValueCollector in oldValuesCollectors)
            {
                var oldValue = oldValueCollector.Value.DynamicInvoke(_target);
                oldValues.Add(oldValueCollector.Key, oldValue);
            }
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


        if (contractRegistry.Postconditions.TryGetValue(targetMethod, out var postconditions))
        {
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

                ContractAwareProxy<TIntf>.InvokeContract(p, postconditionArgs.ToArray(), targetMethod);
            }

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
