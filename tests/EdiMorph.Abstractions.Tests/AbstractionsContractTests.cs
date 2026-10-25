using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using Xunit;

namespace EdiMorph.Abstractions.Tests;

public sealed class AbstractionsContractTests
{
    [Fact]
    public void Request_rejects_blank_format()
    {
        var error = Assert.Throws<ArgumentException>(() => new SpecificationRequest(" ", "004010"));
        Assert.Equal("format", error.ParamName);
    }

    [Fact]
    public void Registry_rejects_a_duplicate_rule_id()
    {
        var registry = new RuleRegistry();
        registry.Register(Descriptor("X12_V4010_ENVELOPE_INTEGRITY"));

        var error = Assert.Throws<InvalidOperationException>(
            () => registry.Register(Descriptor("X12_V4010_ENVELOPE_INTEGRITY")));

        Assert.Contains("X12_V4010_ENVELOPE_INTEGRITY", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Registry_revision_changes_when_a_handler_is_added()
    {
        var registry = new RuleRegistry();
        var before = registry.Revision;

        registry.Register(Descriptor("X12_V4010_L11_SYNTAX"));

        Assert.True(registry.TryGet("X12_V4010_L11_SYNTAX", out var found));
        Assert.NotNull(found);
        Assert.Equal(RuleScope.Segment, found.Scope);
        Assert.NotEqual(before, registry.Revision);
    }

    [Fact]
    public void Descriptor_rejects_a_type_that_is_not_a_validator()
    {
        var error = Assert.Throws<ArgumentException>(() => new RuleHandlerDescriptor(
            "X12_V4010_L11_SYNTAX",
            RuleScope.Segment,
            typeof(string),
            typeof(SampleSubject),
            mandatory: true));

        Assert.Equal("implementationType", error.ParamName);
    }

    [Fact]
    public void Failed_result_keeps_the_diagnostic()
    {
        var diagnostic = new SpecificationDiagnostic(
            DiagnosticCodes.HandlerMissing,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            "Handler is not registered.",
            "X12_V4010_ENVELOPE_INTEGRITY");

        var result = RuleResult.Fail(diagnostic);

        Assert.False(result.IsSuccess);
        Assert.Same(diagnostic, Assert.Single(result.Diagnostics));
        Assert.True(RuleResult.Success.IsSuccess);
    }

    private static RuleHandlerDescriptor Descriptor(string ruleId)
    {
        return RuleHandlerDescriptor.Create<SampleValidator, SampleSubject>(
            ruleId,
            RuleScope.Segment,
            mandatory: true,
            format: "X12",
            version: "004010");
    }

    private sealed record SampleSubject;

    private sealed class SampleValidator : IRuleValidator<SampleSubject>
    {
        public ValueTask<RuleResult> ValidateAsync(
            SampleSubject subject,
            RuleContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RuleResult.Success);
        }
    }
}
