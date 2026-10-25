using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.N7;

public sealed class N7Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_N7_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "N7",
            subject,
            cancellationToken,
            new SyntaxNote('P', "03", "04"),
            new SyntaxNote('P', "05", "16"),
            new SyntaxNote('P', "08", "09"));
    }
}