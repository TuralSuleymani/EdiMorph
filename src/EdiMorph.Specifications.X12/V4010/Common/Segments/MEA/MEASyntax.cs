using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.MEA;

public sealed class MEASyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_MEA_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "MEA",
            subject,
            cancellationToken,
            new SyntaxNote('R', "03", "05", "06", "08"),
            new SyntaxNote('C', "05", "04"),
            new SyntaxNote('C', "06", "04"),
            new SyntaxNote('L', "07", "03", "05", "06"),
            new SyntaxNote('E', "08", "03"));
    }
}