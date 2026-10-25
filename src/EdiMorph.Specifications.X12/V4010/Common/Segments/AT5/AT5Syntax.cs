using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.AT5;

public sealed class AT5Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_AT5_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "AT5",
            subject,
            cancellationToken,
            new SyntaxNote('E', "01", "03"),
            new SyntaxNote('E', "02", "03"));
    }
}