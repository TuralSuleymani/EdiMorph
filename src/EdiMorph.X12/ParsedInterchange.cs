using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

internal sealed record ParsedSegment(X12Segment Segment, string? DefinitionId, string OccurrenceId);

internal sealed record ParsedTransaction(
    X12TransactionSet Transaction,
    IReadOnlyList<ParsedSegment> Segments,
    IReadOnlyList<Placement> Placements);

internal sealed class ParsedInterchange
{
    public required X12Envelope Envelope { get; init; }

    public required IReadOnlyList<ParsedSegment> Segments { get; init; }

    public required IReadOnlyList<ParsedTransaction> Transactions { get; init; }
}
