using System.Text;

namespace MyTool.Agent;

public sealed class CSharpContractTemplateGenerator
{
    public IReadOnlyList<GeneratedArtifact> Generate(DomainSpec spec, string outputDirectory)
    {
        string interfaceName = $"I{spec.ClassName}Contracts";
        MethodSpec method = spec.Methods[0];
        string parameterSignature = string.Join(", ", method.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
        string argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));
        string selectorParameters = string.Join(", ", method.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
        string contractClauses = string.Join(",\n            ", method.Contracts
            .Where(c => c.Kind == "Precondition")
            .Select(c => $"static ({interfaceName} target, {selectorParameters}) => Contract.Requires({c.Predicate})"));

        var artifacts = new List<GeneratedArtifact>();

        if (!spec.ClassAlreadyExists)
        {
            artifacts.Add(new GeneratedArtifact(
                RelativePath: Path.Combine(outputDirectory, $"{spec.ClassName}.cs"),
                Content: BuildClassFile(spec, interfaceName, method, parameterSignature, argumentList)));
        }

        artifacts.Add(new GeneratedArtifact(
            RelativePath: Path.Combine(outputDirectory, $"{spec.ClassName}.Contracts.cs"),
            Content: BuildContractsFile(spec, interfaceName, method, parameterSignature, selectorParameters, argumentList, contractClauses)));

        artifacts.Add(new GeneratedArtifact(
            RelativePath: Path.Combine(outputDirectory, $"{spec.ClassName}.Tests.cs"),
            Content: BuildTestsFile(spec, interfaceName, method)));

        return artifacts;
    }

    private static string BuildClassFile(DomainSpec spec, string interfaceName, MethodSpec method, string parameterSignature, string argumentList)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {spec.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {spec.ClassName} : {interfaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public {method.ReturnType} {method.Name}({parameterSignature}) => {method.BodyExpression};");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildContractsFile(DomainSpec spec, string interfaceName, MethodSpec method, string parameterSignature, string selectorParameters, string argumentList, string contractClauses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#define CONTRACTS_FULL");
        sb.AppendLine();
        sb.AppendLine("using System.Diagnostics.Contracts;");
        sb.AppendLine("using ContractExpressions4;");
        sb.AppendLine();
        sb.AppendLine($"namespace {spec.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"[ContractClass(typeof({spec.ClassName}Contracts))]");
        sb.AppendLine($"public interface {interfaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    {method.ReturnType} {method.Name}({parameterSignature});");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"[ContractClassFor(typeof({interfaceName}))]");
        sb.AppendLine($"public class {spec.ClassName}Contracts");
        sb.AppendLine("{");
        sb.AppendLine($"    public {spec.ClassName}Contracts()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Dbc.Def(static ({interfaceName} target, {selectorParameters}) => target.{method.Name}({argumentList}),");
        sb.AppendLine($"            {contractClauses});");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildTestsFile(DomainSpec spec, string interfaceName, MethodSpec method)
    {
        string divisorName = method.Parameters.Count > 1 ? method.Parameters[1].Name : method.Parameters[0].Name;

        var sb = new StringBuilder();
        sb.AppendLine("#define CONTRACTS_FULL");
        sb.AppendLine();
        sb.AppendLine("using ContractExpressions4;");
        sb.AppendLine("using ContractExpressions4.Check;");
        sb.AppendLine("using FsCheck.Fluent;");
        sb.AppendLine();
        sb.AppendLine($"namespace {spec.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {spec.ClassName}ContractTests");
        sb.AppendLine("{");
        sb.AppendLine("    [Fact]");
        sb.AppendLine($"    public void {method.Name}_WithValidInput_ReturnsExpectedValue()");
        sb.AppendLine("    {");
        sb.AppendLine($"        {interfaceName} proxy = Dbc.Make<{interfaceName}>(new {spec.ClassName}());");
        sb.AppendLine();
        sb.AppendLine($"        int result = proxy.{method.Name}(8, 2);");
        sb.AppendLine();
        sb.AppendLine("        Assert.Equal(4, result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine($"    public void {method.Name}_WhenPreconditionFails_ThrowsPreconditionViolation()");
        sb.AppendLine("    {");
        sb.AppendLine($"        {interfaceName} proxy = Dbc.Make<{interfaceName}>(new {spec.ClassName}());");
        sb.AppendLine();
        sb.AppendLine($"        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.{method.Name}(8, 0));");
        sb.AppendLine();
        sb.AppendLine("        Assert.Equal(ContractKind.Precondition, ex.Kind);");
        sb.AppendLine($"        Assert.Equal(\"{method.Name}\", ex.Method);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Property(Verbose = true)]");
        sb.AppendLine($"    public Property {method.Name}_FsCheck_HoldsForValidInputs(int a, int {divisorName}) =>");
        sb.AppendLine($"        Prop.Implies({divisorName} != 0,");
        sb.AppendLine("            DbcPropertyTest.Check(");
        sb.AppendLine($"                () => Dbc.Make<{interfaceName}>(new {spec.ClassName}()),");
        sb.AppendLine($"                ({interfaceName} proxy) => proxy.{method.Name}(a, {divisorName})));");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
