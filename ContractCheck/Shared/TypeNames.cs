namespace ContractCheck;

/// <summary>Formats types the way C# source spells them (<c>int</c>, <c>List&lt;string&gt;</c>, <c>int?</c>, <c>int[]</c>).</summary>
internal static class TypeNames
{
    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
        [typeof(void)] = "void",
    };

    public static string Format(Type type)
    {
        if (Keywords.TryGetValue(type, out string? keyword))
        {
            return keyword;
        }

        if (type.IsArray)
        {
            return Format(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }

        if (type.IsByRef || type.IsPointer)
        {
            return Format(type.GetElementType()!) + (type.IsPointer ? "*" : "");
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Format(underlying) + "?";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        string name = type.Name;
        int tick = name.IndexOf('`');
        if (tick >= 0)
        {
            name = name[..tick];
        }

        if (type.IsGenericType)
        {
            Type[] arguments = type.GetGenericArguments();
            int inherited = type.IsNested ? type.DeclaringType!.GetGenericArguments().Length : 0;
            if (arguments.Length > inherited)
            {
                name += "<" + string.Join(", ", arguments.Skip(inherited).Select(Format)) + ">";
            }
        }

        return type.IsNested && type.DeclaringType is { } declaring ? Format(declaring) + "." + name : name;
    }
}
