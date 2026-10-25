using EdiMorph.Diagnostics;

namespace EdiMorph.X12;

internal static class ReaderDiagnostics
{
    public static SpecificationDiagnostic Error(string code, string message, string? target = null)
    {
        return new SpecificationDiagnostic(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            message,
            target: target);
    }
}
