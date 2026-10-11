using System.Linq.Expressions;

namespace ContractCheck;

public static partial class Dbc
{
    /// <summary>Defines contracts for a property setter. The selector reads the property, e.g. <c>(IAccount x) =&gt; x.Limit</c>; the clauses receive the assigned value as their last parameter, e.g. <c>(IAccount x, decimal value) =&gt; Contract.Requires(value &gt;= 0)</c>.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the property by reading it.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, TValue>(Expression<Func<T, TValue>> property, params Expression<Action<T, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 1 index parameter. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, TValue>(Expression<Func<T, T1, TValue>> property, params Expression<Action<T, T1, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 2 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, TValue>(Expression<Func<T, T1, T2, TValue>> property, params Expression<Action<T, T1, T2, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 3 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, TValue>(Expression<Func<T, T1, T2, T3, TValue>> property, params Expression<Action<T, T1, T2, T3, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 4 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, TValue>(Expression<Func<T, T1, T2, T3, T4, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 5 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 6 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 7 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 8 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="T1">The type of the member's first parameter.</typeparam>
    /// <typeparam name="T2">The type of the member's second parameter.</typeparam>
    /// <typeparam name="T3">The type of the member's third parameter.</typeparam>
    /// <typeparam name="T4">The type of the member's 4th parameter.</typeparam>
    /// <typeparam name="T5">The type of the member's 5th parameter.</typeparam>
    /// <typeparam name="T6">The type of the member's 6th parameter.</typeparam>
    /// <typeparam name="T7">The type of the member's 7th parameter.</typeparam>
    /// <typeparam name="T8">The type of the member's 8th parameter.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 9 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 10 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 11 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 12 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 13 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);

    /// <summary>Defines contracts for the setter of an indexer with 14 index parameters. The selector reads the indexer, e.g. <c>(IList x, int index) =&gt; x[index]</c>; the clauses receive the assigned value as their last parameter.</summary>
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
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">Selects the indexer by reading it with the lambda's own index parameters.</param>
    /// <param name="contracts">Clauses taking the selector's parameters plus the assigned value; each is a single <c>Contract.*</c> clause call.</param>
    /// <exception cref="InvalidOperationException">A clause or the selector is not a valid contract definition.</exception>
    public static void DefSetter<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TValue>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TValue>> property, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TValue>>[] contracts)
        where T : class
        => ContractDefinitions.Define(property, contracts, setter: true);
}
