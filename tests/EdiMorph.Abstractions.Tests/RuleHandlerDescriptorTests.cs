using EdiMorph.Rules;

namespace EdiMorph.Abstractions.Tests;

public sealed class RuleHandlerDescriptorTests
{
    [Fact]
    public void Constructor_WhenImplementationIsNotAValidator_ShouldThrow()
    {
        // Act
        var construction = () => new RuleHandlerDescriptor(
            TestDefaults.SegmentSyntaxRuleId,
            RuleScope.Segment,
            typeof(string),
            typeof(SampleSubject),
            mandatory: true);

        // Assert
        construction.Should().Throw<ArgumentException>().WithParameterName("implementationType");
    }

    private sealed record SampleSubject;
}
