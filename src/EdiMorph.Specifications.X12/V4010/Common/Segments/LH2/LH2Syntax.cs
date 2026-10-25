using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LH2;

public sealed class LH2Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LH2_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LH2",
            subject,
            cancellationToken,
            new SyntaxNote('P', "06", "07"),
            new SyntaxNote('P', "08", "09"),
            new SyntaxNote('P', "10", "11"));
    }
}