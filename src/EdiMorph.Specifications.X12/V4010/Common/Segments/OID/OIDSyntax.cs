using EdiMorph.Rules;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.Syntax;

namespace EdiMorph.Specifications.X12.V4010.Common.Segments.OID;

public sealed class OIDSyntax : IRuleValidator<X12Segment>
{
    public const string RuleId = "X12_V4010_OID_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12Segment subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        return SegmentSyntax.Check(
            RuleId,
            "OID",
            subject,
            cancellationToken,
            new SyntaxNote('R', "01", "02"),
            new SyntaxNote('C', "03", "02"),
            new SyntaxNote('P', "04", "05"),
            new SyntaxNote('P', "06", "07"),
            new SyntaxNote('P', "08", "09"));
    }
}