using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LH3;

public sealed class LH3Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LH3_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LH3",
            subject,
            cancellationToken,
            new SyntaxNote('P', "01", "02"));
    }
}