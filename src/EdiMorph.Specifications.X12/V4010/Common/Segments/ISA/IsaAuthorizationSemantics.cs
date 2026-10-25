using EdiMorph.Diagnostics;
using EdiMorph.Rules;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.ISA;

public sealed class IsaAuthorizationSemantics : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_ISA_AUTH_SEMANTICS";

    public const string NoInformation = "00";

    public const int InformationLength = 10;

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(subject.Tag, "ISA", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Handler '{RuleId}' checks ISA, not {subject.Tag}.", nameof(subject));
        }

        var failures = new List<SpecificationDiagnostic>();
        RequireSpaces(subject, failures, "01", "02");
        RequireSpaces(subject, failures, "03", "04");
        return ValueTask.FromResult(failures.Count == 0 ? RuleResult.Success : RuleResult.Fail(failures));
    }

    private static void RequireSpaces(
        X12Segment subject,
        List<SpecificationDiagnostic> failures,
        string qualifierPosition,
        string informationPosition)
    {
        if (!subject.Elements.TryGetValue(qualifierPosition, out var qualifier) || qualifier != NoInformation)
        {
            return;
        }

        var information = subject.Elements.TryGetValue(informationPosition, out var value) ? value : null;
        if (information == new string(' ', InformationLength))
        {
            return;
        }

        failures.Add(new SpecificationDiagnostic(
            DiagnosticCodes.RelationalSyntax,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            $"When ISA{qualifierPosition} is {NoInformation}, ISA{informationPosition} must be {InformationLength} spaces.",
            RuleId,
            target: "ISA" + informationPosition));
    }
}
