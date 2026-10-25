using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LFH;

public sealed class LFHSyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LFH_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LFH",
            subject,
            cancellationToken,
            new SyntaxNote('P', "05", "06"));
    }
}