# BACKGROUND: Unit tests for existing implementation of ContractExpressions

As a developer I want to write comrehensive unit tests for the project ContractExpressions.
Baseling git commit 2c6a2ff6c63726c727cc155b5b931fd2f0cd0b95.

The project implements the design-by-contract principles. The contracts can be  associated with an interface `TIntf` using the methods `Dbc.Def<TIntf>(...)`. when an instance of a type implementing `TIntf` is created with `Dbc.Make<TIntf>(instance)`, then each call to the instance is done via a proxy (`ContractAwareProxy`) evaluating the contracts defined previously.

Some existing tests are in the project `ContractExpressions.Tests`.


# TASK: Write a new batch of unit tests 

Use the examples in the folder `CodeContracts/Demo`. 

Place the results into the project `ContractExpressions.Tests` in the folder `CE-1`