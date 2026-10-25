using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.N1;

public sealed class N1Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_N1_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "N1",
            subject,
            cancellationToken,
            new SyntaxNote('R', "02", "03"),
            new SyntaxNote('P', "03", "04"));
    }
}