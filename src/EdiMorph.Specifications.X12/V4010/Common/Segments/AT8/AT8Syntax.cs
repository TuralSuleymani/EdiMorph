using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.AT8;

public sealed class AT8Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_AT8_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "AT8",
            subject,
            cancellationToken,
            new SyntaxNote('P', "01", "02", "03"),
            new SyntaxNote('P', "06", "07"));
    }
}