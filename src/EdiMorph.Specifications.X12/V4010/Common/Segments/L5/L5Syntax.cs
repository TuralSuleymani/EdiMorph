using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.L5;

public sealed class L5Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_L5_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "L5",
            subject,
            cancellationToken,
            new SyntaxNote('P', "03", "04"),
            new SyntaxNote('C', "07", "06"),
            new SyntaxNote('P', "08", "09"));
    }
}