using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ContractCheck;

/// <summary>
/// Runs the contract class of an interface – the class named by its <see cref="ContractClassAttribute"/>, or else the class
/// marked <see cref="ContractClassForAttribute"/> for it – exactly once, so the <c>Dbc.Def</c> calls in its constructor
/// register the interface's contracts.
/// </summary>
internal static class ContractClassLoader
{
    private static readonly ConcurrentDictionary<Type, Lazy<bool>> Loaded = new();

    /// <summary>Loads the contract classes of <paramref name="interfaceType"/> and of every interface it extends.</summary>
    public static void EnsureLoaded(Type interfaceType)
    {
        foreach (Type type in InterfaceHierarchy.BaseFirst(interfaceType))
        {
            // Lazy with ExecutionAndPublication: concurrent first calls run the contract class once and share its outcome.
            _ = Loaded.GetOrAdd(type, static t => new Lazy<bool>(() => Load(t), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }
    }

    private static bool Load(Type interfaceType)
    {
        Type? declared = interfaceType.GetCustomAttribute<ContractClassAttribute>(inherit: false)?.TypeContainingContracts
            ?? ContractClassIndex.Find(interfaceType);
        if (declared is null)
        {
            return false;
        }

        Type contractClass = CloseOver(interfaceType, declared);
        Validate(interfaceType, contractClass);

        try
        {
            if (contractClass.IsAbstract)
            {
                // Abstract (or static) contract classes declare their contracts in a static constructor.
                RuntimeHelpers.RunClassConstructor(contractClass.TypeHandle);
            }
            else
            {
                _ = Activator.CreateInstance(contractClass, nonPublic: true);
            }
        }
        catch (Exception exception) when (exception is TargetInvocationException or TypeInitializationException && exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"The contract class {TypeNames.Format(contractClass)} of {TypeNames.Format(interfaceType)} failed: {exception.InnerException!.Message}",
                exception.InnerException);
        }

        return true;
    }

    private static Type CloseOver(Type interfaceType, Type contractClass)
    {
        if (!contractClass.IsGenericTypeDefinition)
        {
            return contractClass;
        }

        Type[] arguments = interfaceType.IsConstructedGenericType ? interfaceType.GetGenericArguments() : [];
        if (arguments.Length != contractClass.GetGenericArguments().Length)
        {
            throw DefinitionErrors.Create(
                interfaceType,
                $"the generic contract class {TypeNames.Format(contractClass)} must have the same type parameters as the interface");
        }

        return contractClass.MakeGenericType(arguments);
    }

    private static void Validate(Type interfaceType, Type contractClass)
    {
        if (!contractClass.IsClass)
        {
            throw DefinitionErrors.Create(
                interfaceType,
                $"[ContractClass] names {TypeNames.Format(contractClass)}, which is not a class (CodeContracts error CC1006)");
        }

        Type definition = contractClass.IsConstructedGenericType ? contractClass.GetGenericTypeDefinition() : contractClass;
        Type expected = interfaceType.IsConstructedGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
        ContractClassForAttribute? backPointer = definition.GetCustomAttribute<ContractClassForAttribute>(inherit: false);
        if (backPointer?.TypeContractsAreFor != expected)
        {
            throw DefinitionErrors.Create(
                interfaceType,
                $"[ContractClass] names {TypeNames.Format(definition)}, but that class is not marked [ContractClassFor(typeof({TypeNames.Format(expected)}))] (CodeContracts error CC1019)");
        }

        if (contractClass.IsAbstract ? contractClass.TypeInitializer is null : contractClass.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) is null)
        {
            throw DefinitionErrors.Create(
                interfaceType,
                $"the contract class {TypeNames.Format(contractClass)} needs a parameterless constructor (or, if abstract or static, a static constructor) that calls Dbc.Def");
        }
    }
}

/// <summary>
/// Finds contract classes by their <see cref="ContractClassForAttribute"/>. <see cref="ContractClassAttribute"/> is
/// <c>[Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]</c>, so a Release build drops it from an interface whose file
/// doesn't <c>#define CONTRACTS_FULL</c>. The contract class keeps its <see cref="ContractClassForAttribute"/>: its file must
/// define the symbol, or its <c>Contract</c> calls don't compile.
/// </summary>
internal static class ContractClassIndex
{
    private static readonly ConcurrentDictionary<Assembly, Lazy<ILookup<Type, Type>>> Indexes = new();

    /// <summary>The class marked <c>[ContractClassFor(typeof(interfaceType))]</c> in the interface's assembly, if any.</summary>
    public static Type? Find(Type interfaceType)
    {
        Type definition = interfaceType.IsConstructedGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
        if (definition.Assembly == typeof(object).Assembly)
        {
            // The framework's own interfaces carry no contract classes on .NET (it isn't compiled with CONTRACTS_FULL).
            return null;
        }

        Type[] candidates = [.. Indexes.GetOrAdd(definition.Assembly, static assembly => new Lazy<ILookup<Type, Type>>(() => Build(assembly))).Value[definition]];
        return candidates.Length switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw DefinitionErrors.Create(
                definition,
                $"several classes are marked [ContractClassFor(typeof({TypeNames.Format(definition)}))]: {string.Join(", ", candidates.Select(TypeNames.Format))}; name one with [ContractClass] on the interface (and #define CONTRACTS_FULL in its file)"),
        };
    }

    private static ILookup<Type, Type> Build(Assembly assembly) =>
        LoadableTypes(assembly)
            .Where(static type => type.IsClass && type.IsDefined(typeof(ContractClassForAttribute), inherit: false))
            .ToLookup(static type => type.GetCustomAttribute<ContractClassForAttribute>(inherit: false)!.TypeContractsAreFor);

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}

/// <summary>Orders an interface and the interfaces it extends so that base interfaces come first.</summary>
internal static class InterfaceHierarchy
{
    public static IReadOnlyList<Type> BaseFirst(Type interfaceType) =>
        [.. interfaceType.GetInterfaces().OrderBy(static i => i.GetInterfaces().Length), interfaceType];
}
