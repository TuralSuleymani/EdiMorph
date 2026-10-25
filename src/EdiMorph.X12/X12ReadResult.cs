using EdiMorph.Diagnostics;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.X12;

public sealed class X12ReadResult
{
    public X12ReadResult(
        X12Envelope? envelope,
        IReadOnlyList<Transaction204> transactions,
        IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Envelope = envelope;
        Transactions = transactions;
        Diagnostics = diagnostics;
    }

    public X12Envelope? Envelope { get; }

    public IReadOnlyList<Transaction204> Transactions { get; }

    public IReadOnlyList<SpecificationDiagnostic> Diagnostics { get; }

    public bool IsSuccess => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
