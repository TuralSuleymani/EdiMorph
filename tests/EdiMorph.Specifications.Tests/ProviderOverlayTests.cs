using EdiMorph.Specifications.Tests.Factories;

namespace EdiMorph.Specifications.Tests;

public sealed class ProviderOverlayTests
{
    private readonly SpecificationCompiler _sut = SpecificationCompilerFactory.CreateX12(
        SpecificationCompilerFactory.CreateStandardRules(includeIsaAuthorization: true, includeFixtureGuide: true),
        FixtureRuleDocuments.Segment(),
        FixtureRuleDocuments.TransactionOverride(),
        FixtureRuleDocuments.ProviderOverride(),
        FixtureRuleDocuments.CarrierProfile(),
        FixtureRuleDocuments.SecondCarrierProfile());

    [Fact]
    public async Task Compile_WhenCalledTwice_ShouldReturnTheSameBaseInstance()
    {
        // Act
        var first = await _sut.CompileAsync(TestDefaults.Request204());
        var second = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task Compile_WhenTwoProvidersShareTheStandard_ShouldReuseTheBaseAndDifferInFingerprint()
    {
        // Act
        var baseline = await _sut.CompileAsync(TestDefaults.Request204());
        var first = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureCarrier));
        var second = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureCarrierTwo));
        var baselineAgain = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        baseline.IsSuccess.Should().BeTrue();
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        baselineAgain.Should().BeSameAs(baseline);
        first.BaseFingerprint.Should().Be(baseline.Fingerprint);
        second.BaseFingerprint.Should().Be(baseline.Fingerprint);
        first.Fingerprint.Should().NotBe(second.Fingerprint);
        first.Should().NotBeSameAs(baseline);
        second.Segments[TestDefaults.FixtureSegmentId].Should().BeSameAs(baseline.Segments[TestDefaults.FixtureSegmentId]);
    }

    [Fact]
    public async Task Compile_WhenProfileOnlyTightens_ShouldRecordThreeOriginsAndStandardSubset()
    {
        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureCarrier));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Identity.Compliance.Should().Be(ComplianceMode.StandardSubset);
        var element = result.Segments[TestDefaults.FixtureSegmentId].Elements["01"];
        element.Usage.Should().Be(TestDefaults.MandatoryUsage);
        element.AllowedValues.Should().Equal(TestDefaults.CodeA, TestDefaults.CodeB);
        element.Origins.Select(origin => origin.Layer).Should().Equal(
            "segment",
            "transaction-override",
            "segment-override");
        element.Origins.Select(origin => origin.DocumentId).Should().Equal(
            TestDefaults.FixtureSegmentId,
            TestDefaults.FixtureTransactionOverrideId,
            TestDefaults.FixtureProviderOverrideId);
    }

    [Fact]
    public async Task Compile_WhenTheSameHandlerIsSelectedTwice_ShouldBindItOnce()
    {
        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204(TestDefaults.FixtureCarrier));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Handlers.Should().ContainSingle(binding => binding.RuleId == TestDefaults.FixtureGuideRuleId);
    }

    [Fact]
    public async Task Compile_WhenIsaAndEnvelopeHandlersArePresent_ShouldBindEachOnceAsMandatory()
    {
        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Handlers.Should().ContainSingle(binding =>
            binding.RuleId == TestDefaults.EnvelopeIntegrityRuleId && binding.Mandatory);
        result.Handlers.Should().ContainSingle(binding =>
            binding.RuleId == TestDefaults.IsaAuthorizationRuleId && binding.Mandatory);
    }
}
