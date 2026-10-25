using EdiMorph.Diagnostics;
using EdiMorph.Specifications.Tests.Factories;

namespace EdiMorph.Specifications.Tests;

public sealed class ProviderComplianceTests
{
    private readonly SpecificationCompiler _sut = SpecificationCompilerFactory.CreateX12(
        SpecificationCompilerFactory.CreateStandardRules(includeFixtureGuide: true),
        FixtureRuleDocuments.DialectSegment(),
        FixtureRuleDocuments.DialectWidenOverride(),
        FixtureRuleDocuments.SubsetProfile(),
        FixtureRuleDocuments.DialectProfile());

    [Fact]
    public async Task Compile_WhenSubsetWidensCodes_ShouldFailWithStandardLoosened()
    {
        // Arrange
        var baseline = await _sut.CompileAsync(TestDefaults.Request204());

        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureSubset));
        var baselineAgain = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.StandardLoosened
            && diagnostic.Category == DiagnosticCategory.ProviderPolicy);
        baselineAgain.Should().BeSameAs(baseline);
        baseline.Segments[TestDefaults.DialectSegmentId].Elements["01"].AllowedValues
            .Should().Equal(TestDefaults.CodeA, TestDefaults.CodeB);
    }

    [Fact]
    public async Task Compile_WhenDialectWidensCodesAndCitesParent_ShouldSucceedAsPartnerDialect()
    {
        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureDialect));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Identity.Compliance.Should().Be(ComplianceMode.PartnerDialect);
        result.Segments[TestDefaults.DialectSegmentId].Elements["01"].AllowedValues
            .Should().Equal(TestDefaults.CodeA, TestDefaults.CodeB, TestDefaults.DialectCode);
    }
}
