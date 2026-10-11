#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractCheck.Tests.Definitions.Arity;

/// <summary>
/// Exercises every <c>Dbc.Def</c> overload (members with 0–15 parameters, void and value-returning) and every
/// <c>Dbc.DefSetter</c> overload (a property and indexers with 1–14 index parameters). The contract class below
/// calls each overload once, so this file also proves they all compile with type inference.
/// The shapes differ only in arity; keep them uniform when adding cases. Resolution R19 (0–15 parameters plus DefSetter).
/// </summary>
public class DefOverloadArityTests
{
    private const BindingFlags Flags = BindingFlags.DoNotWrapExceptions;

    public static TheoryData<int> MethodArities => new(Enumerable.Range(0, 16));

    public static TheoryData<int> IndexerArities => new(Enumerable.Range(1, 14));

    [Theory]
    [MemberData(nameof(MethodArities))]
    public void Void_member_precondition_is_checked_for_every_arity(int arity)
    {
        IWide proxy = Dbc.Make<IWide>(new Wide { Enabled = true });
        MethodInfo method = typeof(IWide).GetMethod("M" + arity)!;

        method.Invoke(proxy, Flags, null, Arguments(arity, last: 1), null);

        if (arity == 0)
        {
            IWide disabled = Dbc.Make<IWide>(new Wide { Enabled = false });
            Expect.Violation(ContractFailureKind.Precondition, () => method.Invoke(disabled, Flags, null, [], null));
        }
        else
        {
            Expect.Violation(ContractFailureKind.Precondition, () => method.Invoke(proxy, Flags, null, Arguments(arity, last: -1), null));
        }
    }

    [Theory]
    [MemberData(nameof(MethodArities))]
    public void Value_member_postcondition_is_checked_for_every_arity(int arity)
    {
        MethodInfo method = typeof(IWide).GetMethod("F" + arity)!;
        object?[] arguments = Arguments(arity, last: 1);

        Assert.Equal(arity, method.Invoke(Dbc.Make<IWide>(new Wide()), Flags, null, arguments, null));
        Expect.Violation(ContractFailureKind.Postcondition, () => method.Invoke(Dbc.Make<IWide>(new Wide { Broken = true }), Flags, null, arguments, null));
    }

    [Fact]
    public void Property_setter_precondition_is_checked()
    {
        IWide proxy = Dbc.Make<IWide>(new Wide());

        proxy.Value = 3;

        Assert.Equal(3, proxy.Value);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Value = -1);
    }

    [Theory]
    [MemberData(nameof(IndexerArities))]
    public void Indexer_setter_precondition_is_checked_for_every_arity(int arity)
    {
        IWide proxy = Dbc.Make<IWide>(new Wide());
        PropertyInfo indexer = typeof(IWide).GetProperties().Single(p => p.GetIndexParameters().Length == arity);
        object?[] index = Arguments(arity, last: 1);

        indexer.SetValue(proxy, 5, Flags, null, index, null);

        Assert.Equal(5, indexer.GetValue(proxy, index));
        Expect.Violation(ContractFailureKind.Precondition, () => indexer.SetValue(proxy, -5, Flags, null, index, null));
    }

    private static object?[] Arguments(int arity, int last) =>
        [.. Enumerable.Range(1, arity).Select(i => (object?)(i == arity ? last : 1))];
}

[ContractClass(typeof(WideContracts))]
public interface IWide
{
    bool Enabled { get; }

    int Value { get; set; }

    void M0();
    void M1(int a1);
    void M2(int a1, int a2);
    void M3(int a1, int a2, int a3);
    void M4(int a1, int a2, int a3, int a4);
    void M5(int a1, int a2, int a3, int a4, int a5);
    void M6(int a1, int a2, int a3, int a4, int a5, int a6);
    void M7(int a1, int a2, int a3, int a4, int a5, int a6, int a7);
    void M8(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8);
    void M9(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9);
    void M10(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10);
    void M11(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11);
    void M12(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12);
    void M13(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13);
    void M14(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14);
    void M15(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15);

    int F0();
    int F1(int a1);
    int F2(int a1, int a2);
    int F3(int a1, int a2, int a3);
    int F4(int a1, int a2, int a3, int a4);
    int F5(int a1, int a2, int a3, int a4, int a5);
    int F6(int a1, int a2, int a3, int a4, int a5, int a6);
    int F7(int a1, int a2, int a3, int a4, int a5, int a6, int a7);
    int F8(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8);
    int F9(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9);
    int F10(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10);
    int F11(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11);
    int F12(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12);
    int F13(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13);
    int F14(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14);
    int F15(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15);

    int this[int i1] { get; set; }
    int this[int i1, int i2] { get; set; }
    int this[int i1, int i2, int i3] { get; set; }
    int this[int i1, int i2, int i3, int i4] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13] { get; set; }
    int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14] { get; set; }
}

public sealed class Wide : IWide
{
    private readonly Dictionary<string, int> cells = [];

    public bool Enabled { get; set; }

    public bool Broken { get; set; }

    public int Value { get; set; }

    public void M0() { }
    public void M1(int a1) { }
    public void M2(int a1, int a2) { }
    public void M3(int a1, int a2, int a3) { }
    public void M4(int a1, int a2, int a3, int a4) { }
    public void M5(int a1, int a2, int a3, int a4, int a5) { }
    public void M6(int a1, int a2, int a3, int a4, int a5, int a6) { }
    public void M7(int a1, int a2, int a3, int a4, int a5, int a6, int a7) { }
    public void M8(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) { }
    public void M9(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) { }
    public void M10(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) { }
    public void M11(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) { }
    public void M12(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) { }
    public void M13(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) { }
    public void M14(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) { }
    public void M15(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) { }

    public int F0() => 0 + (Broken ? 1 : 0);
    public int F1(int a1) => 0 + (a1 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F2(int a1, int a2) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F3(int a1, int a2, int a3) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F4(int a1, int a2, int a3, int a4) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F5(int a1, int a2, int a3, int a4, int a5) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F6(int a1, int a2, int a3, int a4, int a5, int a6) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F7(int a1, int a2, int a3, int a4, int a5, int a6, int a7) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F8(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F9(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F10(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F11(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F12(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F13(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F14(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0) + (a14 > 0 ? 1 : 0) + (Broken ? 1 : 0);
    public int F15(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) => 0 + (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0) + (a14 > 0 ? 1 : 0) + (a15 > 0 ? 1 : 0) + (Broken ? 1 : 0);

    public int this[int i1] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1 })); set => cells[string.Join(',', new[] { i1 })] = value; }
    public int this[int i1, int i2] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2 })); set => cells[string.Join(',', new[] { i1, i2 })] = value; }
    public int this[int i1, int i2, int i3] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3 })); set => cells[string.Join(',', new[] { i1, i2, i3 })] = value; }
    public int this[int i1, int i2, int i3, int i4] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13 })] = value; }
    public int this[int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14] { get => cells.GetValueOrDefault(string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14 })); set => cells[string.Join(',', new[] { i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14 })] = value; }
}

[ContractClassFor(typeof(IWide))]
internal sealed class WideContracts
{
    public WideContracts()
    {
        Dbc.Def(static (IWide x) => x.M0(), static (IWide x) => Contract.Requires(x.Enabled));
        Dbc.Def(static (IWide x, int a1) => x.M1(a1), static (IWide x, int a1) => Contract.Requires(a1 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2) => x.M2(a1, a2), static (IWide x, int a1, int a2) => Contract.Requires(a2 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3) => x.M3(a1, a2, a3), static (IWide x, int a1, int a2, int a3) => Contract.Requires(a3 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4) => x.M4(a1, a2, a3, a4), static (IWide x, int a1, int a2, int a3, int a4) => Contract.Requires(a4 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5) => x.M5(a1, a2, a3, a4, a5), static (IWide x, int a1, int a2, int a3, int a4, int a5) => Contract.Requires(a5 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6) => x.M6(a1, a2, a3, a4, a5, a6), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6) => Contract.Requires(a6 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7) => x.M7(a1, a2, a3, a4, a5, a6, a7), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7) => Contract.Requires(a7 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) => x.M8(a1, a2, a3, a4, a5, a6, a7, a8), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) => Contract.Requires(a8 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) => x.M9(a1, a2, a3, a4, a5, a6, a7, a8, a9), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) => Contract.Requires(a9 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) => x.M10(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) => Contract.Requires(a10 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) => x.M11(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) => Contract.Requires(a11 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) => x.M12(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) => Contract.Requires(a12 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) => x.M13(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) => Contract.Requires(a13 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) => x.M14(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) => Contract.Requires(a14 >= 0));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) => x.M15(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) => Contract.Requires(a15 >= 0));

        Dbc.Def(static (IWide x) => x.F0(), static (IWide x) => Contract.Ensures(Contract.Result<int>() == 0));
        Dbc.Def(static (IWide x, int a1) => x.F1(a1), static (IWide x, int a1) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2) => x.F2(a1, a2), static (IWide x, int a1, int a2) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3) => x.F3(a1, a2, a3), static (IWide x, int a1, int a2, int a3) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4) => x.F4(a1, a2, a3, a4), static (IWide x, int a1, int a2, int a3, int a4) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5) => x.F5(a1, a2, a3, a4, a5), static (IWide x, int a1, int a2, int a3, int a4, int a5) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6) => x.F6(a1, a2, a3, a4, a5, a6), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7) => x.F7(a1, a2, a3, a4, a5, a6, a7), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) => x.F8(a1, a2, a3, a4, a5, a6, a7, a8), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) => x.F9(a1, a2, a3, a4, a5, a6, a7, a8, a9), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) => x.F10(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) => x.F11(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) => x.F12(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) => x.F13(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) => x.F14(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0) + (a14 > 0 ? 1 : 0)));
        Dbc.Def(static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) => x.F15(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15), static (IWide x, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15) => Contract.Ensures(Contract.Result<int>() == (a1 > 0 ? 1 : 0) + (a2 > 0 ? 1 : 0) + (a3 > 0 ? 1 : 0) + (a4 > 0 ? 1 : 0) + (a5 > 0 ? 1 : 0) + (a6 > 0 ? 1 : 0) + (a7 > 0 ? 1 : 0) + (a8 > 0 ? 1 : 0) + (a9 > 0 ? 1 : 0) + (a10 > 0 ? 1 : 0) + (a11 > 0 ? 1 : 0) + (a12 > 0 ? 1 : 0) + (a13 > 0 ? 1 : 0) + (a14 > 0 ? 1 : 0) + (a15 > 0 ? 1 : 0)));

        Dbc.DefSetter(static (IWide x) => x.Value, static (IWide x, int value) => Contract.Requires(value >= 0));
        Dbc.DefSetter(static (IWide x, int i1) => x[i1], static (IWide x, int i1, int value) => Contract.Requires(value >= 0 && i1 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2) => x[i1, i2], static (IWide x, int i1, int i2, int value) => Contract.Requires(value >= 0 && i2 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3) => x[i1, i2, i3], static (IWide x, int i1, int i2, int i3, int value) => Contract.Requires(value >= 0 && i3 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4) => x[i1, i2, i3, i4], static (IWide x, int i1, int i2, int i3, int i4, int value) => Contract.Requires(value >= 0 && i4 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5) => x[i1, i2, i3, i4, i5], static (IWide x, int i1, int i2, int i3, int i4, int i5, int value) => Contract.Requires(value >= 0 && i5 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6) => x[i1, i2, i3, i4, i5, i6], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int value) => Contract.Requires(value >= 0 && i6 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7) => x[i1, i2, i3, i4, i5, i6, i7], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int value) => Contract.Requires(value >= 0 && i7 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8) => x[i1, i2, i3, i4, i5, i6, i7, i8], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int value) => Contract.Requires(value >= 0 && i8 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int value) => Contract.Requires(value >= 0 && i9 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9, i10], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int value) => Contract.Requires(value >= 0 && i10 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int value) => Contract.Requires(value >= 0 && i11 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int value) => Contract.Requires(value >= 0 && i12 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int value) => Contract.Requires(value >= 0 && i13 >= 0));
        Dbc.DefSetter(static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14) => x[i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14], static (IWide x, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int value) => Contract.Requires(value >= 0 && i14 >= 0));
    }
}
