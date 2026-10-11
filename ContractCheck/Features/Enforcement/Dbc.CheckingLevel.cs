namespace ContractCheck;

public static partial class Dbc
{
    /// <summary>
    /// The clause kinds proxies check (default <see cref="ContractCheckingLevel.Full"/>). A change applies to the next call
    /// of every proxy; contracts are still validated when they are defined, whatever the level.
    /// </summary>
    /// <remarks>
    /// The initial value comes from the runtimeconfig setting <c>ContractCheck.CheckingLevel</c> (a level name, case-insensitive),
    /// e.g. <c>&lt;RuntimeHostConfigurationOption Include="ContractCheck.CheckingLevel" Value="Preconditions" /&gt;</c> in the app's
    /// project file, or <c>AppContext.SetData</c> before the first use.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Reading: the runtimeconfig setting is not a level name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Setting: the value is not a defined level.</exception>
    public static ContractCheckingLevel CheckingLevel
    {
        get => CheckingLevels.Current;
        set => CheckingLevels.Current = value;
    }
}

/// <summary>Holds the process-wide checking level and decides which clause kinds it includes.</summary>
internal static class CheckingLevels
{
    /// <summary>The runtimeconfig / <see cref="AppContext"/> setting that provides the initial level.</summary>
    public const string SettingName = "ContractCheck.CheckingLevel";

    private const int Unset = -1;
    private static int level = Unset;

    public static ContractCheckingLevel Current
    {
        get
        {
            int value = Volatile.Read(ref level);
            if (value != Unset)
            {
                return (ContractCheckingLevel)value;
            }

            // Not cached when invalid, so every use reports the bad setting until it is fixed.
            ContractCheckingLevel configured = Configured();
            Interlocked.CompareExchange(ref level, (int)configured, Unset);
            return (ContractCheckingLevel)Volatile.Read(ref level);
        }

        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{(int)value} is not a {nameof(ContractCheckingLevel)}.");
            }

            Volatile.Write(ref level, (int)value);
        }
    }

    /// <summary>Whether <paramref name="kind"/> is checked at <paramref name="current"/>.</summary>
    public static bool Includes(ContractCheckingLevel current, ClauseKind kind) => current switch
    {
        ContractCheckingLevel.Full => true,
        ContractCheckingLevel.PreAndPost => kind is ClauseKind.Requires or ClauseKind.RequiresOfException or ClauseKind.Ensures or ClauseKind.EnsuresOnThrow,
        ContractCheckingLevel.Preconditions => kind is ClauseKind.Requires or ClauseKind.RequiresOfException,
        ContractCheckingLevel.ReleaseRequires => kind is ClauseKind.RequiresOfException,
        _ => false,
    };

    /// <summary>Forgets the level, so the next read takes it from the runtimeconfig setting again (tests).</summary>
    internal static void Reset() => Volatile.Write(ref level, Unset);

    private static ContractCheckingLevel Configured()
    {
        if (AppContext.GetData(SettingName) is not { } data)
        {
            return ContractCheckingLevel.Full;
        }

        string text = Convert.ToString(data, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        foreach (ContractCheckingLevel candidate in Enum.GetValues<ContractCheckingLevel>())
        {
            if (string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"The runtime setting {SettingName} is '{text}', which is not a checking level; use one of {string.Join(", ", Enum.GetNames<ContractCheckingLevel>())}.");
    }
}
