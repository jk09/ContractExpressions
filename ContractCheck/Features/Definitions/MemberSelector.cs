using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractCheck;

/// <summary>The interface member a <c>Dbc.Def</c>/<c>Dbc.DefSetter</c> call attaches clauses to.</summary>
internal sealed class MemberSignature
{
    public required Type InterfaceType { get; init; }

    /// <summary>The method a proxy intercepts: the method itself, a property getter, or a property/indexer setter.</summary>
    public required MethodInfo Method { get; init; }

    /// <summary>Number of parameters every clause lambda takes: the target, the member's arguments and, for setters, the value.</summary>
    public required int ClauseParameterCount { get; init; }

    public bool IsAwaitable { get; init; }

    /// <summary>The result type of <see cref="Task{TResult}"/>/<see cref="ValueTask{TResult}"/> members.</summary>
    public Type? AwaitedType { get; init; }

    public Type ReturnType => Method.ReturnType;

    public string DisplayName => $"{TypeNames.Format(InterfaceType)}.{Method.Name}";
}

/// <summary>Resolves the member selector lambda of <c>Dbc.Def</c> and <c>Dbc.DefSetter</c>.</summary>
internal static class MemberSelector
{
    private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static MemberSignature Select(LambdaExpression selector, bool setter)
    {
        Type interfaceType = selector.Parameters[0].Type;
        Expression body = StripConversions(selector.Body);

        MethodInfo method;
        Expression? instance;
        IReadOnlyList<Expression> arguments;
        switch (body)
        {
            case MethodCallExpression call when call.Method.DeclaringType != typeof(Contract):
                method = call.Method;
                instance = call.Object;
                arguments = call.Arguments;
                break;
            case MemberExpression { Member: PropertyInfo property } access:
                method = property.GetMethod
                    ?? throw DefinitionErrors.Create(interfaceType, selector, $"property '{property.Name}' has no getter; use Dbc.DefSetter for setters");
                instance = access.Expression;
                arguments = [];
                break;
            default:
                throw DefinitionErrors.Create(
                    interfaceType,
                    selector,
                    "the selector must call a method, or read a property or indexer, of its first parameter, e.g. (IStack x, object item) => x.Push(item)");
        }

        if (instance is null || StripConversions(instance) != selector.Parameters[0])
        {
            throw DefinitionErrors.Create(interfaceType, selector, $"'{method.Name}' must be invoked on the selector's first parameter '{selector.Parameters[0].Name}'");
        }

        if (!IsMemberOf(method, interfaceType))
        {
            throw DefinitionErrors.Create(interfaceType, selector, $"'{method.Name}' is not a member of {TypeNames.Format(interfaceType)} or its base interfaces");
        }

        if (arguments.Count != selector.Parameters.Count - 1
            || arguments.Where((argument, i) => StripConversions(argument) != selector.Parameters[i + 1]).Any())
        {
            throw DefinitionErrors.Create(
                interfaceType,
                selector,
                "pass the selector's own parameters, in declaration order, as the member's arguments, e.g. (IList x, int index, object value) => x.Insert(index, value)");
        }

        if (setter)
        {
            PropertyInfo property = FindProperty(method)
                ?? throw DefinitionErrors.Create(interfaceType, selector, "Dbc.DefSetter needs a property or an indexer");
            method = property.SetMethod
                ?? throw DefinitionErrors.Create(interfaceType, selector, $"property '{property.Name}' has no setter");
        }

        bool awaitable = Awaitables.IsAwaitable(method.ReturnType, out Type? awaitedType);
        return new MemberSignature
        {
            InterfaceType = interfaceType,
            Method = method,
            ClauseParameterCount = selector.Parameters.Count + (setter ? 1 : 0),
            IsAwaitable = awaitable,
            AwaitedType = awaitedType,
        };
    }

    /// <summary>The property whose getter or setter is <paramref name="accessor"/>, if any.</summary>
    public static PropertyInfo? FindProperty(MethodInfo accessor) =>
        accessor.IsSpecialName && accessor.DeclaringType is { } declaringType
            ? declaringType.GetProperties(AllInstance).FirstOrDefault(p =>
                MethodIdentity.Comparer.Equals(p.GetMethod, accessor) || MethodIdentity.Comparer.Equals(p.SetMethod, accessor))
            : null;

    private static bool IsMemberOf(MethodInfo method, Type interfaceType) =>
        method.DeclaringType is { } declaringType
        && (declaringType == interfaceType || interfaceType.GetInterfaces().Contains(declaringType));

    private static Expression StripConversions(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs } conversion)
        {
            expression = conversion.Operand;
        }

        return expression;
    }
}

/// <summary>Builds the exceptions thrown for malformed contract definitions.</summary>
internal static class DefinitionErrors
{
    public static InvalidOperationException Create(Type interfaceType, Expression expression, string reason) =>
        new($"Invalid contract definition for {TypeNames.Format(interfaceType)}: {ExpressionPrinter.Print(expression)} – {reason}.");

    public static InvalidOperationException Create(Type interfaceType, string reason) =>
        new($"Invalid contract definition for {TypeNames.Format(interfaceType)}: {reason}.");
}
