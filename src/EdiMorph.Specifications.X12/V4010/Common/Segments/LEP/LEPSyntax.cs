using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LEP;

public sealed class LEPSyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LEP_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LEP",
            subject,
            cancellationToken,
            new SyntaxNote('P', "03", "04"));
    }
}