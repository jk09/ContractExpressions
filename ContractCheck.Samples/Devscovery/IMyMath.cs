using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Devscovery;

/// <summary>Two flavours of absolute value (CodeContracts/Demo/Devscovery/MyMath.cs).</summary>
[ContractClass(typeof(MyMathContracts))]
public interface IMyMath
{
    /// <summary>.NET style: rejects <see cref="int.MinValue"/> with an <see cref="OverflowException"/>.</summary>
    int AbsNetStyle(int x);

    /// <summary>Java style: returns <see cref="int.MinValue"/> unchanged.</summary>
    int AbsJavaStyle(int x);
}
