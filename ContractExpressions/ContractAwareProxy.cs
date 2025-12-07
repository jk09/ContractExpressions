using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractExpressions;



internal class ContractAwareProxy<TIntf> : DispatchProxy where TIntf : class
{
    private TIntf _target = null!;
    private static readonly ContractRegistry _contractRegistry = ContractRegistry.Instance;

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
        Contract.ContractFailed += (sender, e) =>
        {
            e.SetUnwind();
        };
        try
        {
            contractInvokable.Delegate.DynamicInvoke(args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException?.GetType().FullName == "System.Diagnostics.Contracts.ContractException")
        {
            var contractException = ex.InnerException!;

            AddContractExceptionData(contractException);
            throw contractException;
        }
        catch (TargetInvocationException ex)
        {
            var innerException = ex.InnerException ?? ex;
            AddContractExceptionData(innerException);
            throw innerException;
        }

        void AddContractExceptionData(Exception exception)
        {
            exception.Data[typeof(ContractExceptionData)] = new ContractExceptionData(targetMethod, args, contractInvokable.Expression);
        }
    }

    private void CollectOldValues(MethodInfo targetMethod, object?[]? args, ContractContext context)
    {
        if (_contractRegistry.OldValueCollectors.ContainsKey(targetMethod))
        {
            var oldValuesCollectors = _contractRegistry.OldValueCollectors[targetMethod];
            foreach (var oldValueCollector in oldValuesCollectors)
            {
                var oldValue = oldValueCollector.Value.DynamicInvoke(_target);
                context.OldValues ??= new Dictionary<MemberInfo, object?>();
                context.OldValues.Add(oldValueCollector.Key, oldValue);
            }
        }
    }

    private void EvaluatePreconditions(MethodInfo targetMethod, object?[]? args)
    {
        if (_contractRegistry.Preconditions.TryGetValue(targetMethod, out var preconditions))
        {
            foreach (var contract in preconditions)
            {
                var preconditionArgs = new List<object?>
                {
                    _target
                };

                if (args != null)
                {
                    preconditionArgs.AddRange(args);
                }

                ContractAwareProxy<TIntf>.InvokeContract(contract, preconditionArgs.ToArray(), targetMethod);
            }
        }
    }

    private void EvaluatePostconditions(MethodInfo targetMethod, object?[]? args, ContractContext context)
    {
        if (_contractRegistry.Postconditions.TryGetValue(targetMethod, out var postconditions))
        {
            foreach (var contract in postconditions)
            {
                var postconditionArgs = new List<object?>
                 {
                    _target
                };
                if (args != null)
                {
                    postconditionArgs.AddRange(args);
                }
                postconditionArgs.Add(context);

                ContractAwareProxy<TIntf>.InvokeContract(contract, postconditionArgs.ToArray(), targetMethod);
            }
        }
    }

    private void EvaluatePostconditionsOnThrow(MethodInfo targetMethod, object?[]? args, Exception exception)
    {
        if (_contractRegistry.PostconditionsOnThrow.TryGetValue(targetMethod, out var postconditionsOnThrow))
        {
            foreach (var contract in postconditionsOnThrow)
            {
                var postconditionArgs = new List<object?>
                 {
                    _target
                };
                if (args != null)
                {
                    postconditionArgs.AddRange(args);
                }
                postconditionArgs.Add(exception);

                ContractAwareProxy<TIntf>.InvokeContract(contract, postconditionArgs.ToArray(), targetMethod);
            }

        }
    }

    private void EvaluateInvariants(MethodInfo targetMethod, object?[]? args, ContractContext context)
    {
        if (_contractRegistry.Invariants.TryGetValue(targetMethod, out var invariants))
        {
            foreach (var contract in invariants)
            {
                var invariantArgs = new List<object?>
                 {
                    _target
                };
                if (args != null)
                {
                    invariantArgs.AddRange(args);
                }
                invariantArgs.Add(context);

                ContractAwareProxy<TIntf>.InvokeContract(contract, invariantArgs.ToArray(), targetMethod);
            }

        }
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

        var context = new ContractContext();

        EvaluatePreconditions(targetMethod, args);

        CollectOldValues(targetMethod, args, context);

        object? result = null;
        try
        {
            result = targetMethod.Invoke(_target, args);
            context.Result = result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            EvaluatePostconditionsOnThrow(targetMethod, args, ex.InnerException);
            throw ex.InnerException;
        }

        EvaluateInvariants(targetMethod, args, context);

        EvaluatePostconditions(targetMethod, args, context);

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
