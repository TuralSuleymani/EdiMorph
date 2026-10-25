using EdiMorph.Diagnostics;

namespace EdiMorph.Abstractions.Tests.Factories;

internal static class SpecificationDiagnosticFactory
{
    public static SpecificationDiagnostic MissingHandler(
        string ruleId = TestDefaults.EnvelopeIntegrityRuleId)
    {
        return new SpecificationDiagnostic(
            DiagnosticCodes.HandlerMissing,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            TestDefaults.MissingHandlerMessage,
            ruleId);
    }
}
