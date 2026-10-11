using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Addition;

[ContractClass(typeof(AdderContracts))]
public interface IAdder
{
    int Add(int a, int b);
}

public sealed class Adder : IAdder
{
    public int Add(int a, int b) => unchecked(a + b);
}
