using EdiMorph;
using EdiMorph.Diagnostics;
using EdiMorph.Rules;

namespace EdiMorph.Specifications;

public sealed record RuleOrigin(string DocumentId, string SourcePath, string Layer, string Change);

public sealed record HandlerBinding(string RuleId, RuleScope Scope, bool Mandatory, string SourceDocumentId);

public sealed record CompiledElement(
    string Key,
    string? Reference,
    string Usage,
    int? MinLength,
    int? MaxLength,
    IReadOnlyList<string>? AllowedValues,
    IReadOnlyList<RuleOrigin> Origins);

public sealed record CompiledSegment(
    string Id,
    string Tag,
    IReadOnlyDictionary<string, CompiledElement> Elements,
    IReadOnlyList<RuleOrigin> Origins);

public sealed record CompiledNode(
    string Id,
    string Kind,
    string? Segment,
    string? SegmentRef,
    string Usage,
    int MinOccurs,
    int? MaxOccurs,
    string? LoopId,
    IReadOnlyList<CompiledNode> Children,
    IReadOnlyList<RuleOrigin> Origins);

public sealed record CompiledEnvelope(
    string Id,
    string HeaderSegmentRef,
    string TrailerSegmentRef,
    string GroupHeaderSegmentRef,
    string GroupTrailerSegmentRef,
    IReadOnlyList<RuleOrigin> Origins);

public sealed record CompiledTransaction(
    string Id,
    string Code,
    string? FunctionalGroup,
    IReadOnlyList<CompiledNode> Heading,
    IReadOnlyList<CompiledNode> Detail,
    IReadOnlyList<CompiledNode> Summary);

public sealed record SpecificationIdentity(
    string Format,
    string Version,
    string? Transaction,
    string? Provider,
    string? ProfileRevision,
    ComplianceMode Compliance);

public sealed class CompiledSpecification
{
    internal CompiledSpecification(
        SpecificationIdentity identity,
        string fingerprint,
        string? baseFingerprint,
        string? syntaxId,
        CompiledEnvelope? envelope,
        IReadOnlyDictionary<string, CompiledSegment> segments,
        CompiledTransaction? transaction,
        IReadOnlyList<HandlerBinding> handlers,
        IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        Identity = identity;
        Fingerprint = fingerprint;
        BaseFingerprint = baseFingerprint;
        SyntaxId = syntaxId;
        Envelope = envelope;
        Segments = segments;
        Transaction = transaction;
        Handlers = handlers;
        Diagnostics = diagnostics;
    }

    public SpecificationIdentity Identity { get; }

    public string Fingerprint { get; }

    public string? BaseFingerprint { get; }

    public string? SyntaxId { get; }

    public CompiledEnvelope? Envelope { get; }

    public IReadOnlyDictionary<string, CompiledSegment> Segments { get; }

    public CompiledTransaction? Transaction { get; }

    public IReadOnlyList<HandlerBinding> Handlers { get; }

    public IReadOnlyList<SpecificationDiagnostic> Diagnostics { get; }

    public bool IsSuccess => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
