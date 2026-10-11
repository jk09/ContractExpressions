namespace ContractCheck.Tests.Analyzers;

/// <summary>Source snippets for the analyzer tests.</summary>
internal static class Snippets
{
    /// <summary>Contract interfaces with every kind of member the definitions select.</summary>
    public const string Prelude = """
        #define CONTRACTS_FULL
        using System;
        using System.Collections.Generic;
        using System.Diagnostics.Contracts;
        using System.Linq;
        using System.Linq.Expressions;
        using System.Threading.Tasks;
        using ContractCheck;

        public interface IStore
        {
            int Count { get; }
            int Limit { get; set; }
            string this[int index] { get; set; }
            void Add(string item);
            int Find(string item, int start);
            bool TryGet(string key, out int value);
            void Bump(ref int counter);
            Task<int> CountAsync();
            IReadOnlyList<string> Items { get; }
        }

        public interface ISpecialStore : IStore
        {
            void Compact();
        }

        public abstract class AbstractFailure : Exception
        {
        }

        public sealed class NoMessageFailure : Exception
        {
            public NoMessageFailure(int code)
            {
            }
        }

        public static class Holder
        {
            public static int Field;
        }

        """;

    /// <summary>The prelude plus <paramref name="definitions"/> as statements of a method.</summary>
    public static string Define(string definitions) => Prelude + $$"""
        public static class Definitions
        {
            public static void Define()
            {
                {{definitions}}
            }
        }
        """;
}
