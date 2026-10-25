using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.LAD;

public sealed class LADSyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_LAD_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "LAD",
            subject,
            cancellationToken,
            new SyntaxNote('P', "01", "02"),
            new SyntaxNote('P', "03", "04"),
            new SyntaxNote('P', "05", "06"),
            new SyntaxNote('P', "07", "08"),
            new SyntaxNote('P', "09", "10"),
            new SyntaxNote('P', "11", "12"));
    }
}