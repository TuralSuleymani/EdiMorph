using EdiMorph.Rules;
using EdiMorph.Specifications.X12.V4010.Common.Envelope;
using EdiMorph.Specifications.X12.V4010.Common.Segments.AT5;
using EdiMorph.Specifications.X12.V4010.Common.Segments.AT8;
using EdiMorph.Specifications.X12.V4010.Common.Segments.G62;
using EdiMorph.Specifications.X12.V4010.Common.Segments.ISA;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L11;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L5;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LAD;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LEP;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LFH;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH2;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH4;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH6;
using EdiMorph.Specifications.X12.V4010.Common.Segments.MEA;
using EdiMorph.Specifications.X12.V4010.Common.Segments.MS3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.N1;
using EdiMorph.Specifications.X12.V4010.Common.Segments.N7;
using EdiMorph.Specifications.X12.V4010.Common.Segments.OID;
using EdiMorph.Specifications.X12.V4010.Common.Segments.PLD;
using EdiMorph.Specifications.X12.V4010.Common.Segments.S5;
using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.Specifications.X12.V4010;

public static class X12V4010Rules
{
    public static void Register(IRuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Register<EnvelopeIntegrity, X12Envelope>(registry, EnvelopeIntegrity.RuleId, RuleScope.Envelope);
        Register<IsaAuthorizationSemantics, X12Segment>(registry, IsaAuthorizationSemantics.RuleId, RuleScope.Segment);
        Register<L11Syntax, X12Segment>(registry, L11Syntax.RuleId, RuleScope.Segment);
        Register<G62Syntax, X12Segment>(registry, G62Syntax.RuleId, RuleScope.Segment);
        Register<MS3Syntax, X12Segment>(registry, MS3Syntax.RuleId, RuleScope.Segment);
        Register<AT5Syntax, X12Segment>(registry, AT5Syntax.RuleId, RuleScope.Segment);
        Register<PLDSyntax, X12Segment>(registry, PLDSyntax.RuleId, RuleScope.Segment);
        Register<LH6Syntax, X12Segment>(registry, LH6Syntax.RuleId, RuleScope.Segment);
        Register<N1Syntax, X12Segment>(registry, N1Syntax.RuleId, RuleScope.Segment);
        Register<N7Syntax, X12Segment>(registry, N7Syntax.RuleId, RuleScope.Segment);
        Register<S5Syntax, X12Segment>(registry, S5Syntax.RuleId, RuleScope.Segment);
        Register<AT8Syntax, X12Segment>(registry, AT8Syntax.RuleId, RuleScope.Segment);
        Register<LADSyntax, X12Segment>(registry, LADSyntax.RuleId, RuleScope.Segment);
        Register<L5Syntax, X12Segment>(registry, L5Syntax.RuleId, RuleScope.Segment);
        Register<LH2Syntax, X12Segment>(registry, LH2Syntax.RuleId, RuleScope.Segment);
        Register<LH3Syntax, X12Segment>(registry, LH3Syntax.RuleId, RuleScope.Segment);
        Register<LFHSyntax, X12Segment>(registry, LFHSyntax.RuleId, RuleScope.Segment);
        Register<LEPSyntax, X12Segment>(registry, LEPSyntax.RuleId, RuleScope.Segment);
        Register<LH4Syntax, X12Segment>(registry, LH4Syntax.RuleId, RuleScope.Segment);
        Register<OIDSyntax, X12Segment>(registry, OIDSyntax.RuleId, RuleScope.Segment);
        Register<L3Syntax, X12Segment>(registry, L3Syntax.RuleId, RuleScope.Segment);
        Register<MEASyntax, X12Segment>(registry, MEASyntax.RuleId, RuleScope.Segment);
        Register<X204Syntax, X12TransactionSet>(
            registry,
            X204Syntax.RuleId,
            RuleScope.Transaction,
            transaction: "204");
    }

    private static void Register<TValidator, TSubject>(
        IRuleRegistry registry,
        string ruleId,
        RuleScope scope,
        string? transaction = null)
        where TValidator : IRuleValidator<TSubject>
    {
        registry.Register(RuleHandlerDescriptor.Create<TValidator, TSubject>(
            ruleId,
            scope,
            mandatory: true,
            format: "X12",
            version: "004010",
            transaction: transaction));
    }
}
