namespace MyTool.Agent;

public sealed class ContractInferenceEngine
{
    public DomainSpec Enrich(DomainSpec spec)
    {
        var methods = new List<MethodSpec>();
        foreach (MethodSpec method in spec.Methods)
        {
            var merged = method.Contracts.ToList();

            if (method.BodyExpression.Contains("/", StringComparison.Ordinal) && method.Parameters.Count > 1)
            {
                string divisorName = method.Parameters[1].Name;
                string inferred = $"{divisorName} != 0";
                if (!merged.Any(contract => contract.Predicate.Equals(inferred, StringComparison.OrdinalIgnoreCase)))
                {
                    merged.Add(new ContractSpec("Precondition", inferred, false));
                }
            }

            methods.Add(method with { Contracts = merged });
        }

        return spec with { Methods = methods };
    }

    public IReadOnlyList<string> Verify(DomainSpec spec)
    {
        var warnings = new List<string>();
        foreach (MethodSpec method in spec.Methods)
        {
            if (!method.Contracts.Any())
            {
                warnings.Add($"Method '{method.Name}' has no inferred or explicit contracts.");
            }

            if (!method.Contracts.Any(contract => contract.Kind == "Precondition"))
            {
                warnings.Add($"Method '{method.Name}' has no precondition contracts.");
            }
        }

        return warnings;
    }
}
