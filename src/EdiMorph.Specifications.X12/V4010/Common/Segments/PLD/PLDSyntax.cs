using EdiMorph.Rules;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.PLD;

public sealed class PLDSyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_PLD_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "PLD",
            subject,
            cancellationToken,
            new SyntaxNote('P', "03", "04"));
    }
}