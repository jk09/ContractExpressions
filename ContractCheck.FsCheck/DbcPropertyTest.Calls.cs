using System.Collections;
using System.Globalization;
using System.Reflection;
using FsCheck;
using FsCheck.Fluent;

namespace ContractCheck.FsCheck;

public static partial class DbcPropertyTest
{
    /// <summary>
    /// A property that throws random sequences of calls at one proxy per case: every method, property and indexer of
    /// <typeparamref name="T"/> (inherited members included) with arguments from FsCheck, so the contracts are checked
    /// across state changes without hand-written steps. Returned tasks are awaited.
    /// </summary>
    /// <remarks>
    /// A call rejected by an entry check is skipped; exceptions of <see cref="CallOptions.ExpectedExceptions"/> types are
    /// normal outcomes (after the proxy checked <c>EnsuresOnThrow</c>); any other failure fails the case, and FsCheck shrinks
    /// both the sequence and the arguments. Members FsCheck can't supply arguments for – <c>out</c>/<c>ref</c> or pointer
    /// parameters, open generic methods, types without a generator in <see cref="CallOptions.ArbMap"/> – are left out
    /// (<see cref="CallableMembers{T}"/> lists the rest).
    /// </remarks>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="proxyFactory">Creates a fresh proxy for each case, usually <c>() =&gt; Dbc.Make&lt;T&gt;(new Impl())</c>.</param>
    /// <param name="options">Generators, expected exceptions and excluded members.</param>
    /// <exception cref="InvalidOperationException"><typeparamref name="T"/> has no member that can be called.</exception>
    public static Property CheckCalls<T>(Func<T> proxyFactory, CallOptions? options = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(proxyFactory);
        options ??= new CallOptions();
        CallCatalog catalog = CallCatalog.For(typeof(T), options);
        if (catalog.Members.Count == 0)
        {
            throw new InvalidOperationException($"{typeof(T).Name} has no member DbcPropertyTest.CheckCalls can call; see CallableMembers.");
        }

        return Prop.ForAll(
            catalog.Sequences(),
            calls => CheckSequence(proxyFactory, calls.Select(call => (Action<T>)(proxy => call.Invoke(proxy, options)))));
    }

    /// <summary>The members <see cref="CheckCalls{T}(Func{T}, CallOptions?)"/> calls, as they appear in counterexamples.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="options">Generators and excluded members, as passed to <c>CheckCalls</c>.</param>
    /// <returns>One entry per callable member, e.g. <c>Push(string)</c>, <c>Count</c>, <c>this[int] = string</c>.</returns>
    public static IReadOnlyList<string> CallableMembers<T>(CallOptions? options = null)
        where T : class =>
        [.. CallCatalog.For(typeof(T), options ?? new CallOptions()).Members.Select(static member => member.Signature)];
}

/// <summary>Options of <see cref="DbcPropertyTest.CheckCalls{T}(Func{T}, CallOptions?)"/>.</summary>
public sealed class CallOptions
{
    /// <summary>Where arguments come from; merge your own generators, e.g. <c>ArbMap.Default.Merge&lt;Generators&gt;()</c>.</summary>
    public IArbMap ArbMap { get; init; } = global::FsCheck.Fluent.ArbMap.Default;

    /// <summary>
    /// Exception types a call may throw as documented behaviour (e.g. <see cref="InvalidOperationException"/> "stock is short").
    /// Contract failures are never expected: a typed precondition still just skips the call.
    /// </summary>
    public IReadOnlyCollection<Type> ExpectedExceptions { get; init; } = [];

    /// <summary>Members to leave out, by method or property name (an indexer is <c>Item</c>).</summary>
    public IReadOnlyCollection<string> Exclude { get; init; } = [];
}

/// <summary>One callable member: how to generate, shrink and show its arguments.</summary>
internal sealed class CallableMember(MethodInfo method, Arbitrary<object>[] arguments, string signature)
{
    public MethodInfo Method { get; } = method;

    public Arbitrary<object>[] Arguments { get; } = arguments;

    public string Signature { get; } = signature;

    public Gen<GeneratedCall> Generator =>
        Arguments.Length == 0
            ? Gen.Constant(new GeneratedCall(this, []))
            : Gen.CollectToArray(Arguments.Select(static arbitrary => arbitrary.Generator)).Select(values => new GeneratedCall(this, values));
}

/// <summary>A generated call; <see cref="ToString"/> shows it as C#, so counterexamples read like a call log.</summary>
public sealed class GeneratedCall
{
    internal GeneratedCall(CallableMember member, object[] arguments)
    {
        Member = member;
        Arguments = arguments;
    }

    internal CallableMember Member { get; }

    internal object[] Arguments { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        MethodInfo method = Member.Method;
        string[] args = [.. Arguments.Select(Format)];
        if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal))
        {
            return args.Length == 0 ? method.Name[4..] : $"this[{string.Join(", ", args)}]";
        }

        if (method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal))
        {
            string target = args.Length == 1 ? method.Name[4..] : $"this[{string.Join(", ", args[..^1])}]";
            return $"{target} = {args[^1]}";
        }

        return $"{method.Name}({string.Join(", ", args)})";
    }

    internal void Invoke(object proxy, CallOptions options)
    {
        try
        {
            object? result = Member.Method.Invoke(proxy, BindingFlags.DoNotWrapExceptions, binder: null, (object?[])Arguments.Clone(), CultureInfo.InvariantCulture);
            DbcPropertyTest.AwaitResult(result);
        }
        catch (Exception exception) when (exception.GetContractFailureKind() is null && options.ExpectedExceptions.Any(type => type.IsInstanceOfType(exception)))
        {
            // Documented behaviour; the proxy has already checked the member's EnsuresOnThrow clauses.
        }
    }

    internal IEnumerable<GeneratedCall> Shrink()
    {
        for (int i = 0; i < Arguments.Length; i++)
        {
            foreach (object smaller in Member.Arguments[i].Shrinker(Arguments[i]))
            {
                object[] arguments = (object[])Arguments.Clone();
                arguments[i] = smaller;
                yield return new GeneratedCall(Member, arguments);
            }
        }
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        string text => "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
        char c => $"'{c}'",
        bool b => b ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(Format)) + "]",
        _ => value.ToString() ?? value.GetType().Name,
    };
}

/// <summary>The callable members of an interface and the generator of call sequences over them.</summary>
internal sealed class CallCatalog
{
    private CallCatalog(IReadOnlyList<CallableMember> members) => Members = members;

    public IReadOnlyList<CallableMember> Members { get; }

    public static CallCatalog For(Type interfaceType, CallOptions options)
    {
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"{interfaceType.Name} is not an interface.", nameof(interfaceType));
        }

        IEnumerable<MethodInfo> methods = new[] { interfaceType }.Concat(interfaceType.GetInterfaces())
            .SelectMany(static type => type.GetMethods())
            .Where(static method => !method.IsStatic);

        List<CallableMember> members = [];
        foreach (MethodInfo method in methods)
        {
            if (Describe(method, options) is { } member)
            {
                members.Add(member);
            }
        }

        return new CallCatalog(members);
    }

    /// <summary>Sequences of 0..size calls; shrinks by dropping calls, then by shrinking arguments.</summary>
    public Arbitrary<GeneratedCall[]> Sequences() =>
        Arb.From(Gen.ArrayOf(Gen.OneOf(Members.Select(static member => member.Generator))), Shrink);

    private static IEnumerable<GeneratedCall[]> Shrink(GeneratedCall[] calls)
    {
        if (calls.Length > 1)
        {
            yield return calls[..(calls.Length / 2)];
            yield return calls[(calls.Length / 2)..];
        }

        for (int i = 0; i < calls.Length; i++)
        {
            yield return [.. calls[..i], .. calls[(i + 1)..]];
        }

        for (int i = 0; i < calls.Length; i++)
        {
            foreach (GeneratedCall smaller in calls[i].Shrink())
            {
                GeneratedCall[] copy = (GeneratedCall[])calls.Clone();
                copy[i] = smaller;
                yield return copy;
            }
        }
    }

    private static CallableMember? Describe(MethodInfo method, CallOptions options)
    {
        bool accessor = method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal));
        if ((method.IsSpecialName && !accessor) || method.ContainsGenericParameters || method.ReturnType.IsByRef)
        {
            return null;
        }

        string name = accessor ? method.Name[4..] : method.Name;
        if (options.Exclude.Contains(name, StringComparer.Ordinal))
        {
            return null;
        }

        ParameterInfo[] parameters = method.GetParameters();
        var arguments = new Arbitrary<object>[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type type = parameters[i].ParameterType;
            if (type.IsByRef || type.IsPointer || TryArbitrary(options.ArbMap, type) is not { } arbitrary)
            {
                return null;
            }

            arguments[i] = arbitrary;
        }

        return new CallableMember(method, arguments, Signature(method, parameters, accessor));
    }

    private static Arbitrary<object>? TryArbitrary(IArbMap arbMap, Type type)
    {
        try
        {
            Arbitrary<object> arbitrary = arbMap.ArbFor(type);
            _ = arbitrary.Generator.Sample(1, 1);
            return arbitrary;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }

    private static string Signature(MethodInfo method, ParameterInfo[] parameters, bool accessor)
    {
        string[] types = [.. parameters.Select(static p => Short(p.ParameterType))];
        if (!accessor)
        {
            return $"{method.Name}({string.Join(", ", types)})";
        }

        bool getter = method.Name.StartsWith("get_", StringComparison.Ordinal);
        int indexCount = getter ? types.Length : types.Length - 1;
        string target = indexCount == 0 ? method.Name[4..] : $"this[{string.Join(", ", types[..indexCount])}]";
        return getter ? target : $"{target} = {types[^1]}";
    }

    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte", [typeof(char)] = "char",
        [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint",
        [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
        [typeof(decimal)] = "decimal", [typeof(string)] = "string", [typeof(object)] = "object",
    };

    private static string Short(Type type) =>
        Keywords.TryGetValue(type, out string? keyword) ? keyword
        : type.IsArray ? Short(type.GetElementType()!) + "[]"
        : Nullable.GetUnderlyingType(type) is { } underlying ? Short(underlying) + "?"
        : type.IsGenericType ? $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", type.GetGenericArguments().Select(Short))}>"
        : type.Name;
}
