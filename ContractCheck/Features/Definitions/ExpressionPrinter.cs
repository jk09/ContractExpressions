using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ContractCheck;

/// <summary>
/// Prints expression trees as C#-like source text (<c>x.Count == Contract.OldValue(x.Count) + 1</c>) for failure messages,
/// dropping the conversions the C# compiler inserts implicitly.
/// </summary>
internal static class ExpressionPrinter
{
    private enum Precedence
    {
        Lowest,
        Lambda,
        Conditional,
        Coalesce,
        OrElse,
        AndAlso,
        Or,
        ExclusiveOr,
        And,
        Equality,
        Relational,
        Shift,
        Additive,
        Multiplicative,
        Unary,
        Primary,
    }

    public static string Print(Expression expression)
    {
        var builder = new StringBuilder();
        Write(builder, expression, Precedence.Lowest);
        return builder.ToString();
    }

    private static void Write(StringBuilder b, Expression e, Precedence context)
    {
        e = StripImplicitConversions(e);
        bool parenthesize = PrecedenceOf(e) < context;
        if (parenthesize)
        {
            b.Append('(');
        }

        WriteCore(b, e);
        if (parenthesize)
        {
            b.Append(')');
        }
    }

    private static void WriteCore(StringBuilder b, Expression e)
    {
        switch (e)
        {
            case BinaryExpression binary:
                WriteBinary(b, binary);
                break;
            case UnaryExpression unary:
                WriteUnary(b, unary);
                break;
            case ConstantExpression constant:
                b.Append(FormatConstant(constant.Value));
                break;
            case ParameterExpression parameter:
                b.Append(parameter.Name ?? "_");
                break;
            case MemberExpression member:
                WriteMember(b, member);
                break;
            case MethodCallExpression call:
                WriteCall(b, call);
                break;
            case ConditionalExpression conditional:
                Write(b, conditional.Test, Precedence.Coalesce);
                b.Append(" ? ");
                Write(b, conditional.IfTrue, Precedence.Conditional);
                b.Append(" : ");
                Write(b, conditional.IfFalse, Precedence.Conditional);
                break;
            case LambdaExpression lambda:
                WriteLambda(b, lambda);
                break;
            case NewExpression creation:
                WriteNew(b, creation);
                break;
            case NewArrayExpression array when array.NodeType == ExpressionType.NewArrayInit:
                b.Append("new[] { ");
                WriteList(b, array.Expressions);
                b.Append(" }");
                break;
            case NewArrayExpression array:
                b.Append("new ").Append(TypeNames.Format(ElementTypeOf(array.Type))).Append('[');
                WriteList(b, array.Expressions);
                b.Append(']');
                break;
            case TypeBinaryExpression typeTest when typeTest.NodeType == ExpressionType.TypeIs:
                Write(b, typeTest.Expression, Precedence.Relational);
                b.Append(" is ").Append(TypeNames.Format(typeTest.TypeOperand));
                break;
            case InvocationExpression invocation:
                Write(b, invocation.Expression, Precedence.Primary);
                b.Append('(');
                WriteList(b, invocation.Arguments);
                b.Append(')');
                break;
            case IndexExpression index:
                Write(b, index.Object!, Precedence.Primary);
                b.Append('[');
                WriteList(b, index.Arguments);
                b.Append(']');
                break;
            case DefaultExpression @default:
                b.Append("default(").Append(TypeNames.Format(@default.Type)).Append(')');
                break;
            default:
                b.Append(e);
                break;
        }
    }

    private static void WriteBinary(StringBuilder b, BinaryExpression binary)
    {
        if (binary.NodeType == ExpressionType.ArrayIndex)
        {
            Write(b, binary.Left, Precedence.Primary);
            b.Append('[');
            Write(b, binary.Right, Precedence.Lowest);
            b.Append(']');
            return;
        }

        Precedence own = PrecedenceOf(binary);
        Expression left = binary.Left;
        Expression right = binary.Right;

        // Enum and char comparisons arrive as (int)x.Kind == 1 and (int)c == 39; print them as x.Kind == Kind.Value and c == '\''.
        if (own is Precedence.Equality or Precedence.Relational
            && (TryUnwrapEnumComparison(ref left, ref right) || TryUnwrapEnumComparison(ref right, ref left)))
        {
            Write(b, left, own);
            b.Append(' ').Append(Operator(binary.NodeType)).Append(' ');
            Write(b, right, own + 1);
            return;
        }

        bool rightAssociative = binary.NodeType == ExpressionType.Coalesce;
        Write(b, left, rightAssociative ? own + 1 : own);
        b.Append(' ').Append(Operator(binary.NodeType)).Append(' ');
        Write(b, right, rightAssociative ? own : own + 1);
    }

    private static bool TryUnwrapEnumComparison(ref Expression enumSide, ref Expression constantSide)
    {
        if (enumSide is not UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            || constantSide is not ConstantExpression { Value: { } value })
        {
            return false;
        }

        if (UnderlyingEnum(convert.Operand.Type) is { } enumType)
        {
            enumSide = convert.Operand;
            constantSide = Expression.Constant(Enum.ToObject(enumType, value), enumType);
            return true;
        }

        if ((Nullable.GetUnderlyingType(convert.Operand.Type) ?? convert.Operand.Type) == typeof(char) && value is int code)
        {
            enumSide = convert.Operand;
            constantSide = Expression.Constant((char)code);
            return true;
        }

        return false;
    }

    private static Type? UnderlyingEnum(Type type)
    {
        Type candidate = Nullable.GetUnderlyingType(type) ?? type;
        return candidate.IsEnum ? candidate : null;
    }

    private static void WriteUnary(StringBuilder b, UnaryExpression unary)
    {
        switch (unary.NodeType)
        {
            case ExpressionType.Not:
                b.Append(unary.Type == typeof(bool) || unary.Type == typeof(bool?) ? '!' : '~');
                Write(b, unary.Operand, Precedence.Unary);
                break;
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
                b.Append('-');
                Write(b, unary.Operand, Precedence.Unary);
                break;
            case ExpressionType.UnaryPlus:
                b.Append('+');
                Write(b, unary.Operand, Precedence.Unary);
                break;
            case ExpressionType.OnesComplement:
                b.Append('~');
                Write(b, unary.Operand, Precedence.Unary);
                break;
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                b.Append('(').Append(TypeNames.Format(unary.Type)).Append(')');
                Write(b, unary.Operand, Precedence.Unary);
                break;
            case ExpressionType.TypeAs:
                Write(b, unary.Operand, Precedence.Relational);
                b.Append(" as ").Append(TypeNames.Format(unary.Type));
                break;
            case ExpressionType.ArrayLength:
                Write(b, unary.Operand, Precedence.Primary);
                b.Append(".Length");
                break;
            case ExpressionType.Quote:
                Write(b, unary.Operand, Precedence.Lowest);
                break;
            default:
                b.Append(unary);
                break;
        }
    }

    private static void WriteMember(StringBuilder b, MemberExpression member)
    {
        if (ByRefPlaceholders.Is(member.Member, out bool isOut))
        {
            b.Append(ByRefPlaceholders.Format((FieldInfo)member.Member, isOut));
            return;
        }

        if (member.Expression is ConstantExpression { Value: { } closure } && IsCompilerGenerated(closure.GetType()))
        {
            b.Append(member.Member.Name);
            return;
        }

        if (member.Expression is null)
        {
            b.Append(TypeNames.Format(member.Member.DeclaringType!));
        }
        else
        {
            Write(b, member.Expression, Precedence.Primary);
        }

        b.Append('.').Append(member.Member.Name);
    }

    private static void WriteCall(StringBuilder b, MethodCallExpression call)
    {
        MethodInfo method = call.Method;
        IReadOnlyList<Expression> arguments = call.Arguments;

        if (method.DeclaringType == typeof(Contract))
        {
            b.Append("Contract.").Append(method.Name);
            if (method.IsGenericMethod && method.Name is nameof(Contract.Result) or nameof(Contract.Requires) or nameof(Contract.EnsuresOnThrow))
            {
                b.Append('<').Append(TypeNames.Format(method.GetGenericArguments()[0])).Append('>');
            }

            b.Append('(');
            WriteArguments(b, method, arguments);
            b.Append(')');
            return;
        }

        if (call.Object is null && method.IsDefined(typeof(ExtensionAttribute), inherit: false) && arguments.Count > 0)
        {
            Write(b, arguments[0], Precedence.Primary);
            b.Append('.').Append(method.Name).Append('(');
            WriteList(b, arguments.Skip(1));
            b.Append(')');
            return;
        }

        if (call.Object is null)
        {
            b.Append(TypeNames.Format(method.DeclaringType!)).Append('.').Append(method.Name);
        }
        else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal) && arguments.Count > 0)
        {
            // Indexed property getters are C# indexers (get_Item, string's get_Chars, ...).
            Write(b, call.Object, Precedence.Primary);
            b.Append('[');
            WriteList(b, arguments);
            b.Append(']');
            return;
        }
        else if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal) && arguments.Count == 0)
        {
            Write(b, call.Object, Precedence.Primary);
            b.Append('.').Append(method.Name, 4, method.Name.Length - 4);
            return;
        }
        else
        {
            Write(b, call.Object, Precedence.Primary);
            b.Append('.').Append(method.Name);
        }

        if (method.IsGenericMethod && method.GetParameters().Length == 0)
        {
            b.Append('<').Append(string.Join(", ", method.GetGenericArguments().Select(TypeNames.Format))).Append('>');
        }

        b.Append('(');
        WriteArguments(b, method, arguments);
        b.Append(')');
    }

    /// <summary>Writes call arguments, with <c>out</c>/<c>ref</c> before the arguments of by-ref parameters.</summary>
    private static void WriteArguments(StringBuilder b, MethodInfo method, IReadOnlyList<Expression> arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        for (int i = 0; i < arguments.Count; i++)
        {
            if (i > 0)
            {
                b.Append(", ");
            }

            if (i < parameters.Length && parameters[i].ParameterType.IsByRef && !parameters[i].IsIn)
            {
                b.Append(parameters[i].IsOut ? "out " : "ref ");
            }

            Write(b, arguments[i], Precedence.Lowest);
        }
    }

    private static void WriteLambda(StringBuilder b, LambdaExpression lambda)
    {
        if (lambda.Parameters.Count == 1)
        {
            b.Append(lambda.Parameters[0].Name);
        }
        else
        {
            b.Append('(').Append(string.Join(", ", lambda.Parameters.Select(p => p.Name))).Append(')');
        }

        b.Append(" => ");
        Write(b, lambda.Body, Precedence.Lambda);
    }

    private static void WriteNew(StringBuilder b, NewExpression creation)
    {
        if (creation.Members is { Count: > 0 } members && IsCompilerGenerated(creation.Type))
        {
            b.Append("new { ");
            for (int i = 0; i < members.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(", ");
                }

                b.Append(members[i].Name).Append(" = ");
                Write(b, creation.Arguments[i], Precedence.Lowest);
            }

            b.Append(" }");
            return;
        }

        b.Append("new ").Append(TypeNames.Format(creation.Type)).Append('(');
        WriteList(b, creation.Arguments);
        b.Append(')');
    }

    private static void WriteList(StringBuilder b, IEnumerable<Expression> expressions)
    {
        bool first = true;
        foreach (Expression expression in expressions)
        {
            if (!first)
            {
                b.Append(", ");
            }

            Write(b, expression, Precedence.Lowest);
            first = false;
        }
    }

    private static Expression StripImplicitConversions(Expression e)
    {
        while (e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
               && IsImplicit(convert.Operand.Type, convert.Type))
        {
            e = convert.Operand;
        }

        return e;
    }

    private static bool IsImplicit(Type from, Type to)
    {
        if (from == to || to.IsAssignableFrom(from))
        {
            return true;
        }

        Type? toUnderlying = Nullable.GetUnderlyingType(to);
        Type fromUnderlying = Nullable.GetUnderlyingType(from) ?? from;
        if (toUnderlying is not null && (toUnderlying == fromUnderlying || IsImplicitNumeric(fromUnderlying, toUnderlying)))
        {
            return true;
        }

        return IsImplicitNumeric(from, to);
    }

    private static bool IsImplicitNumeric(Type from, Type to) => Type.GetTypeCode(from) switch
    {
        TypeCode.SByte => Type.GetTypeCode(to) is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.Byte => Type.GetTypeCode(to) is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.Int16 => Type.GetTypeCode(to) is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.UInt16 or TypeCode.Char => Type.GetTypeCode(to) is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.Int32 => Type.GetTypeCode(to) is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.UInt32 => Type.GetTypeCode(to) is TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.Int64 or TypeCode.UInt64 => Type.GetTypeCode(to) is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
        TypeCode.Single => Type.GetTypeCode(to) is TypeCode.Double,
        _ => false,
    } && !from.IsEnum && !to.IsEnum;

    private static Precedence PrecedenceOf(Expression e) => e.NodeType switch
    {
        ExpressionType.Lambda => Precedence.Lambda,
        ExpressionType.Conditional => Precedence.Conditional,
        ExpressionType.Coalesce => Precedence.Coalesce,
        ExpressionType.OrElse => Precedence.OrElse,
        ExpressionType.AndAlso => Precedence.AndAlso,
        ExpressionType.Or => Precedence.Or,
        ExpressionType.ExclusiveOr => Precedence.ExclusiveOr,
        ExpressionType.And => Precedence.And,
        ExpressionType.Equal or ExpressionType.NotEqual => Precedence.Equality,
        ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
            or ExpressionType.TypeIs or ExpressionType.TypeAs => Precedence.Relational,
        ExpressionType.LeftShift or ExpressionType.RightShift => Precedence.Shift,
        ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked => Precedence.Additive,
        ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo => Precedence.Multiplicative,
        ExpressionType.Not or ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.UnaryPlus
            or ExpressionType.OnesComplement or ExpressionType.Convert or ExpressionType.ConvertChecked => Precedence.Unary,
        _ => Precedence.Primary,
    };

    private static string Operator(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Add or ExpressionType.AddChecked => "+",
        ExpressionType.Subtract or ExpressionType.SubtractChecked => "-",
        ExpressionType.Multiply or ExpressionType.MultiplyChecked => "*",
        ExpressionType.Divide => "/",
        ExpressionType.Modulo => "%",
        ExpressionType.And => "&",
        ExpressionType.Or => "|",
        ExpressionType.ExclusiveOr => "^",
        ExpressionType.AndAlso => "&&",
        ExpressionType.OrElse => "||",
        ExpressionType.Equal => "==",
        ExpressionType.NotEqual => "!=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LeftShift => "<<",
        ExpressionType.RightShift => ">>",
        ExpressionType.Coalesce => "??",
        ExpressionType.Power => "**",
        _ => nodeType.ToString(),
    };

    private static string FormatConstant(object? value) => value switch
    {
        null => "null",
        string s => "\"" + Escape(s, '"') + "\"",
        char c => "'" + Escape(c.ToString(), '\'') + "'",
        bool flag => flag ? "true" : "false",
        Type type => "typeof(" + TypeNames.Format(type) + ")",
        Enum e => FormatEnum(e),
        float f => float.IsFinite(f) ? f.ToString("R", CultureInfo.InvariantCulture) + "f" : "float." + Special(f),
        double d => double.IsFinite(d) ? d.ToString("R", CultureInfo.InvariantCulture) : "double." + Special(d),
        decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
        long l => l.ToString(CultureInfo.InvariantCulture) + "L",
        ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "UL",
        uint u => u.ToString(CultureInfo.InvariantCulture) + "u",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ when IsCompilerGenerated(value.GetType()) => "<closure>",
        _ => value.ToString() ?? "",
    };

    private static string Special(double d) => double.IsNaN(d) ? "NaN" : d > 0 ? "PositiveInfinity" : "NegativeInfinity";

    private static string FormatEnum(Enum value)
    {
        string type = TypeNames.Format(value.GetType());
        string text = value.ToString();
        if (char.IsDigit(text[0]) || text[0] == '-')
        {
            return "(" + type + ")" + text;
        }

        return string.Join(" | ", text.Split(", ").Select(name => type + "." + name));
    }

    private static string Escape(string text, char quote)
    {
        var b = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            b.Append(c switch
            {
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\0' => "\\0",
                _ when c == quote => "\\" + quote,
                _ => c.ToString(),
            });
        }

        return b.ToString();
    }

    private static Type ElementTypeOf(Type arrayType) => arrayType.GetElementType() ?? arrayType;

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) || type.Name.StartsWith('<');
}
