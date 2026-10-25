using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LH4;

public sealed class LH4Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LH4_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LH4",
            subject,
            cancellationToken,
            new SyntaxNote('P', "08", "12"));
    }
}