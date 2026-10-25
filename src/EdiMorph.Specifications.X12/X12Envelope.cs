namespace EdiMorph.Specifications.X12;

public sealed class X12TransactionSet
{
    public X12TransactionSet(
        string? headerControlNumber,
        string? trailerControlNumber,
        int? declaredSegmentCount,
        int segmentCount,
        IReadOnlyList<X12Segment>? segments = null)
    {
        HeaderControlNumber = headerControlNumber;
        TrailerControlNumber = trailerControlNumber;
        DeclaredSegmentCount = declaredSegmentCount;
        SegmentCount = segmentCount;
        Segments = segments ?? [];
    }

    public string? HeaderControlNumber { get; }

    public string? TrailerControlNumber { get; }

    public int? DeclaredSegmentCount { get; }

    public int SegmentCount { get; }

    public IReadOnlyList<X12Segment> Segments { get; }
}

public sealed class X12FunctionalGroup
{
    public X12FunctionalGroup(
        string? headerControlNumber,
        string? trailerControlNumber,
        int? declaredTransactionCount,
        IReadOnlyList<X12TransactionSet> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        HeaderControlNumber = headerControlNumber;
        TrailerControlNumber = trailerControlNumber;
        DeclaredTransactionCount = declaredTransactionCount;
        Transactions = transactions;
    }

    public string? HeaderControlNumber { get; }

    public string? TrailerControlNumber { get; }

    public int? DeclaredTransactionCount { get; }

    public IReadOnlyList<X12TransactionSet> Transactions { get; }
}

public sealed class X12Envelope
{
    public X12Envelope(
        string? headerControlNumber,
        string? trailerControlNumber,
        int? declaredGroupCount,
        IReadOnlyList<X12FunctionalGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        HeaderControlNumber = headerControlNumber;
        TrailerControlNumber = trailerControlNumber;
        DeclaredGroupCount = declaredGroupCount;
        Groups = groups;
    }

    public string? HeaderControlNumber { get; }

    public string? TrailerControlNumber { get; }

    public int? DeclaredGroupCount { get; }

    public IReadOnlyList<X12FunctionalGroup> Groups { get; }
}
