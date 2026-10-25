namespace EdiMorph.X12;

internal abstract record Placement(string NodeId);

internal sealed record SegmentPlacement(string NodeId, ParsedSegment Segment) : Placement(NodeId);

internal sealed record LoopPlacement(string NodeId, string? LoopId, IReadOnlyList<Placement> Children) : Placement(NodeId);
