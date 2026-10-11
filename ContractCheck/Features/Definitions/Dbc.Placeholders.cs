namespace ContractCheck;

#pragma warning disable CA2211 // Placeholders must be assignable fields: C# only passes variables and fields as out/ref arguments.

public static partial class Dbc
{
    /// <summary>
    /// Stands for an <see langword="out"/> parameter in a <c>Dbc.Def</c> selector and its clauses – expression-tree lambdas
    /// can't declare <see langword="out"/> parameters:
    /// <code>
    /// Dbc.Def(static (IDict d, string key) =&gt; d.TryGetValue(key, out Dbc.Out&lt;int&gt;.Value),
    ///     static (IDict d, string key) =&gt; Contract.Ensures(!Contract.Result&lt;bool&gt;() || Dbc.Out&lt;int&gt;.Value &gt;= 0));
    /// </code>
    /// In <c>Ensures</c> clauses a placeholder is the parameter's value when the member returns
    /// (<c>Contract.ValueAtReturn(out Dbc.Out&lt;T&gt;.Value)</c> means the same). The fields are never read or written at run time.
    /// </summary>
    /// <typeparam name="T">The parameter's type.</typeparam>
    public static class Out<T>
    {
        /// <summary>The first <see langword="out"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value = default!;

        /// <summary>The second <see langword="out"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value2 = default!;

        /// <summary>The third <see langword="out"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value3 = default!;
    }

    /// <summary>
    /// Stands for a <see langword="ref"/> parameter in a <c>Dbc.Def</c> selector and its clauses:
    /// <code>
    /// Dbc.Def(static (ICounter c, int step) =&gt; c.Bump(ref Dbc.Ref&lt;int&gt;.Value, step),
    ///     static (ICounter c, int step) =&gt; Contract.Requires(Dbc.Ref&lt;int&gt;.Value &gt;= 0),
    ///     static (ICounter c, int step) =&gt; Contract.Ensures(Dbc.Ref&lt;int&gt;.Value == Contract.OldValue(Dbc.Ref&lt;int&gt;.Value) + step));
    /// </code>
    /// In entry checks a placeholder is the value passed in, in <c>Ensures</c> and member invariants the value when the member
    /// returns, and inside <c>Contract.OldValue</c> the value passed in. The fields are never read or written at run time.
    /// </summary>
    /// <typeparam name="T">The parameter's type.</typeparam>
    public static class Ref<T>
    {
        /// <summary>The first <see langword="ref"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value = default!;

        /// <summary>The second <see langword="ref"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value2 = default!;

        /// <summary>The third <see langword="ref"/> parameter of type <typeparamref name="T"/>.</summary>
        public static T Value3 = default!;
    }
}

#pragma warning restore CA2211

/// <summary>Recognises the <see cref="Dbc.Out{T}"/> and <see cref="Dbc.Ref{T}"/> placeholder fields.</summary>
internal static class ByRefPlaceholders
{
    /// <summary>Whether <paramref name="member"/> is a placeholder field; <paramref name="isOut"/> tells which kind.</summary>
    public static bool Is(System.Reflection.MemberInfo member, out bool isOut)
    {
        isOut = false;
        if (member is not System.Reflection.FieldInfo { IsStatic: true, DeclaringType: { IsGenericType: true } declaring })
        {
            return false;
        }

        Type definition = declaring.GetGenericTypeDefinition();
        isOut = definition == typeof(Dbc.Out<>);
        return isOut || definition == typeof(Dbc.Ref<>);
    }

    /// <summary>Compares placeholder fields by declaring type and name.</summary>
    public static IEqualityComparer<System.Reflection.FieldInfo> Comparer { get; } = new FieldComparer();

    /// <summary>C# text of a placeholder, e.g. <c>Dbc.Out&lt;int&gt;.Value</c>.</summary>
    public static string Format(System.Reflection.FieldInfo field, bool isOut) =>
        $"Dbc.{(isOut ? "Out" : "Ref")}<{TypeNames.Format(field.FieldType)}>.{field.Name}";

    private sealed class FieldComparer : IEqualityComparer<System.Reflection.FieldInfo>
    {
        public bool Equals(System.Reflection.FieldInfo? x, System.Reflection.FieldInfo? y) =>
            ReferenceEquals(x, y) || (x is not null && y is not null && x.DeclaringType == y.DeclaringType && x.Name == y.Name);

        public int GetHashCode(System.Reflection.FieldInfo obj) => HashCode.Combine(obj.DeclaringType, obj.Name);
    }
}
