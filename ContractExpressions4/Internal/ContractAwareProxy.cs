using System.Reflection;

namespace ContractExpressions4.Internal;

internal class ContractAwareProxy<T> : DispatchProxy
    where T : class
{
    private T? target;
    private InterfaceContracts? contracts;

    internal void Initialize(T targetInstance, InterfaceContracts interfaceContracts)
    {
        target = targetInstance;
        contracts = interfaceContracts;
    }

    internal void ValidateInvariantsAfterCreation()
    {
        if (target is null || contracts is null)
        {
            throw new InvalidOperationException("Proxy is not initialized.");
        }

        ContractInvocationContext context = new(target, []);
        ContractEvaluator.ValidateAll(context, contracts.Invariants, "<creation>");
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (target is null || contracts is null)
        {
            throw new InvalidOperationException("Proxy is not initialized.");
        }

        if (targetMethod is null)
        {
            throw new InvalidOperationException("Target method is null.");
        }

        object?[] invocationArgs = args ?? [];
        MethodContracts methodContracts = contracts.GetMethodContracts(targetMethod);
        ContractInvocationContext context = new(target, invocationArgs);

        ContractEvaluator.ValidateAll(context, contracts.Invariants, targetMethod.Name);
        ContractEvaluator.ValidateAll(context, methodContracts.Preconditions, targetMethod.Name);

        foreach (CompiledContract postcondition in methodContracts.Postconditions)
        {
            foreach (OldValueCapture capture in postcondition.OldValueCaptures)
            {
                context.SetOldValue(postcondition.Token, capture.Slot, capture.Reader(context));
            }
        }

        object? result;

        try
        {
            result = targetMethod.Invoke(target, invocationArgs);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        context.Result = result;
        ContractEvaluator.ValidateAll(context, contracts.Invariants, targetMethod.Name);
        ContractEvaluator.ValidateAll(context, methodContracts.Postconditions, targetMethod.Name);

        return result;
    }
}
