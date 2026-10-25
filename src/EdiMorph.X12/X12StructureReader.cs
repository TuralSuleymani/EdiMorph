using System.Globalization;
using EdiMorph.Diagnostics;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

internal static class X12StructureReader
{
    private const string InterchangeControlPosition = "13";

    private const string FirstElement = "01";

    private const string SecondElement = "02";

    private const string GroupControlPosition = "06";

    private const string VersionPosition = "08";

    public static ParsedInterchange Read(
        CompiledSpecification specification,
        IReadOnlyList<X12Token> tokens,
        List<SpecificationDiagnostic> diagnostics)
    {
        var envelope = specification.Envelope ?? throw new InvalidOperationException("A compiled envelope is required.");
        var transaction = specification.Transaction ?? throw new InvalidOperationException("A compiled transaction is required.");
        var header = Require(specification, envelope.HeaderSegmentRef, "interchange header");
        var trailer = Require(specification, envelope.TrailerSegmentRef, "interchange trailer");
        var groupHeader = Require(specification, envelope.GroupHeaderSegmentRef, "functional group header");
        var groupTrailer = Require(specification, envelope.GroupTrailerSegmentRef, "functional group trailer");
        var transactionHeader = transaction.Heading.Select(StartTag).FirstOrDefault(tag => tag is not null)
            ?? throw new InvalidOperationException("The compiled transaction has no header segment.");

        var cursor = new Cursor(tokens);
        var segments = new List<ParsedSegment>();
        var headerSegment = AcceptEnvelope(specification, cursor, header, diagnostics, segments, header.Tag);
        var groups = new List<X12FunctionalGroup>();
        var transactions = new List<ParsedTransaction>();
        while (cursor.Current is not null && !string.Equals(cursor.Current.Tag, trailer.Tag, StringComparison.Ordinal))
        {
            groups.Add(ReadGroup(
                specification,
                transaction,
                cursor,
                groupHeader,
                groupTrailer,
                trailer.Tag,
                transactionHeader,
                diagnostics,
                segments,
                transactions));
        }

        X12Segment? trailerSegment = null;
        if (cursor.Current is not null && string.Equals(cursor.Current.Tag, trailer.Tag, StringComparison.Ordinal))
        {
            trailerSegment = AcceptEnvelope(specification, cursor, trailer, diagnostics, segments, trailer.Tag);
        }
        else
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Segment '{trailer.Tag}' is required.",
                trailer.Tag));
        }

        while (cursor.Current is not null)
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Segment '{cursor.Current.Tag}' is not allowed after {trailer.Tag}.",
                cursor.Current.Tag));
            cursor.Index++;
        }

        if (groups.Count == 0)
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                "At least one functional group is required.",
                groupHeader.Tag));
        }

        return new ParsedInterchange
        {
            Envelope = new X12Envelope(
                Value(headerSegment, InterchangeControlPosition),
                Value(trailerSegment, SecondElement),
                Count(trailerSegment, FirstElement),
                groups),
            Segments = segments,
            Transactions = transactions
        };
    }

    private static X12FunctionalGroup ReadGroup(
        CompiledSpecification specification,
        CompiledTransaction transaction,
        Cursor cursor,
        CompiledSegment groupHeader,
        CompiledSegment groupTrailer,
        string interchangeTrailer,
        string transactionHeader,
        List<SpecificationDiagnostic> diagnostics,
        List<ParsedSegment> segments,
        List<ParsedTransaction> transactions)
    {
        X12Segment? header = null;
        if (cursor.Current is not null && string.Equals(cursor.Current.Tag, groupHeader.Tag, StringComparison.Ordinal))
        {
            header = AcceptEnvelope(specification, cursor, groupHeader, diagnostics, segments, groupHeader.Tag);
            var identifier = Value(header, FirstElement);
            if (transaction.FunctionalGroup is not null
                && !string.Equals(identifier, transaction.FunctionalGroup, StringComparison.Ordinal))
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.Structure,
                    $"Functional group {groupHeader.Tag}{FirstElement} must be {transaction.FunctionalGroup}.",
                    groupHeader.Tag + FirstElement));
            }

            var version = Value(header, VersionPosition);
            if (!string.Equals(version, specification.Identity.Version, StringComparison.Ordinal))
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.ElementConstraint,
                    $"{groupHeader.Tag}{VersionPosition} must equal version {specification.Identity.Version}.",
                    groupHeader.Tag + VersionPosition));
            }
        }
        else
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Segment '{groupHeader.Tag}' is required.",
                groupHeader.Tag));
        }

        var sets = new List<X12TransactionSet>();
        while (cursor.Current is not null
            && !string.Equals(cursor.Current.Tag, groupTrailer.Tag, StringComparison.Ordinal)
            && !string.Equals(cursor.Current.Tag, interchangeTrailer, StringComparison.Ordinal))
        {
            if (string.Equals(cursor.Current.Tag, transactionHeader, StringComparison.Ordinal))
            {
                var parsed = ReadTransaction(specification, transaction, cursor, groupTrailer.Tag, interchangeTrailer, diagnostics);
                sets.Add(parsed.Transaction);
                transactions.Add(parsed);
                segments.AddRange(parsed.Segments);
            }
            else
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.Structure,
                    $"Segment '{cursor.Current.Tag}' is not allowed at this position.",
                    cursor.Current.Tag));
                cursor.Index++;
            }
        }

        X12Segment? trailer = null;
        if (cursor.Current is not null && string.Equals(cursor.Current.Tag, groupTrailer.Tag, StringComparison.Ordinal))
        {
            trailer = AcceptEnvelope(specification, cursor, groupTrailer, diagnostics, segments, groupTrailer.Tag);
        }
        else
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Segment '{groupTrailer.Tag}' is required.",
                groupTrailer.Tag));
        }

        return new X12FunctionalGroup(
            Value(header, GroupControlPosition),
            Value(trailer, SecondElement),
            Count(trailer, FirstElement),
            sets);
    }

    private static ParsedTransaction ReadTransaction(
        CompiledSpecification specification,
        CompiledTransaction transaction,
        Cursor cursor,
        string groupTrailer,
        string interchangeTrailer,
        List<SpecificationDiagnostic> diagnostics)
    {
        var accepted = new List<ParsedSegment>();
        var placements = new List<Placement>();
        var stops = new HashSet<string>(StringComparer.Ordinal) { groupTrailer, interchangeTrailer };
        var nodes = transaction.Heading.Concat(transaction.Detail).Concat(transaction.Summary).ToArray();
        ParseSequence(specification, nodes, stops, cursor, accepted, placements, diagnostics);

        var segments = accepted.Select(segment => segment.Segment).ToArray();
        var header = segments.FirstOrDefault(segment => string.Equals(segment.Tag, StartTag(transaction.Heading[0]), StringComparison.Ordinal));
        var trailer = segments.LastOrDefault(segment =>
            transaction.Summary.Count > 0
            && string.Equals(segment.Tag, StartTag(transaction.Summary[^1]), StringComparison.Ordinal));
        var code = Value(header, FirstElement);
        if (!string.Equals(code, transaction.Code, StringComparison.Ordinal))
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Transaction set {header?.Tag}{FirstElement} must be {transaction.Code}.",
                header?.Tag + FirstElement));
        }

        return new ParsedTransaction(
            new X12TransactionSet(
                Value(header, SecondElement),
                Value(trailer, SecondElement),
                Count(trailer, FirstElement),
                segments.Length,
                segments),
            accepted,
            placements);
    }

    private static void ParseSequence(
        CompiledSpecification specification,
        IReadOnlyList<CompiledNode> nodes,
        IReadOnlySet<string> parentStops,
        Cursor cursor,
        List<ParsedSegment> accepted,
        List<Placement> placements,
        List<SpecificationDiagnostic> diagnostics)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            var following = Following(nodes, index + 1, parentStops);
            var count = 0;
            while (true)
            {
                var current = cursor.Current;
                if (current is null)
                {
                    break;
                }

                if (Starts(node, current) && (node.MaxOccurs is null || count < node.MaxOccurs))
                {
                    if (IsLoop(node))
                    {
                        var childStops = new HashSet<string>(following, StringComparer.Ordinal);
                        var start = StartTag(node);
                        if (start is not null)
                        {
                            childStops.Add(start);
                        }

                        var children = new List<Placement>();
                        ParseSequence(specification, node.Children, childStops, cursor, accepted, children, diagnostics);
                        placements.Add(new LoopPlacement(node.Id, node.LoopId, children));
                    }
                    else
                    {
                        AcceptNode(specification, node, cursor, accepted, placements, diagnostics);
                    }

                    count++;
                    continue;
                }

                if (following.Contains(current.Tag))
                {
                    break;
                }

                if (Starts(node, current) && node.MaxOccurs is int maximum)
                {
                    diagnostics.Add(ReaderDiagnostics.Error(
                        DiagnosticCodes.Structure,
                        $"Segment '{current.Tag}' repeats more than {maximum} times.",
                        node.Id));
                }
                else
                {
                    diagnostics.Add(ReaderDiagnostics.Error(
                        DiagnosticCodes.Structure,
                        $"Segment '{current.Tag}' is not allowed at this position.",
                        current.Tag));
                }

                var extension = new ParsedSegment(new X12Segment(current.Tag, current.Elements), null, current.Tag);
                accepted.Add(extension);
                placements.Add(new SegmentPlacement(current.Tag, extension));
                cursor.Index++;
            }

            if (count < node.MinOccurs)
            {
                var name = IsLoop(node) ? node.LoopId ?? node.Id : node.Segment ?? node.Id;
                var kind = IsLoop(node) ? "Loop" : "Segment";
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.Structure,
                    $"{kind} '{name}' is required.",
                    node.Id));
            }
        }
    }

    private static X12Segment? AcceptEnvelope(
        CompiledSpecification specification,
        Cursor cursor,
        CompiledSegment definition,
        List<SpecificationDiagnostic> diagnostics,
        List<ParsedSegment> accepted,
        string occurrenceId)
    {
        if (cursor.Current is null || !string.Equals(cursor.Current.Tag, definition.Tag, StringComparison.Ordinal))
        {
            return null;
        }

        var segment = ElementConstraintChecker.Check(cursor.Current, definition, diagnostics);
        accepted.Add(new ParsedSegment(segment, definition.Id, occurrenceId));
        cursor.Index++;
        return segment;
    }

    private static void AcceptNode(
        CompiledSpecification specification,
        CompiledNode node,
        Cursor cursor,
        List<ParsedSegment> accepted,
        List<Placement> placements,
        List<SpecificationDiagnostic> diagnostics)
    {
        var token = cursor.Current ?? throw new InvalidOperationException("A structure node was accepted without a segment.");
        cursor.Index++;
        if (node.SegmentRef is null || !specification.Segments.TryGetValue(node.SegmentRef, out var definition))
        {
            var undefined = new ParsedSegment(new X12Segment(token.Tag, token.Elements), null, node.Id);
            accepted.Add(undefined);
            placements.Add(new SegmentPlacement(node.Id, undefined));
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.Structure,
                $"Segment '{token.Tag}' has no compiled definition.",
                node.Id));
            return;
        }

        var segment = ElementConstraintChecker.Check(token, definition, diagnostics);
        var parsed = new ParsedSegment(segment, definition.Id, node.Id);
        accepted.Add(parsed);
        placements.Add(new SegmentPlacement(node.Id, parsed));
    }

    private static HashSet<string> Following(IReadOnlyList<CompiledNode> nodes, int start, IReadOnlySet<string> parentStops)
    {
        var tags = new HashSet<string>(parentStops, StringComparer.Ordinal);
        for (var index = start; index < nodes.Count; index++)
        {
            var tag = StartTag(nodes[index]);
            if (tag is not null)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private static bool Starts(CompiledNode node, X12Token token)
    {
        return string.Equals(StartTag(node), token.Tag, StringComparison.Ordinal);
    }

    private static string? StartTag(CompiledNode node)
    {
        if (!IsLoop(node))
        {
            return node.Segment;
        }

        foreach (var child in node.Children)
        {
            var tag = StartTag(child);
            if (tag is not null)
            {
                return tag;
            }
        }

        return null;
    }

    private static bool IsLoop(CompiledNode node)
    {
        return string.Equals(node.Kind, "loop", StringComparison.Ordinal);
    }

    private static CompiledSegment Require(CompiledSpecification specification, string segmentRef, string role)
    {
        if (!specification.Segments.TryGetValue(segmentRef, out var segment))
        {
            throw new InvalidOperationException($"The compiled envelope is missing the {role}.");
        }

        return segment;
    }

    private static string? Value(X12Segment? segment, string position)
    {
        return segment is not null && segment.Elements.TryGetValue(position, out var value) ? value : null;
    }

    private static int? Count(X12Segment? segment, string position)
    {
        var value = Value(segment, position);
        return value is not null && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : null;
    }

    private sealed class Cursor(IReadOnlyList<X12Token> tokens)
    {
        public int Index { get; set; }

        public X12Token? Current => Index < tokens.Count ? tokens[Index] : null;
    }
}
