#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4;

[ContractClass(typeof(DivisionProgramContracts))]
public interface IDivisionProgram
{
    double Divide(double dividend, double divisor);
}

[ContractClassFor(typeof(IDivisionProgram))]
public class DivisionProgramContracts
{
    public DivisionProgramContracts()
    {
        Dbc.Def(static (IDivisionProgram x, double dividend, double divisor) => x.Divide(dividend, divisor),
            static (IDivisionProgram x, double dividend, double divisor) => Contract.Requires(divisor != 0),
            static (IDivisionProgram x, double dividend, double divisor) => Contract.Ensures(Contract.Result<double>() * divisor == dividend));
    }
}
