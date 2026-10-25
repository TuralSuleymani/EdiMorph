using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.MS3;

public sealed class MS3Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_MS3_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "MS3",
            subject,
            cancellationToken,
            new SyntaxNote('C', "05", "03"));
    }
}