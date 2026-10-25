using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.G62;

public sealed class G62Syntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_G62_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "G62",
            subject,
            cancellationToken,
            new SyntaxNote('R', "01", "03"),
            new SyntaxNote('P', "01", "02"),
            new SyntaxNote('P', "03", "04"));
    }
}