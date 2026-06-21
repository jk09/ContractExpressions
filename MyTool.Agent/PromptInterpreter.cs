using System.Text.RegularExpressions;

namespace MyTool.Agent;

public sealed class PromptInterpreter
{
    private static readonly Regex ExplicitPreconditionRegex = new(@"precondition\s*:\s*(?<value>[^\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public DomainSpec Parse(string prompt, string workspaceRoot, bool augmentExisting)
    {
        string normalized = prompt.Trim();
        string lower = normalized.ToLowerInvariant();

        string className = InferClassName(normalized, lower);
        MethodSpec method = InferPrimaryMethod(lower);
        IReadOnlyList<ContractSpec> explicitContracts = ExtractExplicitContracts(prompt);

        MethodSpec enrichedMethod = method with
        {
            Contracts = method.Contracts.Concat(explicitContracts).DistinctBy(c => (c.Kind, c.Predicate)).ToList()
        };

        string? existingClassFile = augmentExisting
            ? WorkspaceClassLocator.FindClassFile(workspaceRoot, className)
            : null;

        return new DomainSpec(
            Namespace: "GeneratedContracts",
            ClassName: className,
            Prompt: prompt,
            Methods: [enrichedMethod],
            ClassAlreadyExists: existingClassFile is not null,
            ExistingClassPath: existingClassFile);
    }

    private static string InferClassName(string prompt, string lower)
    {
        if (lower.Contains("division"))
        {
            return "Division";
        }

        Match classMatch = Regex.Match(prompt, @"class\s+(?<name>[A-Z][A-Za-z0-9_]*)");
        if (classMatch.Success)
        {
            return classMatch.Groups["name"].Value;
        }

        return "GeneratedProgram";
    }

    private static MethodSpec InferPrimaryMethod(string lower)
    {
        if (lower.Contains("division") || lower.Contains("divide"))
        {
            return new MethodSpec(
                Name: "Divide",
                ReturnType: "int",
                BodyExpression: "a / b",
                Parameters: [new ParameterSpec("a", "int"), new ParameterSpec("b", "int")],
                Contracts: []);
        }

        return new MethodSpec(
            Name: "Execute",
            ReturnType: "int",
            BodyExpression: "0",
            Parameters: [new ParameterSpec("value", "int")],
            Contracts: []);
    }

    private static IReadOnlyList<ContractSpec> ExtractExplicitContracts(string prompt)
    {
        MatchCollection matches = ExplicitPreconditionRegex.Matches(prompt);
        var contracts = new List<ContractSpec>();

        foreach (Match match in matches)
        {
            string value = match.Groups["value"].Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            contracts.Add(new ContractSpec("Precondition", NormalizePredicate(value), true));
        }

        return contracts;
    }

    private static string NormalizePredicate(string predicate)
    {
        string normalized = predicate.Trim();
        normalized = Regex.Replace(normalized, @"\bthe\s+", string.Empty, RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bdivisor\b", "b", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bthe\s+b\b", "b", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+", " ");

        if (Regex.IsMatch(normalized, @"^b\s*!=\s*0$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(normalized, @"^b\s*<>\s*0$", RegexOptions.IgnoreCase))
        {
            return "b != 0";
        }

        return normalized;
    }
}
