using EdiMorph.Abstractions.Tests.Factories;
using EdiMorph.Rules;

namespace EdiMorph.Abstractions.Tests;

public sealed class RuleResultTests
{
    [Fact]
    public void Fail_WhenDiagnosticIsProvided_ShouldKeepThatDiagnostic()
    {
        // Arrange
        var diagnostic = SpecificationDiagnosticFactory.MissingHandler();

        // Act
        var result = RuleResult.Fail(diagnostic);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle().Which.Should().BeSameAs(diagnostic);
    }

    [Fact]
    public void Success_WhenNoDiagnosticsWereRecorded_ShouldSucceed()
    {
        // Act
        var result = RuleResult.Success;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }
}
