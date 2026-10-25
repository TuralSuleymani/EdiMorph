using EdiMorph.Diagnostics;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

public sealed class X12ReadResult
{
    public X12ReadResult(X12Envelope? envelope, IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Envelope = envelope;
        Diagnostics = diagnostics;
    }

    public X12Envelope? Envelope { get; }

    public IReadOnlyList<SpecificationDiagnostic> Diagnostics { get; }

    public bool IsSuccess => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
