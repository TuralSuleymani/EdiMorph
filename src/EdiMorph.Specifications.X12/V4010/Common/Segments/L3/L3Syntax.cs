using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.L3;

public sealed class L3Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_L3_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "L3",
            subject,
            cancellationToken,
            new SyntaxNote('P', "01", "02"),
            new SyntaxNote('P', "03", "04"),
            new SyntaxNote('P', "09", "10"),
            new SyntaxNote('C', "12", "01"),
            new SyntaxNote('P', "14", "15"));
    }
}