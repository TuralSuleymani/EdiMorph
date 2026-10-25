using EdiMorph.Rules;

namespace EdiMorph.Abstractions.Tests.Factories;

internal static class RuleHandlerDescriptorFactory
{
    public static RuleHandlerDescriptor Create(
        string ruleId = TestDefaults.EnvelopeIntegrityRuleId,
        RuleScope scope = RuleScope.Segment)
    {
        return RuleHandlerDescriptor.Create<AcceptingValidator, SampleSubject>(
            ruleId,
            scope,
            mandatory: true,
            format: TestDefaults.X12Format,
            version: TestDefaults.X12Version);
    }

    private sealed record SampleSubject;

    private sealed class AcceptingValidator : IRuleValidator<SampleSubject>
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
