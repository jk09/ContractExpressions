namespace ContractCheck.Samples.PropertyTests;

/// <summary>Assertions about properties themselves – used to pin known defects so the suite stays green while they are open.</summary>
internal static class PropertyAssert
{
    /// <summary>Asserts that FsCheck falsifies <paramref name="property"/> and that the failure mentions <paramref name="expected"/>.</summary>
    public static void Falsified(Property property, string expected, int maxTest = 100)
    {
        Exception failure = Assert.ThrowsAny<Exception>(() => Check.One(Config.QuickThrowOnFailure.WithMaxTest(maxTest), property));

        Assert.Contains("Falsifiable", failure.Message, StringComparison.Ordinal);
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }
}
