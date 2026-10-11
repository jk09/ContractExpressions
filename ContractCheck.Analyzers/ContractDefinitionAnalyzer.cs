using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ContractCheck.Analyzers;

/// <summary>
/// Checks <c>Dbc.Def</c>/<c>Dbc.DefSetter</c> calls at compile time with the rules <c>Dbc.Make</c> applies at run time
/// (CCK0001–CCK0005), and warns when a derived interface adds preconditions to an inherited member (CCK0008).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractDefinitionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.InvalidSelector,
        Descriptors.InvalidClause,
        Descriptors.ResultMisuse,
        Descriptors.OldValueMisuse,
        Descriptors.ByRefMisuse,
        Descriptors.InheritedPrecondition);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            if (KnownSymbols.Create(start.Compilation) is { } symbols)
            {
                start.RegisterSyntaxNodeAction(c => new DefinitionAnalysis(c, symbols).Run(), SyntaxKind.InvocationExpression);
            }
        });
    }
}

/// <summary>The kind of a clause, from the <c>Contract</c> method it calls.</summary>
internal enum ClauseKind
{
    Requires,
    RequiresOfException,
    Assert,
    Assume,
    Ensures,
    EnsuresOnThrow,
    Invariant,
}

/// <summary>An out/ref placeholder passed by a selector, by its C# text (e.g. <c>Dbc.Out&lt;int&gt;.Value</c>).</summary>
internal sealed class PlaceholderSlot(string parameterName, bool isOut)
{
    public string ParameterName { get; } = parameterName;

    public bool IsOut { get; } = isOut;
}

/// <summary>The member a selector picked.</summary>
internal sealed class SelectedMember(
    ISymbol symbol, INamedTypeSymbol declaringInterface, ITypeSymbol? returnType, ITypeSymbol? awaitedType, Dictionary<string, PlaceholderSlot> placeholders, string display)
{
    public ISymbol Symbol { get; } = symbol;

    public INamedTypeSymbol DeclaringInterface { get; } = declaringInterface;

    /// <summary>Null for <see langword="void"/> members and setters.</summary>
    public ITypeSymbol? ReturnType { get; } = returnType;

    public ITypeSymbol? AwaitedType { get; } = awaitedType;

    public Dictionary<string, PlaceholderSlot> Placeholders { get; } = placeholders;

    public string Display { get; } = display;
}

/// <summary>Analyzes one <c>Dbc.Def</c>/<c>Dbc.DefSetter</c> invocation.</summary>
internal sealed class DefinitionAnalysis(SyntaxNodeAnalysisContext context, KnownSymbols symbols)
{
    private const string Usage =
        "pass the selector's own parameters, in declaration order, as the member's arguments, e.g. (IList x, int index, object value) => x.Insert(index, value), " +
        "and Dbc.Out<T>.Value / Dbc.Ref<T>.Value for out/ref parameters";

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.MinimallyQualifiedFormat;

    private SemanticModel Model => context.SemanticModel;

    public void Run()
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!LooksLikeDbcCall(invocation)
            || Model.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Dbc)
            || method.Name is not ("Def" or "DefSetter")
            || method.TypeArguments.Length == 0)
        {
            return;
        }

        bool setter = method.Name == "DefSetter";
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0 || arguments[0].Expression is not LambdaExpressionSyntax selector || selector.ExpressionBody is not { } selectorBody)
        {
            return;
        }

        ITypeSymbol contractType = method.TypeArguments[0];
        if (contractType.TypeKind is TypeKind.TypeParameter or TypeKind.Error)
        {
            // Closed at run time (e.g. in a generic helper); Dbc.Make checks it then.
            return;
        }

        if (contractType.TypeKind != TypeKind.Interface)
        {
            Report(Descriptors.InvalidSelector, selector, $"contracts can only be defined for interfaces, because Dbc.Make proxies interfaces, and {Display(contractType)} is not one");
            return;
        }

        List<LambdaExpressionSyntax> clauses = arguments.Skip(1).Select(static a => a.Expression).OfType<LambdaExpressionSyntax>().ToList();
        if (!setter && ContractMethod(selectorBody) is { } formMethod)
        {
            if (formMethod.Name != "Invariant")
            {
                Report(
                    Descriptors.InvalidClause,
                    selectorBody,
                    $"a Dbc.Def without a member selector can only define invariants; attach Contract.{formMethod.Name} to a member, e.g. Dbc.Def((IFoo x, int a) => x.M(a), (IFoo x, int a) => Contract.{formMethod.Name}(...))");
                return;
            }

            AnalyzeClause(selector, member: null, contractType);
            foreach (LambdaExpressionSyntax clause in clauses)
            {
                AnalyzeClause(clause, member: null, contractType);
            }

            return;
        }

        if (SelectMember(selectorBody, ParameterNames(selector), contractType, setter) is not { } member)
        {
            return;
        }

        foreach (LambdaExpressionSyntax clause in clauses)
        {
            AnalyzeClause(clause, member, contractType);
        }
    }

    private static bool LooksLikeDbcCall(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText is "Def" or "DefSetter",
            IdentifierNameSyntax name => name.Identifier.ValueText is "Def" or "DefSetter",
            GenericNameSyntax generic => generic.Identifier.ValueText is "Def" or "DefSetter",
            _ => false,
        };

    private SelectedMember? SelectMember(ExpressionSyntax body, IReadOnlyList<string> parameters, ITypeSymbol contractType, bool setter)
    {
        ExpressionSyntax receiver;
        ISymbol? symbol;
        SeparatedSyntaxList<ArgumentSyntax> arguments = default;
        switch (Strip(body))
        {
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call:
                receiver = access.Expression;
                symbol = Model.GetSymbolInfo(call, context.CancellationToken).Symbol;
                arguments = call.ArgumentList.Arguments;
                break;
            case MemberAccessExpressionSyntax access:
                receiver = access.Expression;
                symbol = Model.GetSymbolInfo(access, context.CancellationToken).Symbol;
                break;
            case ElementAccessExpressionSyntax element:
                receiver = element.Expression;
                symbol = Model.GetSymbolInfo(element, context.CancellationToken).Symbol;
                arguments = element.ArgumentList.Arguments;
                break;
            case InvocationExpressionSyntax { Expression: SimpleNameSyntax name } call
                when Model.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol:
                Report(Descriptors.InvalidSelector, body, $"'{name.Identifier.ValueText}' must be invoked on the selector's first parameter '{(parameters.Count > 0 ? parameters[0] : "x")}'");
                return null;
            default:
                Report(Descriptors.InvalidSelector, body, "the selector must call a method, or read a property or indexer, of its first parameter, e.g. (IStack x, object item) => x.Push(item)");
                return null;
        }

        if (symbol is null)
        {
            return null;
        }

        string memberName = symbol is IPropertySymbol { IsIndexer: true } ? "this[]" : symbol.Name;
        if (Strip(receiver) is not IdentifierNameSyntax target || parameters.Count == 0 || target.Identifier.ValueText != parameters[0])
        {
            Report(Descriptors.InvalidSelector, body, $"'{memberName}' must be invoked on the selector's first parameter '{(parameters.Count > 0 ? parameters[0] : "x")}'");
            return null;
        }

        if (symbol.IsStatic
            || symbol is IMethodSymbol { ReducedFrom: not null }
            || symbol.ContainingType is not { TypeKind: TypeKind.Interface } declaring
            || !(SymbolEqualityComparer.Default.Equals(declaring, contractType) || contractType.AllInterfaces.Contains(declaring, SymbolEqualityComparer.Default)))
        {
            Report(Descriptors.InvalidSelector, body, $"'{memberName}' is not a member of {Display(contractType)} or its base interfaces");
            return null;
        }

        ImmutableArray<IParameterSymbol> memberParameters;
        ITypeSymbol? returnType;
        switch (symbol)
        {
            case IMethodSymbol methodSymbol when !setter:
                memberParameters = methodSymbol.Parameters;
                returnType = methodSymbol.ReturnsVoid ? null : methodSymbol.ReturnType;
                break;
            case IMethodSymbol:
                Report(Descriptors.InvalidSelector, body, "Dbc.DefSetter needs a property or an indexer");
                return null;
            case IPropertySymbol property when setter:
                if (property.SetMethod is null)
                {
                    Report(Descriptors.InvalidSelector, body, $"property '{property.Name}' has no setter");
                    return null;
                }

                memberParameters = property.Parameters;
                returnType = null;
                break;
            case IPropertySymbol property:
                if (property.GetMethod is null)
                {
                    Report(Descriptors.InvalidSelector, body, $"property '{property.Name}' has no getter; use Dbc.DefSetter for setters");
                    return null;
                }

                memberParameters = property.Parameters;
                returnType = property.Type;
                break;
            default:
                Report(Descriptors.InvalidSelector, body, "the selector must call a method, or read a property or indexer, of its first parameter, e.g. (IStack x, object item) => x.Push(item)");
                return null;
        }

        var placeholders = new Dictionary<string, PlaceholderSlot>();
        int next = 1;
        for (int i = 0; i < arguments.Count; i++)
        {
            if (i >= memberParameters.Length)
            {
                Report(Descriptors.InvalidSelector, body, Usage);
                return null;
            }

            IParameterSymbol parameter = memberParameters[i];
            ExpressionSyntax argument = Strip(arguments[i].Expression);
            if (parameter.RefKind is RefKind.Out or RefKind.Ref)
            {
                string kind = parameter.RefKind == RefKind.Out ? "Out" : "Ref";
                string expected = $"Dbc.{kind}<{Display(parameter.Type)}>.Value";
                if (Placeholder(argument) is not { } placeholder)
                {
                    Report(Descriptors.InvalidSelector, arguments[i], $"pass {expected} for the {kind.ToLowerInvariant()} parameter '{parameter.Name}'");
                    return null;
                }

                if (placeholder.IsOut != (parameter.RefKind == RefKind.Out))
                {
                    Report(
                        Descriptors.InvalidSelector,
                        arguments[i],
                        $"'{parameter.Name}' is {(parameter.RefKind == RefKind.Out ? "an out" : "a ref")} parameter, so pass {expected} instead of {placeholder.Text}");
                    return null;
                }

                if (placeholders.ContainsKey(placeholder.Text))
                {
                    Report(Descriptors.InvalidSelector, arguments[i], $"{placeholder.Text} is passed for two parameters; use Value, Value2 and Value3 for parameters of the same type");
                    return null;
                }

                placeholders.Add(placeholder.Text, new PlaceholderSlot(parameter.Name, placeholder.IsOut));
                continue;
            }

            if (next >= parameters.Count || argument is not IdentifierNameSyntax name || name.Identifier.ValueText != parameters[next])
            {
                Report(Descriptors.InvalidSelector, body, Usage);
                return null;
            }

            next++;
        }

        if (next != parameters.Count)
        {
            Report(Descriptors.InvalidSelector, body, Usage);
            return null;
        }

        return new SelectedMember(symbol, declaring, returnType, Awaited(returnType), placeholders, $"{Display(contractType)}.{memberName}");
    }

    private void AnalyzeClause(LambdaExpressionSyntax clause, SelectedMember? member, ITypeSymbol contractType)
    {
        if (clause.ExpressionBody is not { } body)
        {
            return;
        }

        if (ContractMethod(body) is not { } method || Strip(body) is not InvocationExpressionSyntax invocation)
        {
            Report(Descriptors.InvalidClause, body, "a contract clause must be a single call to System.Diagnostics.Contracts.Contract, e.g. Contract.Requires(...)");
            return;
        }

        ClauseKind kind;
        switch (method.Name)
        {
            case "Requires":
                kind = method.IsGenericMethod ? ClauseKind.RequiresOfException : ClauseKind.Requires;
                break;
            case "Ensures":
                kind = ClauseKind.Ensures;
                break;
            case "EnsuresOnThrow":
                kind = ClauseKind.EnsuresOnThrow;
                break;
            case "Invariant":
                kind = ClauseKind.Invariant;
                break;
            case "Assert":
                kind = ClauseKind.Assert;
                break;
            case "Assume":
                kind = ClauseKind.Assume;
                break;
            case "EndContractBlock":
                Report(Descriptors.ByRefMisuse, body, "Contract.EndContractBlock marks legacy if-then-throw preconditions inside method bodies; use Contract.Requires<TException> instead");
                return;
            case "ValueAtReturn":
                Report(
                    Descriptors.ByRefMisuse,
                    body,
                    "Contract.ValueAtReturn is not a clause; use it inside Ensures with an out/ref placeholder, e.g. Contract.Ensures(Contract.ValueAtReturn(out Dbc.Out<int>.Value) >= 0)");
                return;
            default:
                Report(Descriptors.InvalidClause, body, $"Contract.{method.Name} is not a clause; use it inside a condition, e.g. Contract.Ensures(Contract.{method.Name}(...))");
                return;
        }

        if (member is null && kind != ClauseKind.Invariant)
        {
            Report(
                Descriptors.InvalidClause,
                body,
                "an interface-level Dbc.Def accepts only Contract.Invariant clauses; attach other clauses to a member, e.g. Dbc.Def((IFoo x) => x.M(), (IFoo x) => Contract.Ensures(...))");
            return;
        }

        if (kind == ClauseKind.RequiresOfException && method.TypeArguments.Length == 1 && WhyNotThrowable(method.TypeArguments[0]) is { } reason)
        {
            Report(Descriptors.InvalidClause, body, $"Contract.Requires<{Display(method.TypeArguments[0])}> cannot be thrown: {reason}");
            return;
        }

        if (member is not null
            && kind is ClauseKind.Requires or ClauseKind.RequiresOfException or ClauseKind.Assert or ClauseKind.Assume
            && !SymbolEqualityComparer.Default.Equals(member.DeclaringInterface, contractType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InheritedPrecondition,
                body.GetLocation(),
                method.Name,
                Display(member.DeclaringInterface),
                member.Symbol is IPropertySymbol { IsIndexer: true } ? "this[]" : member.Symbol.Name,
                Display(contractType)));
        }

        bool readsTask = false;
        bool readsAwaited = false;
        foreach (SyntaxNode node in invocation.ArgumentList.DescendantNodes())
        {
            if (node is InvocationExpressionSyntax nested && ContractMethod(nested) is { } nestedMethod)
            {
                switch (CheckNested(nested, nestedMethod, kind, member, clause))
                {
                    case ResultRead.Task:
                        readsTask = true;
                        break;
                    case ResultRead.Awaited:
                        readsAwaited = true;
                        break;
                }
            }
            else if (node is MemberAccessExpressionSyntax access && Placeholder(access) is { } placeholder)
            {
                CheckPlaceholderRead(access, placeholder, kind, member);
            }
        }

        if (readsTask && readsAwaited)
        {
            Report(Descriptors.ResultMisuse, body, "a clause cannot read both the returned task and its awaited result");
        }
    }

    private ResultRead CheckNested(InvocationExpressionSyntax call, IMethodSymbol method, ClauseKind kind, SelectedMember? member, LambdaExpressionSyntax clause)
    {
        switch (method.Name)
        {
            case "Result":
                return CheckResult(call, method, kind, member);
            case "OldValue":
                ExpressionSyntax? value = call.ArgumentList.Arguments.Count == 1 ? call.ArgumentList.Arguments[0].Expression : null;
                if (kind is not (ClauseKind.Ensures or ClauseKind.EnsuresOnThrow))
                {
                    Report(Descriptors.OldValueMisuse, call, "Contract.OldValue can only be used in Ensures and EnsuresOnThrow clauses (CodeContracts error CC1023)");
                }
                else if (value is null)
                {
                    break;
                }
                else if (Model.GetConstantValue(Strip(value), context.CancellationToken).HasValue)
                {
                    Report(Descriptors.OldValueMisuse, call, "Contract.OldValue of a literal is always the literal (CodeContracts error CC1000)");
                }
                else if (ContainsResultOrOldValue(value))
                {
                    Report(Descriptors.OldValueMisuse, call, "Contract.OldValue cannot contain Contract.Result or another Contract.OldValue");
                }
                else if (!CapturableOnEntry(value, clause))
                {
                    Report(
                        Descriptors.OldValueMisuse,
                        call,
                        "Contract.OldValue can only refer to the variable of an enclosing Contract.ForAll/Exists(from, to, i => ...) whose bounds do not depend on the result; " +
                        "for other lambdas, capture a snapshot instead, e.g. Contract.OldValue(xs.ToArray())");
                }

                break;
            case "ValueAtReturn":
                if (call.ArgumentList.Arguments.Count != 1 || Placeholder(Strip(call.ArgumentList.Arguments[0].Expression)) is null)
                {
                    Report(Descriptors.ByRefMisuse, call, "Contract.ValueAtReturn needs an out/ref placeholder, e.g. Contract.ValueAtReturn(out Dbc.Out<int>.Value)");
                }
                else if (kind != ClauseKind.Ensures)
                {
                    Report(Descriptors.ByRefMisuse, call, "Contract.ValueAtReturn can only be used in Ensures clauses");
                }

                break;
        }

        return ResultRead.None;
    }

    private ResultRead CheckResult(InvocationExpressionSyntax call, IMethodSymbol method, ClauseKind kind, SelectedMember? member)
    {
        if (kind != ClauseKind.Ensures || member is null)
        {
            Report(Descriptors.ResultMisuse, call, kind == ClauseKind.EnsuresOnThrow
                ? "Contract.Result cannot be used in EnsuresOnThrow: the member threw instead of returning (CodeContracts error CC1001)"
                : "Contract.Result can only be used in Ensures clauses (CodeContracts error CC1023)");
            return ResultRead.None;
        }

        if (method.TypeArguments.Length != 1)
        {
            return ResultRead.None;
        }

        ITypeSymbol requested = method.TypeArguments[0];
        if (member.ReturnType is null)
        {
            Report(Descriptors.ResultMisuse, call, $"{member.Display} returns void, so Contract.Result<{Display(requested)}>() has no value");
            return ResultRead.None;
        }

        if (SymbolEqualityComparer.Default.Equals(requested, member.ReturnType))
        {
            return member.AwaitedType is null ? ResultRead.None : ResultRead.Task;
        }

        if (SymbolEqualityComparer.Default.Equals(requested, member.AwaitedType))
        {
            return ResultRead.Awaited;
        }

        string expected = member.AwaitedType is null
            ? Display(member.ReturnType)
            : $"{Display(member.AwaitedType)} (the awaited result) or {Display(member.ReturnType)}";
        Report(
            Descriptors.ResultMisuse,
            call,
            $"Contract.Result<{Display(requested)}>() does not match the return type of {member.Display}; use Contract.Result<{expected}>() (CodeContracts error CC1002)");
        return ResultRead.None;
    }

    /// <summary>
    /// Whether <c>Contract.OldValue(value)</c> can be evaluated before the call: it may only use the clause's parameters, variables
    /// it declares itself, and the index of an enclosing <c>Contract.ForAll/Exists(from, to, i =&gt; ...)</c> whose bounds can be.
    /// </summary>
    private bool CapturableOnEntry(ExpressionSyntax value, LambdaExpressionSyntax clause)
    {
        List<ISymbol> free = FreeVariables(value, clause);
        return free.Count == 0 || (free.Count == 1 && IsQuantifierIndex(free[0], clause));
    }

    private bool IsQuantifierIndex(ISymbol variable, LambdaExpressionSyntax clause)
    {
        if (variable is not IParameterSymbol { ContainingSymbol: IMethodSymbol { Parameters.Length: 1 } lambda }
            || lambda.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is not AnonymousFunctionExpressionSyntax predicate)
        {
            return false;
        }

        SyntaxNode? parent = predicate.Parent;
        while (parent is ParenthesizedExpressionSyntax or CastExpressionSyntax)
        {
            parent = parent.Parent;
        }

        return parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax quantifier } list } argument
            && list.Arguments.Count == 3
            && list.Arguments.IndexOf(argument) == 2
            && ContractMethod(quantifier) is { Name: "ForAll" or "Exists" }
            && list.Arguments.Take(2).All(bound => FreeVariables(bound.Expression, clause).Count == 0 && !ContainsResultOrOldValue(bound.Expression));
    }

    /// <summary>
    /// The lambda parameters and query range variables <paramref name="expression"/> reads that are neither the clause's
    /// parameters nor declared inside <paramref name="expression"/> – what the run time sees as free variables.
    /// </summary>
    private List<ISymbol> FreeVariables(ExpressionSyntax expression, LambdaExpressionSyntax clause)
    {
        var free = new List<ISymbol>();
        foreach (IdentifierNameSyntax identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            ISymbol? symbol = Model.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
            SyntaxNode? declaration = symbol switch
            {
                IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda } =>
                    lambda.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken),
                IRangeVariableSymbol range => range.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken),
                _ => null,
            };

            if (symbol is not null
                && declaration is not null
                && !(declaration.SyntaxTree == clause.SyntaxTree && declaration.Span == clause.Span)
                && !expression.Span.Contains(declaration.Span)
                && !free.Contains(symbol, SymbolEqualityComparer.Default))
            {
                free.Add(symbol);
            }
        }

        return free;
    }

    private bool ContainsResultOrOldValue(ExpressionSyntax expression) =>
        expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(n => ContractMethod(n) is { Name: "Result" or "OldValue" });

    /// <summary>
    /// Why a failed <c>Requires&lt;TException&gt;</c> couldn't create its exception, by the rules of the run time's
    /// TypedExceptionFactory; null when it can. Type parameters are closed at run time and checked then.
    /// </summary>
    private static string? WhyNotThrowable(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || named.TypeArguments.Any(static t => t.TypeKind == TypeKind.TypeParameter))
        {
            return null;
        }

        if (named.IsAbstract)
        {
            return $"'{Display(named)}' is abstract";
        }

        bool usable = named.InstanceConstructors.Any(static c =>
            c.DeclaredAccessibility == Accessibility.Public
            && c.Parameters.All(static p => p.RefKind == RefKind.None && p.Type.SpecialType == SpecialType.System_String)
            && (c.Parameters.Length < 2 || (c.Parameters.Length == 2 && c.Parameters[0].Name == "paramName")));
        return usable ? null : $"'{Display(named)}' has no public (string paramName, string message), (string message) or parameterless constructor";
    }

    private void CheckPlaceholderRead(MemberAccessExpressionSyntax access, Placeholder placeholder, ClauseKind kind, SelectedMember? member)
    {
        if (member is null)
        {
            Report(Descriptors.ByRefMisuse, access, $"{placeholder.Text} stands for an out/ref parameter of a member, and interface invariants have none");
            return;
        }

        if (!member.Placeholders.TryGetValue(placeholder.Text, out PlaceholderSlot? slot))
        {
            Report(Descriptors.ByRefMisuse, access, $"the selector doesn't pass {placeholder.Text} for a parameter of {member.Display}");
            return;
        }

        string? enclosing = EnclosingContractCall(access);
        if (enclosing == "ValueAtReturn")
        {
            return;
        }

        if (enclosing == "OldValue")
        {
            if (slot.IsOut)
            {
                Report(Descriptors.ByRefMisuse, access, $"out parameter '{slot.ParameterName}' has no value on entry, so Contract.OldValue can't capture it");
            }

            return;
        }

        if (slot.IsOut && kind != ClauseKind.Ensures)
        {
            Report(Descriptors.ByRefMisuse, access, kind == ClauseKind.EnsuresOnThrow
                ? $"out parameter '{slot.ParameterName}' has no defined value when the member throws"
                : $"out parameter '{slot.ParameterName}' has no value before the call; read it in an Ensures clause");
        }
        else if (!slot.IsOut && kind == ClauseKind.EnsuresOnThrow)
        {
            Report(
                Descriptors.ByRefMisuse,
                access,
                $"the value of ref parameter '{slot.ParameterName}' when the member throws is unknown; use Contract.OldValue({placeholder.Text}) for the value passed in");
        }
    }

    /// <summary>The nearest enclosing <c>Contract.OldValue</c>/<c>ValueAtReturn</c> call the expression is an argument of, if any.</summary>
    private string? EnclosingContractCall(SyntaxNode node)
    {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is InvocationExpressionSyntax call && ContractMethod(call) is { Name: "OldValue" or "ValueAtReturn" } method)
            {
                return method.Name;
            }

            if (current is LambdaExpressionSyntax)
            {
                return null;
            }
        }

        return null;
    }

    private IMethodSymbol? ContractMethod(ExpressionSyntax expression) =>
        Strip(expression) is InvocationExpressionSyntax call
        && Model.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol method
        && SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Contract)
            ? method
            : null;

    private Placeholder? Placeholder(ExpressionSyntax expression)
    {
        if (symbols.Out is null || symbols.Ref is null
            || Model.GetSymbolInfo(expression, context.CancellationToken).Symbol is not IFieldSymbol { IsStatic: true } field
            || field.ContainingType is not { IsGenericType: true } holder)
        {
            return null;
        }

        bool isOut = SymbolEqualityComparer.Default.Equals(holder.OriginalDefinition, symbols.Out);
        bool isRef = SymbolEqualityComparer.Default.Equals(holder.OriginalDefinition, symbols.Ref);
        return isOut || isRef
            ? new Placeholder($"Dbc.{(isOut ? "Out" : "Ref")}<{Display(holder.TypeArguments[0])}>.{field.Name}", isOut)
            : null;
    }

    private ITypeSymbol? Awaited(ITypeSymbol? type) =>
        type is INamedTypeSymbol { IsGenericType: true } named
        && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.TaskOfT) || SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.ValueTaskOfT))
            ? named.TypeArguments[0]
            : null;

    private static IReadOnlyList<string> ParameterNames(LambdaExpressionSyntax lambda) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter.Identifier.ValueText },
        ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Select(static p => p.Identifier.ValueText).ToList(),
        _ => new string[0],
    };

    private static ExpressionSyntax Strip(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    break;
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    break;
                case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } bang:
                    expression = bang.Operand;
                    break;
                default:
                    return expression;
            }
        }
    }

    private static string Display(ITypeSymbol type) => type.ToDisplayString(TypeFormat);

    private void Report(DiagnosticDescriptor descriptor, SyntaxNode node, string message) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, node.GetLocation(), message));
}

/// <summary>Which result a <c>Contract.Result</c> call of an awaitable member reads.</summary>
internal enum ResultRead
{
    None,
    Task,
    Awaited,
}

/// <summary>A <c>Dbc.Out&lt;T&gt;</c>/<c>Dbc.Ref&lt;T&gt;</c> field access.</summary>
internal sealed class Placeholder(string text, bool isOut)
{
    public string Text { get; } = text;

    public bool IsOut { get; } = isOut;
}
