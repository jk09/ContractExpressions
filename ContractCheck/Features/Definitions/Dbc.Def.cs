using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractCheck;

/// <summary>
/// Design-by-Contract entry points: <c>Dbc.Def</c> declares contracts for interface members, and
/// <see cref="Make{T}(T)"/> wraps an implementation in a proxy that checks them on every call.
/// </summary>
/// <remarks>
/// Contracts are usually declared in the parameterless constructor of a class marked
/// <see cref="ContractClassForAttribute"/>, referenced from the interface by <see cref="ContractClassAttribute"/>;
/// <see cref="Make{T}(T)"/> runs that constructor once per interface. Files that call <c>Contract.*</c> inside
/// clauses must <c>#define CONTRACTS_FULL</c>, because those methods are <c>[Conditional("CONTRACTS_FULL")]</c>.
/// The <c>Def</c> overloads cover members with 0 to 15 parameters; the overloads below differ only in arity.
/// </remarks>
public static partial class Dbc
{
    /// <summary>
    /// Defines contracts for a parameterless interface member that returns <see langword="void"/>; or, when <paramref name="member"/>
    /// is itself a <c>Contract.Invariant</c> clause such as <c>(IAccount x) =&gt; Contract.Invariant(x.Balance &gt;= 0)</c>,
    /// defines invariants of the whole interface (checked by <see cref="Make{T}(T)"/> and after every non-pure member).
    /// </summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="member">Selects the member, e.g. <c>(IStack x) =&gt; x.Clear()</c>, or is the first interface invariant.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>, or further invariants in the interface form.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T>(Expression<Action<T>> member, params Expression<Action<T>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a property, or a method without parameters, that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, TResult>(Expression<Func<T, TResult>> member, params Expression<Action<T>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 1 parameter that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1>(Expression<Action<T, T1>> member, params Expression<Action<T, T1>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 1 parameter that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, TResult>(Expression<Func<T, T1, TResult>> member, params Expression<Action<T, T1>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 2 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2>(Expression<Action<T, T1, T2>> member, params Expression<Action<T, T1, T2>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 2 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, TResult>(Expression<Func<T, T1, T2, TResult>> member, params Expression<Action<T, T1, T2>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 3 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3>(Expression<Action<T, T1, T2, T3>> member, params Expression<Action<T, T1, T2, T3>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 3 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, TResult>(Expression<Func<T, T1, T2, T3, TResult>> member, params Expression<Action<T, T1, T2, T3>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 4 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4>(Expression<Action<T, T1, T2, T3, T4>> member, params Expression<Action<T, T1, T2, T3, T4>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 4 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, TResult>(Expression<Func<T, T1, T2, T3, T4, TResult>> member, params Expression<Action<T, T1, T2, T3, T4>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 5 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5>(Expression<Action<T, T1, T2, T3, T4, T5>> member, params Expression<Action<T, T1, T2, T3, T4, T5>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 5 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 6 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6>(Expression<Action<T, T1, T2, T3, T4, T5, T6>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 6 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 7 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 7 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 8 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 8 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 9 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 9 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 10 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 10 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 11 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 11 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 12 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 12 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 13 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 13 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 14 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <typeparam name="T14">The type of the member's 14th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 14 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <typeparam name="T14">The type of the member's 14th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for an interface member with 15 parameters that returns <see langword="void"/>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <typeparam name="T14">The type of the member's 14th parameter.</typeparam>
    /// <typeparam name="T15">The type of the member's 15th parameter.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);

    /// <summary>Defines contracts for a method or indexer with 15 parameters that returns a value; clauses read it with <c>Contract.Result&lt;TResult&gt;()</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="T9">The type of the member's 9th parameter.</typeparam>
    /// <typeparam name="T10">The type of the member's 10th parameter.</typeparam>
    /// <typeparam name="T11">The type of the member's 11th parameter.</typeparam>
    /// <typeparam name="T12">The type of the member's 12th parameter.</typeparam>
    /// <typeparam name="T13">The type of the member's 13th parameter.</typeparam>
    /// <typeparam name="T14">The type of the member's 14th parameter.</typeparam>
    /// <typeparam name="T15">The type of the member's 15th parameter.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="member">Selects the member, passing the lambda's own parameters as its arguments in order.</param>
    /// <param name="contracts">Clauses taking the same parameters as <paramref name="member"/>; each is a single call to <c>Contract.Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c>, <c>Ensures</c>, <c>EnsuresOnThrow&lt;TException&gt;</c> or <c>Invariant</c>.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>> member, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>[] contracts)
        where T : class
        => ContractDefinitions.Define(member, contracts, setter: false);
}
