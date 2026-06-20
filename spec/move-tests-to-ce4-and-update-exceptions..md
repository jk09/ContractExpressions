Move all xUnit test fixture classes from the project #file:ContractExpressions.Tests into the project  #file:ContractExpressions4.Tests and adapt namespaces to use the `Dbc.Def` API from #file:ContractExpressions4.csproj 

- the `Dbc.Def` API calls should not be changed, but assumed to be identical in the source and target test projects
- do not change the code of the source tests except for the following:
- the `using` namespace clause, which should refer the project #file:ContractExpressions4.csproj \
- the checks for contract validation exceptions - assume that the `ContractViolationException` is thrown when the contract is failed, instead of `System.Diagnostics.Contracts.ContractException`
- keep the folder structure and filenames of the source test fixture classes, maybe adding disambiguators when needed