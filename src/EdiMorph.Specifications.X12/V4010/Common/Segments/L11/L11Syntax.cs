using EdiMorph.Rules;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.L11;

public sealed class L11Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_L11_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "L11",
            subject,
            cancellationToken,
            new SyntaxNote('R', "01", "03"),
            new SyntaxNote('P', "01", "02"));
    }
}