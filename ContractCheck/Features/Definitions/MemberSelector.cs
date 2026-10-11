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

    /// <summary>For clause parameter <c>i + 1</c> (after the target): the index of the member argument it stands for.</summary>
    public required IReadOnlyList<int> ArgumentIndexes { get; init; }

    /// <summary>The <see cref="Dbc.Out{T}"/>/<see cref="Dbc.Ref{T}"/> placeholders the selector passes for out/ref parameters.</summary>
    public IReadOnlyDictionary<FieldInfo, ByRefSlot> Placeholders { get; init; } = new Dictionary<FieldInfo, ByRefSlot>();

    public bool IsAwaitable { get; init; }

    /// <summary>The result type of <see cref="Task{TResult}"/>/<see cref="ValueTask{TResult}"/> members.</summary>
    public Type? AwaitedType { get; init; }

    public Type ReturnType => Method.ReturnType;

    public string DisplayName => $"{TypeNames.Format(InterfaceType)}.{Method.Name}";
}

/// <summary>An out or ref parameter a placeholder stands for: its argument index and name.</summary>
internal sealed record ByRefSlot(int ArgumentIndex, bool IsOut, string Name);

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

        (List<int> argumentIndexes, Dictionary<FieldInfo, ByRefSlot> placeholders) = MapArguments(selector, interfaceType, method, arguments);

        if (setter)
        {
            PropertyInfo property = FindProperty(method)
                ?? throw DefinitionErrors.Create(interfaceType, selector, "Dbc.DefSetter needs a property or an indexer");
            method = property.SetMethod
                ?? throw DefinitionErrors.Create(interfaceType, selector, $"property '{property.Name}' has no setter");
            argumentIndexes.Add(arguments.Count);
        }

        bool awaitable = Awaitables.IsAwaitable(method.ReturnType, out Type? awaitedType);
        return new MemberSignature
        {
            InterfaceType = interfaceType,
            Method = method,
            ClauseParameterCount = selector.Parameters.Count + (setter ? 1 : 0),
            ArgumentIndexes = argumentIndexes,
            Placeholders = placeholders,
            IsAwaitable = awaitable,
            AwaitedType = awaitedType,
        };
    }

    /// <summary>
    /// Matches the call's arguments to the selector's own parameters (in order) and, for out/ref parameters, to
    /// <see cref="Dbc.Out{T}"/>/<see cref="Dbc.Ref{T}"/> placeholders.
    /// </summary>
    private static (List<int> ArgumentIndexes, Dictionary<FieldInfo, ByRefSlot> Placeholders) MapArguments(
        LambdaExpression selector, Type interfaceType, MethodInfo method, IReadOnlyList<Expression> arguments)
    {
        const string Usage = "pass the selector's own parameters, in declaration order, as the member's arguments, e.g. (IList x, int index, object value) => x.Insert(index, value), " +
            "and Dbc.Out<T>.Value / Dbc.Ref<T>.Value for out/ref parameters";
        ParameterInfo[] parameters = method.GetParameters();
        List<int> argumentIndexes = [];
        Dictionary<FieldInfo, ByRefSlot> placeholders = new(ByRefPlaceholders.Comparer);
        int next = 1;
        for (int i = 0; i < arguments.Count; i++)
        {
            Expression argument = StripConversions(arguments[i]);
            ParameterInfo parameter = parameters[i];
            if (parameter.ParameterType.IsByRef && !parameter.IsIn)
            {
                string kind = parameter.IsOut ? "Out" : "Ref";
                string expected = $"Dbc.{kind}<{TypeNames.Format(parameter.ParameterType.GetElementType()!)}>.Value";
                if (argument is not MemberExpression { Member: FieldInfo field } || !ByRefPlaceholders.Is(field, out bool isOut))
                {
                    throw DefinitionErrors.Create(interfaceType, selector, $"pass {expected} for the {kind.ToLowerInvariant()} parameter '{parameter.Name}'");
                }

                if (isOut != parameter.IsOut)
                {
                    throw DefinitionErrors.Create(
                        interfaceType,
                        selector,
                        $"'{parameter.Name}' is {(parameter.IsOut ? "an out" : "a ref")} parameter, so pass {expected} instead of {ByRefPlaceholders.Format(field, isOut)}");
                }

                if (!placeholders.TryAdd(field, new ByRefSlot(i, isOut, parameter.Name ?? $"argument {i}")))
                {
                    throw DefinitionErrors.Create(
                        interfaceType,
                        selector,
                        $"{ByRefPlaceholders.Format(field, isOut)} is passed for two parameters; use Value, Value2 and Value3 for parameters of the same type");
                }

                continue;
            }

            if (next >= selector.Parameters.Count || argument != selector.Parameters[next])
            {
                throw DefinitionErrors.Create(interfaceType, selector, Usage);
            }

            argumentIndexes.Add(i);
            next++;
        }

        if (next != selector.Parameters.Count)
        {
            throw DefinitionErrors.Create(interfaceType, selector, Usage);
        }

        return (argumentIndexes, placeholders);
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
