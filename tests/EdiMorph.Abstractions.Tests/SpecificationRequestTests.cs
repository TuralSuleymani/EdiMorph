namespace EdiMorph.Abstractions.Tests;

public sealed class SpecificationRequestTests
{
    public static TheoryData<string, string, string> BlankRequiredValues =>
        new()
        {
            { TestDefaults.BlankValue, TestDefaults.X12Version, "format" },
            { TestDefaults.X12Format, TestDefaults.BlankValue, "version" }
        };

    [Theory]
    [MemberData(nameof(BlankRequiredValues))]
    public void Constructor_WhenRequiredValueIsBlank_ShouldThrow(
        string format,
        string version,
        string parameterName)
    {
        // Act
        var construction = () => new SpecificationRequest(format, version);

        // Assert
        construction.Should().Throw<ArgumentException>().WithParameterName(parameterName);
    }
}
