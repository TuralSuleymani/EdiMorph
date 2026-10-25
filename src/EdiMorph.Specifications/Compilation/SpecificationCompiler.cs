using System.Security.Cryptography;
using System.Text;
using EdiMorph;
using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications.Authoring;

namespace EdiMorph.Specifications;

public sealed class SpecificationCompiler
{
    public const string CompilerVersion = "1";

    private readonly SpecificationCatalog _catalog;
    private readonly IRuleRegistry _rules;
    private readonly SpecificationResolver _resolver;
    private readonly object _gate = new();
    private readonly Dictionary<string, CompiledSpecification> _base = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CompiledSpecification> _effective = new(StringComparer.Ordinal);

    public SpecificationCompiler(SpecificationCatalog catalog, IRuleRegistry rules)
        : this(catalog, rules, new SpecificationResolver())
    {
    }

    internal SpecificationCompiler(SpecificationCatalog catalog, IRuleRegistry rules, SpecificationResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(resolver);
        _catalog = catalog;
        _rules = rules;
        _resolver = resolver;
    }

    public ValueTask<CompiledSpecification> CompileAsync(
        SpecificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Compile(request));
    }

    private CompiledSpecification Compile(SpecificationRequest request)
    {
        var authoring = AuthoringErrors();
        if (authoring.Count > 0)
        {
            return Failed(request, ComplianceMode.StandardSubset, authoring);
        }

        var baseRequest = new SpecificationRequest(request.Format, request.Version, request.Transaction);
        var basePlan = _resolver.Resolve(_catalog, baseRequest);
        var baseKey = CacheKey(baseRequest, basePlan.BaseEntries, _rules.Revision);
        var baseSpec = GetOrCompileBase(baseKey, baseRequest, basePlan);
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return baseSpec;
        }

        if (!baseSpec.IsSuccess)
        {
            return baseSpec;
        }

        var plan = _resolver.Resolve(_catalog, request);
        var profile = plan.OverlayEntries.FirstOrDefault(entry => entry.Kind == "provider-profile");
        var compliance = ParseCompliance(profile?.Document.Compliance);
        var effectiveKey = string.Join(
            '|',
            baseSpec.Fingerprint,
            request.Provider,
            request.ProfileRevision,
            profile?.ContentHash,
            string.Join(',', plan.OverlayEntries.Select(entry => entry.ContentHash)),
            _rules.Revision,
            CompilerVersion);

        lock (_gate)
        {
            if (_effective.TryGetValue(effectiveKey, out var cached))
            {
                return cached;
            }
        }

        var diagnostics = plan.Diagnostics.ToList();
        IncludeDocumentErrors(plan.OverlayEntries, diagnostics);
        var composition = SpecificationComposer.Apply(baseSpec, plan.OverlayEntries, compliance, _rules, diagnostics);
        var spec = Create(
            request,
            compliance,
            baseSpec.Fingerprint,
            plan.OverlayEntries,
            composition,
            diagnostics);
        if (spec.IsSuccess)
        {
            lock (_gate)
            {
                _effective[effectiveKey] = spec;
            }
        }

        return spec;
    }

    private CompiledSpecification GetOrCompileBase(string key, SpecificationRequest request, ResolutionPlan plan)
    {
        lock (_gate)
        {
            if (_base.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var diagnostics = plan.Diagnostics.ToList();
        IncludeDocumentErrors(plan.BaseEntries, diagnostics);
        var composition = SpecificationComposer.ComposeBase(plan.BaseEntries, _rules, diagnostics);
        var spec = Create(request, ComplianceMode.StandardSubset, null, plan.BaseEntries, composition, diagnostics);
        if (!spec.IsSuccess)
        {
            return spec;
        }

        lock (_gate)
        {
            if (_base.TryGetValue(key, out var cached))
            {
                return cached;
            }

            _base[key] = spec;
            return spec;
        }
    }

    private List<SpecificationDiagnostic> AuthoringErrors()
    {
        var diagnostics = new List<SpecificationDiagnostic>();
        var entries = _catalog.Entries;
        FlagSameScopePairs(entries, diagnostics);
        FlagMissingParents(entries, diagnostics);
        FlagCycles(entries, diagnostics);
        foreach (var entry in entries)
        {
            if (entry.Document.Errors.Any(error => error.Code == DiagnosticCodes.UnsupportedRuleFormat))
            {
                diagnostics.AddRange(entry.Document.Errors.Where(error => error.Code == DiagnosticCodes.UnsupportedRuleFormat));
            }
        }

        return diagnostics;
    }

    private static void FlagSameScopePairs(IReadOnlyList<CatalogEntry> entries, List<SpecificationDiagnostic> diagnostics)
    {
        var groups = new Dictionary<string, List<CatalogEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (ScopeKey(entry.Path) is not string key)
            {
                continue;
            }

            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add(entry);
        }

        foreach (var group in groups.Values)
        {
            var hasRule = group.Any(entry => entry.Path.EndsWith(".rule", StringComparison.OrdinalIgnoreCase)
                && !entry.Path.EndsWith(".override.rule", StringComparison.OrdinalIgnoreCase));
            var hasOverride = group.Any(entry => entry.Path.EndsWith(".override.rule", StringComparison.OrdinalIgnoreCase));
            if (!hasRule || !hasOverride)
            {
                continue;
            }

            var first = group[0];
            diagnostics.Add(Standard(
                DiagnosticCodes.AmbiguousDefinition,
                $"'{DirectoryOf(first.Path)}' contains both a .rule file and a .override.rule file.",
                first.Path));
        }
    }

    private static void FlagMissingParents(IReadOnlyList<CatalogEntry> entries, List<SpecificationDiagnostic> diagnostics)
    {
        var ids = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var extends = entry.Document.Extends;
            if (extends is null)
            {
                if (entry.Kind is "segment-override" or "transaction-override")
                {
                    diagnostics.Add(Standard(
                        DiagnosticCodes.ReferencedSegmentMissing,
                        $"Override '{entry.Id}' does not name a parent rule.",
                        entry.Path,
                        entry.Id));
                }

                continue;
            }

            if (!ids.Contains(extends))
            {
                diagnostics.Add(Standard(
                    DiagnosticCodes.ReferencedSegmentMissing,
                    $"Parent rule '{extends}' was not found.",
                    entry.Path,
                    extends));
            }
        }
    }

    private static void FlagCycles(IReadOnlyList<CatalogEntry> entries, List<SpecificationDiagnostic> diagnostics)
    {
        var edges = entries
            .Where(entry => entry.Document.Extends is not null)
            .GroupBy(entry => entry.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Document.Extends!, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in edges.Keys)
        {
            if (!HasCycle(id, edges, visiting, visited))
            {
                continue;
            }

            var entry = entries.First(item => item.Id == id);
            diagnostics.Add(Standard(
                DiagnosticCodes.CircularInheritance,
                $"Rule '{id}' extends itself through its parent chain.",
                entry.Path,
                id));
            return;
        }
    }

    private static bool HasCycle(
        string id,
        Dictionary<string, string> edges,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (!edges.TryGetValue(id, out var parent) || visited.Contains(id))
        {
            return false;
        }

        if (!visiting.Add(id))
        {
            return true;
        }

        var cycle = HasCycle(parent, edges, visiting, visited);
        visiting.Remove(id);
        visited.Add(id);
        return cycle;
    }

    private static CompiledSpecification Create(
        SpecificationRequest request,
        ComplianceMode compliance,
        string? baseFingerprint,
        IReadOnlyList<CatalogEntry> sources,
        Composition composition,
        IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        var identity = new SpecificationIdentity(
            request.Format,
            request.Version,
            request.Transaction,
            request.Provider,
            request.ProfileRevision,
            compliance);
        var material = string.Join(
            '|',
            CompilerVersion,
            identity.Format,
            identity.Version,
            identity.Transaction,
            identity.Provider,
            identity.ProfileRevision,
            identity.Compliance,
            baseFingerprint,
            string.Join(',', sources.Select(source => source.Id + "@" + source.ContentHash)),
            string.Join(',', composition.Handlers.Select(handler => handler.RuleId)));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return new CompiledSpecification(
            identity,
            fingerprint,
            baseFingerprint,
            composition.SyntaxId,
            composition.Envelope,
            composition.Segments,
            composition.Transaction,
            composition.Handlers,
            diagnostics.ToArray());
    }

    private static CompiledSpecification Failed(
        SpecificationRequest request,
        ComplianceMode compliance,
        IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        return Create(request, compliance, null, [], Composition.Empty, diagnostics);
    }

    private static void IncludeDocumentErrors(IEnumerable<CatalogEntry> entries, List<SpecificationDiagnostic> diagnostics)
    {
        foreach (var entry in entries)
        {
            diagnostics.AddRange(entry.Document.Errors);
        }
    }

    private static ComplianceMode ParseCompliance(string? compliance)
    {
        return compliance switch
        {
            "partner-dialect" => ComplianceMode.PartnerDialect,
            _ => ComplianceMode.StandardSubset
        };
    }

    private static string CacheKey(SpecificationRequest request, IReadOnlyList<CatalogEntry> entries, string registryRevision)
    {
        return string.Join(
            '|',
            CompilerVersion,
            request.Format,
            request.Version,
            request.Transaction,
            registryRevision,
            string.Join(',', entries.Select(entry => entry.ContentHash)));
    }

    private static string? ScopeKey(string path)
    {
        var file = path.Replace('\\', '/');
        var slash = file.LastIndexOf('/');
        var directory = slash < 0 ? "" : file[..slash];
        var name = slash < 0 ? file : file[(slash + 1)..];
        if (name.EndsWith(".override.rule", StringComparison.OrdinalIgnoreCase))
        {
            return directory + "/" + name[..^".override.rule".Length];
        }

        if (name.EndsWith(".rule", StringComparison.OrdinalIgnoreCase))
        {
            return directory + "/" + name[..^".rule".Length];
        }

        return null;
    }

    private static string DirectoryOf(string path)
    {
        var normalized = path.Replace('\\', '/');
        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? "" : normalized[..slash];
    }

    private static SpecificationDiagnostic Standard(string code, string message, string? path = null, string? target = null)
    {
        return new SpecificationDiagnostic(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            message,
            sourcePath: path,
            target: target);
    }
}

internal sealed record Composition(
    string? SyntaxId,
    CompiledEnvelope? Envelope,
    IReadOnlyDictionary<string, CompiledSegment> Segments,
    CompiledTransaction? Transaction,
    IReadOnlyList<HandlerBinding> Handlers)
{
    public static Composition Empty { get; } = new(null, null, new Dictionary<string, CompiledSegment>(), null, []);
}

internal static class SpecificationComposer
{
    public static Composition ComposeBase(
        IReadOnlyList<CatalogEntry> documents,
        IRuleRegistry rules,
        List<SpecificationDiagnostic> diagnostics)
    {
        var segments = new Dictionary<string, CompiledSegment>(StringComparer.Ordinal);
        string? syntaxId = null;
        CompiledEnvelope? envelope = null;
        CompiledTransaction? transaction = null;
        var bindings = new List<HandlerBinding>();

        foreach (var entry in documents)
        {
            BindCustomRules(entry, rules, bindings, diagnostics, DiagnosticCategory.Standard);
            switch (entry.Kind)
            {
                case "syntax":
                    syntaxId = entry.Id;
                    break;
                case "segment":
                    segments[entry.Id] = CreateSegment(entry);
                    break;
            }
        }

        foreach (var entry in documents)
        {
            switch (entry.Kind)
            {
                case "envelope":
                    envelope = CreateEnvelope(entry, segments, diagnostics);
                    break;
                case "transaction":
                    transaction = CreateTransaction(entry, segments, diagnostics);
                    break;
            }
        }

        return new Composition(syntaxId, envelope, segments, transaction, bindings);
    }

    public static Composition Apply(
        CompiledSpecification baseline,
        IReadOnlyList<CatalogEntry> overlays,
        ComplianceMode compliance,
        IRuleRegistry rules,
        List<SpecificationDiagnostic> diagnostics)
    {
        var segments = new Dictionary<string, CompiledSegment>(baseline.Segments, StringComparer.Ordinal);
        var transaction = baseline.Transaction;
        var bindings = baseline.Handlers.ToList();
        var profile = overlays.FirstOrDefault(entry => entry.Kind == "provider-profile");
        if (profile is not null)
        {
            BindCustomRules(profile, rules, bindings, diagnostics, DiagnosticCategory.ProviderPolicy);
            if (!IsKnownCompliance(profile.Document.Compliance))
            {
                diagnostics.Add(Policy(
                    DiagnosticCodes.IllegalOverride,
                    $"Provider profile '{profile.Id}' has unknown compliance '{profile.Document.Compliance}'.",
                    profile.Path));
            }
        }

        foreach (var entry in overlays.Where(item => item.Kind is "segment-override" or "transaction-override"))
        {
            ApplyOverride(entry, compliance, segments, ref transaction, rules, bindings, diagnostics);
        }

        return new Composition(baseline.SyntaxId, baseline.Envelope, segments, transaction, bindings);
    }

    private static void ApplyOverride(
        CatalogEntry entry,
        ComplianceMode compliance,
        Dictionary<string, CompiledSegment> segments,
        ref CompiledTransaction? transaction,
        IRuleRegistry rules,
        List<HandlerBinding> bindings,
        List<SpecificationDiagnostic> diagnostics)
    {
        foreach (var operation in entry.Document.Operations)
        {
            switch (operation.Name)
            {
                case "restrictAllowedValues":
                    RestrictValues(entry, operation, segments, diagnostics);
                    break;
                case "restrictMaxLength":
                    RestrictLength(entry, operation, segments, diagnostics);
                    break;
                case "changeUsage":
                    ChangeUsage(entry, operation, segments, diagnostics, allowLoosen: false);
                    break;
                case "loosenUsage":
                    LoosenUsage(entry, operation, compliance, segments, diagnostics);
                    break;
                case "widenAllowedValues":
                    WidenValues(entry, operation, compliance, segments, diagnostics);
                    break;
                case "changeOccurrence":
                    transaction = ChangeOccurrence(entry, operation, transaction, diagnostics);
                    break;
                case "addSegment":
                    transaction = AddSegment(entry, operation, compliance, transaction, diagnostics);
                    break;
                case "addRuleBinding":
                    Bind(operation.RuleId, entry, rules, bindings, diagnostics, DiagnosticCategory.ProviderPolicy);
                    break;
                default:
                    diagnostics.Add(Policy(
                        DiagnosticCodes.IllegalOverride,
                        $"Override operation '{operation.Name}' is not supported.",
                        entry.Path,
                        operation.Target));
                    break;
            }
        }
    }

    private static void RestrictValues(
        CatalogEntry entry,
        OverrideOperation operation,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (!TryGetElement(entry, operation, segments, diagnostics, out var segmentId, out var element))
        {
            return;
        }

        var requested = operation.AllowedValues ?? [];
        if (element.AllowedValues is not null
            && requested.Except(element.AllowedValues, StringComparer.Ordinal).Any())
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.StandardLoosened,
                $"Allowed values for '{operation.Target}' are not a subset of the parent list.",
                entry.Path,
                operation.Target));
            return;
        }

        Replace(segments, segmentId, element with
        {
            AllowedValues = requested.ToArray(),
            Origins = Append(element.Origins, entry, operation.Name)
        });
    }

    private static void RestrictLength(
        CatalogEntry entry,
        OverrideOperation operation,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (!TryGetElement(entry, operation, segments, diagnostics, out var segmentId, out var element)
            || operation.MaxLength is not int maxLength)
        {
            return;
        }

        if (element.MaxLength is int parent && maxLength > parent)
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.StandardLoosened,
                $"Maximum length {maxLength} for '{operation.Target}' is wider than {parent}.",
                entry.Path,
                operation.Target));
            return;
        }

        Replace(segments, segmentId, element with
        {
            MaxLength = maxLength,
            Origins = Append(element.Origins, entry, operation.Name)
        });
    }

    private static void ChangeUsage(
        CatalogEntry entry,
        OverrideOperation operation,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics,
        bool allowLoosen)
    {
        if (!TryGetElement(entry, operation, segments, diagnostics, out var segmentId, out var element)
            || operation.Usage is not string usage)
        {
            return;
        }

        if (!allowLoosen && Loosens(element.Usage, usage))
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.StandardLoosened,
                $"Usage of '{operation.Target}' cannot change from {element.Usage} to {usage}.",
                entry.Path,
                operation.Target));
            return;
        }

        Replace(segments, segmentId, element with
        {
            Usage = usage,
            Origins = Append(element.Origins, entry, operation.Name)
        });
    }

    private static void LoosenUsage(
        CatalogEntry entry,
        OverrideOperation operation,
        ComplianceMode compliance,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (!DepartureIsNamed(entry, operation, compliance, diagnostics))
        {
            return;
        }

        ChangeUsage(entry, operation, segments, diagnostics, allowLoosen: true);
    }

    private static void WidenValues(
        CatalogEntry entry,
        OverrideOperation operation,
        ComplianceMode compliance,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (!DepartureIsNamed(entry, operation, compliance, diagnostics)
            || !TryGetElement(entry, operation, segments, diagnostics, out var segmentId, out var element))
        {
            return;
        }

        var widened = element.AllowedValues is null
            ? operation.AllowedValues ?? []
            : element.AllowedValues.Concat(operation.AllowedValues ?? []).Distinct(StringComparer.Ordinal).ToArray();
        Replace(segments, segmentId, element with
        {
            AllowedValues = widened,
            Origins = Append(element.Origins, entry, operation.Name)
        });
    }

    private static CompiledTransaction? ChangeOccurrence(
        CatalogEntry entry,
        OverrideOperation operation,
        CompiledTransaction? transaction,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (transaction is null || operation.Target is null)
        {
            diagnostics.Add(Policy(DiagnosticCodes.IllegalOverride, "Occurrence override has no transaction target.", entry.Path, operation.Target));
            return transaction;
        }

        var found = false;
        var updated = Map(transaction, node =>
        {
            if (node.Id != operation.Target)
            {
                return node;
            }

            found = true;
            if (LoosensOccurrence(node, operation))
            {
                diagnostics.Add(Policy(
                    DiagnosticCodes.StandardLoosened,
                    $"Occurrence of '{node.Id}' cannot be loosened.",
                    entry.Path,
                    node.Id));
                return node;
            }

            return node with
            {
                MinOccurs = operation.MinOccurs ?? node.MinOccurs,
                MaxOccurs = operation.MaxOccursSet ? operation.MaxOccurs : node.MaxOccurs,
                Origins = Append(node.Origins, entry, operation.Name)
            };
        });

        if (!found)
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.IllegalOverride,
                $"Structure node '{operation.Target}' was not found.",
                entry.Path,
                operation.Target));
            return transaction;
        }

        return updated;
    }

    private static CompiledTransaction? AddSegment(
        CatalogEntry entry,
        OverrideOperation operation,
        ComplianceMode compliance,
        CompiledTransaction? transaction,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (!DepartureIsNamed(entry, operation, compliance, diagnostics) || transaction is null)
        {
            return transaction;
        }

        var node = new CompiledNode(
            operation.Target ?? operation.Segment ?? "dialect-segment",
            "segment",
            operation.Segment,
            operation.SegmentRef,
            operation.Usage ?? "O",
            operation.MinOccurs ?? 0,
            operation.MaxOccursSet ? operation.MaxOccurs : 1,
            null,
            [],
            [Origin(entry, operation.Name)]);
        return transaction with { Heading = transaction.Heading.Append(node).ToArray() };
    }

    private static bool DepartureIsNamed(
        CatalogEntry entry,
        OverrideOperation operation,
        ComplianceMode compliance,
        List<SpecificationDiagnostic> diagnostics)
    {
        if (compliance != ComplianceMode.PartnerDialect || string.IsNullOrWhiteSpace(operation.DepartsFrom))
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.StandardLoosened,
                $"Operation '{operation.Name}' departs from the standard without partner-dialect compliance and a parent citation.",
                entry.Path,
                operation.Target));
            return false;
        }

        if (!string.Equals(operation.DepartsFrom, entry.Document.Extends, StringComparison.Ordinal))
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.StandardLoosened,
                $"Operation '{operation.Name}' cites '{operation.DepartsFrom}' instead of parent '{entry.Document.Extends}'.",
                entry.Path,
                operation.DepartsFrom));
            return false;
        }

        return true;
    }

    private static bool TryGetElement(
        CatalogEntry entry,
        OverrideOperation operation,
        Dictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics,
        out string segmentId,
        out CompiledElement element)
    {
        segmentId = "";
        element = null!;
        if (!TrySplitTarget(operation.Target, entry.Document.Extends, out segmentId, out var elementKey)
            || !segments.TryGetValue(segmentId, out var segment)
            || !segment.Elements.TryGetValue(elementKey, out element!))
        {
            diagnostics.Add(Policy(
                DiagnosticCodes.IllegalOverride,
                $"Override target '{operation.Target}' was not found.",
                entry.Path,
                operation.Target));
            return false;
        }

        return true;
    }

    private static bool TrySplitTarget(string? target, string? extends, out string segmentId, out string elementKey)
    {
        segmentId = "";
        elementKey = "";
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        const string marker = ".elements.";
        var index = target.LastIndexOf(marker, StringComparison.Ordinal);
        if (index >= 0)
        {
            segmentId = target[..index];
            elementKey = target[(index + marker.Length)..];
            return segmentId.Length > 0 && elementKey.Length > 0;
        }

        if (target.StartsWith("elements.", StringComparison.Ordinal) && extends is not null)
        {
            segmentId = extends;
            elementKey = target["elements.".Length..];
            return elementKey.Length > 0;
        }

        return false;
    }

    private static void Replace(Dictionary<string, CompiledSegment> segments, string segmentId, CompiledElement element)
    {
        var segment = segments[segmentId];
        var elements = new Dictionary<string, CompiledElement>(segment.Elements, StringComparer.Ordinal)
        {
            [element.Key] = element
        };
        segments[segmentId] = segment with { Elements = elements };
    }

    private static CompiledSegment CreateSegment(CatalogEntry entry)
    {
        var origin = Origin(entry, "defined");
        var elements = entry.Document.Elements.ToDictionary(
            pair => pair.Key,
            pair => new CompiledElement(
                pair.Value.Key,
                pair.Value.Reference,
                pair.Value.Usage,
                pair.Value.MinLength,
                pair.Value.MaxLength,
                pair.Value.AllowedValues,
                [origin]),
            StringComparer.Ordinal);
        return new CompiledSegment(entry.Id, entry.Document.Tag ?? "", elements, [origin]);
    }

    private static CompiledEnvelope CreateEnvelope(
        CatalogEntry entry,
        IReadOnlyDictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        var refs = entry.Document.EnvelopeSegmentRefs;
        var header = refs.Count > 0 ? refs[0] : "";
        var trailer = refs.Count > 1 ? refs[1] : "";
        var groupHeader = refs.Count > 2 ? refs[2] : "";
        var groupTrailer = refs.Count > 3 ? refs[3] : "";
        foreach (var segmentRef in refs)
        {
            if (!segments.ContainsKey(segmentRef))
            {
                diagnostics.Add(Standard(
                    DiagnosticCodes.ReferencedSegmentMissing,
                    $"Envelope segment '{segmentRef}' was not found.",
                    entry.Path,
                    segmentRef));
            }
        }

        return new CompiledEnvelope(entry.Id, header, trailer, groupHeader, groupTrailer, [Origin(entry, "defined")]);
    }

    private static CompiledTransaction CreateTransaction(
        CatalogEntry entry,
        IReadOnlyDictionary<string, CompiledSegment> segments,
        List<SpecificationDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var heading = CompileNodes(entry, entry.Document.Heading, segments, seen, diagnostics);
        var detail = CompileNodes(entry, entry.Document.Detail, segments, seen, diagnostics);
        var summary = CompileNodes(entry, entry.Document.Summary, segments, seen, diagnostics);
        return new CompiledTransaction(
            entry.Id,
            entry.Document.Transaction ?? "",
            entry.Document.FunctionalGroup,
            heading,
            detail,
            summary);
    }

    private static IReadOnlyList<CompiledNode> CompileNodes(
        CatalogEntry entry,
        IReadOnlyList<StructureNodeModel> nodes,
        IReadOnlyDictionary<string, CompiledSegment> segments,
        HashSet<string> seen,
        List<SpecificationDiagnostic> diagnostics)
    {
        var loops = new HashSet<string>(StringComparer.Ordinal);
        var compiled = new List<CompiledNode>(nodes.Count);
        foreach (var node in nodes)
        {
            if (!seen.Add(node.Id))
            {
                diagnostics.Add(Standard(
                    DiagnosticCodes.DuplicateOccurrence,
                    $"Structure node '{node.Id}' is declared more than once.",
                    entry.Path,
                    node.Id));
            }

            if (node.Kind == "loop" && node.LoopId is not null && !loops.Add(node.LoopId))
            {
                diagnostics.Add(Standard(
                    DiagnosticCodes.AmbiguousLoop,
                    $"Loop '{node.LoopId}' is declared more than once in the same parent.",
                    entry.Path,
                    node.LoopId));
            }

            if (node.SegmentRef is not null && !segments.ContainsKey(node.SegmentRef))
            {
                diagnostics.Add(Standard(
                    DiagnosticCodes.ReferencedSegmentMissing,
                    $"Segment '{node.SegmentRef}' was not found.",
                    entry.Path,
                    node.SegmentRef));
            }

            compiled.Add(new CompiledNode(
                node.Id,
                node.Kind,
                node.Segment,
                node.SegmentRef,
                node.Usage,
                node.MinOccurs,
                node.MaxOccurs,
                node.LoopId,
                CompileNodes(entry, node.Children, segments, seen, diagnostics),
                [Origin(entry, "defined")]));
        }

        return compiled;
    }

    private static void BindCustomRules(
        CatalogEntry entry,
        IRuleRegistry rules,
        List<HandlerBinding> bindings,
        List<SpecificationDiagnostic> diagnostics,
        DiagnosticCategory category)
    {
        foreach (var ruleId in entry.Document.CustomRules)
        {
            Bind(ruleId, entry, rules, bindings, diagnostics, category);
        }
    }

    private static void Bind(
        string? ruleId,
        CatalogEntry entry,
        IRuleRegistry rules,
        List<HandlerBinding> bindings,
        List<SpecificationDiagnostic> diagnostics,
        DiagnosticCategory category)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        if (bindings.Any(binding => binding.RuleId == ruleId))
        {
            return;
        }

        if (!rules.TryGet(ruleId, out var descriptor) || descriptor is null)
        {
            diagnostics.Add(new SpecificationDiagnostic(
                DiagnosticCodes.HandlerMissing,
                DiagnosticSeverity.Error,
                category,
                $"Handler '{ruleId}' is not registered.",
                ruleId,
                entry.Path,
                ruleId));
            return;
        }

        var expected = ExpectedScope(entry.Kind);
        if (expected is not null && descriptor.Scope != expected)
        {
            diagnostics.Add(new SpecificationDiagnostic(
                DiagnosticCodes.IllegalOverride,
                DiagnosticSeverity.Error,
                category,
                $"Handler '{ruleId}' is registered for {descriptor.Scope}, not {expected}.",
                ruleId,
                entry.Path,
                ruleId));
            return;
        }

        bindings.Add(new HandlerBinding(ruleId, descriptor.Scope, descriptor.Mandatory, entry.Id));
    }

    private static RuleScope? ExpectedScope(string kind)
    {
        return kind switch
        {
            "syntax" => RuleScope.Syntax,
            "version" => RuleScope.Version,
            "envelope" => RuleScope.Envelope,
            "segment" or "segment-override" => RuleScope.Segment,
            "transaction" or "transaction-override" => RuleScope.Transaction,
            "provider-profile" => RuleScope.Provider,
            _ => null
        };
    }

    private static CompiledTransaction Map(CompiledTransaction transaction, Func<CompiledNode, CompiledNode> change)
    {
        return transaction with
        {
            Heading = Map(transaction.Heading, change),
            Detail = Map(transaction.Detail, change),
            Summary = Map(transaction.Summary, change)
        };
    }

    private static IReadOnlyList<CompiledNode> Map(IReadOnlyList<CompiledNode> nodes, Func<CompiledNode, CompiledNode> change)
    {
        return nodes.Select(node =>
        {
            var children = Map(node.Children, change);
            var current = children == node.Children ? node : node with { Children = children };
            return change(current);
        }).ToArray();
    }

    private static bool Loosens(string current, string next)
    {
        return Rank(next) < Rank(current);
    }

    private static int Rank(string usage)
    {
        return usage switch
        {
            "M" => 2,
            "C" or "X" => 1,
            _ => 0
        };
    }

    private static bool LoosensOccurrence(CompiledNode node, OverrideOperation operation)
    {
        if (operation.MinOccurs is int min && min < node.MinOccurs)
        {
            return true;
        }

        if (!operation.MaxOccursSet)
        {
            return false;
        }

        if (operation.MaxOccurs is null)
        {
            return node.MaxOccurs is not null;
        }

        return node.MaxOccurs is int current && operation.MaxOccurs > current;
    }

    private static bool IsKnownCompliance(string? compliance)
    {
        return compliance is null or "standard-subset" or "partner-dialect";
    }

    private static IReadOnlyList<RuleOrigin> Append(IReadOnlyList<RuleOrigin> origins, CatalogEntry entry, string change)
    {
        return origins.Append(Origin(entry, change)).ToArray();
    }

    private static RuleOrigin Origin(CatalogEntry entry, string change)
    {
        return new RuleOrigin(entry.Id, entry.Path, entry.Kind, change);
    }

    private static SpecificationDiagnostic Standard(string code, string message, string path, string? target = null)
    {
        return new SpecificationDiagnostic(code, DiagnosticSeverity.Error, DiagnosticCategory.Standard, message, sourcePath: path, target: target);
    }

    private static SpecificationDiagnostic Policy(string code, string message, string path, string? target = null)
    {
        return new SpecificationDiagnostic(code, DiagnosticSeverity.Error, DiagnosticCategory.ProviderPolicy, message, sourcePath: path, target: target);
    }
}
