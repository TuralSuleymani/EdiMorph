using System.Text.Json;
using System.Text.Json.Nodes;
using EdiMorph.Diagnostics;

namespace EdiMorph.Specifications.Authoring;

internal sealed record ElementModel(
    string Key,
    string? Reference,
    string Usage,
    int? MinLength,
    int? MaxLength,
    IReadOnlyList<string>? AllowedValues);

internal sealed record StructureNodeModel(
    string Id,
    string Kind,
    string? Segment,
    string? SegmentRef,
    string Usage,
    int MinOccurs,
    int? MaxOccurs,
    string? LoopId,
    IReadOnlyList<StructureNodeModel> Children);

internal sealed record OverrideOperation(
    string Name,
    string? Target,
    string? Usage,
    int? MinOccurs,
    bool MaxOccursSet,
    int? MaxOccurs,
    int? MaxLength,
    IReadOnlyList<string>? AllowedValues,
    string? RuleId,
    string? DepartsFrom,
    string? Segment,
    string? SegmentRef);

internal sealed class RuleDocument
{
    internal RuleDocument(
        string path,
        string id,
        string kind,
        int ruleFormatVersion,
        IReadOnlyList<SpecificationDiagnostic> errors)
    {
        Path = path;
        Id = id;
        Kind = kind;
        RuleFormatVersion = ruleFormatVersion;
        Errors = errors;
        CustomRules = [];
        Includes = [];
        Elements = new Dictionary<string, ElementModel>(StringComparer.Ordinal);
        Heading = [];
        Detail = [];
        Summary = [];
        Operations = [];
        OverrideRefs = [];
        Transactions = [];
        EnvelopeSegmentRefs = [];
    }

    public string Path { get; }

    public string Id { get; }

    public string Kind { get; }

    public int RuleFormatVersion { get; }

    public string? Format { get; internal init; }

    public string? Version { get; internal init; }

    public string? Transaction { get; internal init; }

    public string? Provider { get; internal init; }

    public string? ProfileRevision { get; internal init; }

    public string? Compliance { get; internal init; }

    public string? Extends { get; internal init; }

    public string? Tag { get; internal init; }

    public string? FunctionalGroup { get; internal init; }

    public string? SyntaxRef { get; internal init; }

    public string? EnvelopeRef { get; internal init; }

    public IReadOnlyList<string> CustomRules { get; internal init; }

    public IReadOnlyList<string> Includes { get; internal init; }

    public IReadOnlyDictionary<string, ElementModel> Elements { get; internal init; }

    public IReadOnlyList<StructureNodeModel> Heading { get; internal init; }

    public IReadOnlyList<StructureNodeModel> Detail { get; internal init; }

    public IReadOnlyList<StructureNodeModel> Summary { get; internal init; }

    public IReadOnlyList<OverrideOperation> Operations { get; internal init; }

    public IReadOnlyList<string> OverrideRefs { get; internal init; }

    public IReadOnlyList<string> Transactions { get; internal init; }

    public IReadOnlyList<string> EnvelopeSegmentRefs { get; internal init; }

    public IReadOnlyList<SpecificationDiagnostic> Errors { get; }

    public static RuleDocument Invalid(string path, string message)
    {
        return new RuleDocument(Normalize(path), "", "", 0, [Error(DiagnosticCodes.UnsupportedRuleFormat, message, path)])
        {
            Elements = new Dictionary<string, ElementModel>(StringComparer.Ordinal)
        };
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static SpecificationDiagnostic Error(string code, string message, string path, string? target = null)
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

internal static class RuleDocumentParser
{
    private static readonly string[] RelationalOperators =
    [
        "fieldEquals",
        "conditionalExactValue",
        "countEqualsField",
        "segmentCountEqualsField"
    ];

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static RuleDocument Parse(string path, string content)
    {
        var normalized = path.Replace('\\', '/');
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content, documentOptions: DocumentOptions);
        }
        catch (JsonException exception)
        {
            return RuleDocument.Invalid(normalized, $"Rule file '{normalized}' is not valid JSON. {exception.Message}");
        }

        if (root is not JsonObject document)
        {
            return RuleDocument.Invalid(normalized, $"Rule file '{normalized}' must be a JSON object.");
        }

        var errors = new List<SpecificationDiagnostic>();
        CollectRelationalOperators(document, normalized, errors);

        var formatVersion = ReadInt(document, "ruleFormatVersion");
        if (formatVersion != 1)
        {
            errors.Add(Error(
                DiagnosticCodes.UnsupportedRuleFormat,
                $"Rule file '{normalized}' uses unsupported ruleFormatVersion '{formatVersion?.ToString() ?? "missing"}'.",
                normalized));
        }

        var id = ReadString(document, "id") ?? "";
        var kind = ReadString(document, "kind") ?? "";
        if (id.Length == 0 || kind.Length == 0)
        {
            errors.Add(Error(
                DiagnosticCodes.UnsupportedRuleFormat,
                $"Rule file '{normalized}' must include id and kind.",
                normalized));
        }

        var elements = ReadElements(document["elements"] as JsonObject);
        var sections = ReadSections(document["structure"] as JsonObject, normalized, errors);
        return new RuleDocument(normalized, id, kind, formatVersion ?? 0, errors)
        {
            Format = ReadString(document, "format"),
            Version = ReadString(document, "version"),
            Transaction = ReadString(document, "transaction"),
            Provider = ReadString(document, "provider"),
            ProfileRevision = ReadString(document, "profileRevision"),
            Compliance = ReadString(document, "compliance"),
            Extends = ReadString(document, "extends"),
            Tag = ReadString(document, "tag"),
            FunctionalGroup = ReadString(document, "functionalGroup"),
            SyntaxRef = ReadString(document, "syntaxRef"),
            EnvelopeRef = ReadEnvelopeRef(document),
            CustomRules = ReadStringList(document["customRules"] as JsonArray),
            Includes = ReadStringList(document["includes"] as JsonArray),
            Elements = elements,
            Heading = sections.Heading,
            Detail = sections.Detail,
            Summary = sections.Summary,
            Operations = ReadOperations(document["operations"] as JsonArray),
            OverrideRefs = ReadStringList(document["overrides"] as JsonArray),
            Transactions = ReadStringList(document["transactions"] as JsonArray),
            EnvelopeSegmentRefs = ReadEnvelopeSegments(document)
        };
    }

    private static string? ReadEnvelopeRef(JsonObject document)
    {
        if (document["interchange"] is not JsonObject interchange)
        {
            return null;
        }

        return ReadString(interchange, "envelopeRef");
    }

    private static IReadOnlyList<string> ReadEnvelopeSegments(JsonObject document)
    {
        if (document["interchange"] is not JsonObject interchange)
        {
            return [];
        }

        var refs = new List<string>();
        AddSegmentRef(interchange["header"], refs);
        AddSegmentRef(interchange["trailer"], refs);
        if (interchange["groups"] is JsonObject groups)
        {
            AddSegmentRef(groups["header"], refs);
            AddSegmentRef(groups["trailer"], refs);
        }

        return refs;
    }

    private static void AddSegmentRef(JsonNode? node, List<string> refs)
    {
        if (node is JsonObject obj && ReadString(obj, "segmentRef") is string segmentRef)
        {
            refs.Add(segmentRef);
        }
    }

    private static Dictionary<string, ElementModel> ReadElements(JsonObject? elements)
    {
        var result = new Dictionary<string, ElementModel>(StringComparer.Ordinal);
        if (elements is null)
        {
            return result;
        }

        foreach (var (key, value) in elements)
        {
            if (value is not JsonObject element)
            {
                continue;
            }

            var constraints = element["constraints"] as JsonObject;
            int? min = null;
            int? max = null;
            if (constraints?["length"] is JsonObject length)
            {
                if (ReadInt(length, "exact") is int exactLength)
                {
                    min = exactLength;
                    max = exactLength;
                }
                else
                {
                    min = ReadInt(length, "min");
                    max = ReadInt(length, "max");
                }
            }
            else if (constraints?["physicalLength"] is JsonObject physical && ReadInt(physical, "exact") is int exact)
            {
                min = exact;
                max = exact;
            }
            else if (ReadInt(element, "width") is int width)
            {
                min = width;
                max = width;
            }

            IReadOnlyList<string>? allowed = null;
            if (constraints?["allowedValues"] is JsonArray values)
            {
                allowed = ReadStringList(values);
            }

            result[key] = new ElementModel(
                key,
                ReadString(element, "reference"),
                ReadString(element, "usage") ?? "O",
                min,
                max,
                allowed);
        }

        return result;
    }

    private static (IReadOnlyList<StructureNodeModel> Heading, IReadOnlyList<StructureNodeModel> Detail, IReadOnlyList<StructureNodeModel> Summary) ReadSections(
        JsonObject? structure,
        string path,
        List<SpecificationDiagnostic> errors)
    {
        if (structure?["sections"] is not JsonObject sections)
        {
            return ([], [], []);
        }

        return (
            ReadNodes(sections["heading"] as JsonArray, path, errors),
            ReadNodes(sections["detail"] as JsonArray, path, errors),
            ReadNodes(sections["summary"] as JsonArray, path, errors));
    }

    private static IReadOnlyList<StructureNodeModel> ReadNodes(
        JsonArray? nodes,
        string path,
        List<SpecificationDiagnostic> errors)
    {
        if (nodes is null)
        {
            return [];
        }

        var result = new List<StructureNodeModel>(nodes.Count);
        foreach (var node in nodes)
        {
            if (node is not JsonObject obj)
            {
                continue;
            }

            var id = ReadString(obj, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add(Error(DiagnosticCodes.InvalidOccurrence, $"A structure node in '{path}' is missing an id.", path));
                continue;
            }

            var min = ReadInt(obj, "minOccurs") ?? 0;
            var max = obj.ContainsKey("maxOccurs") && obj["maxOccurs"] is not null
                ? ReadInt(obj, "maxOccurs")
                : null;
            if (max is int maxOccurs && min > maxOccurs)
            {
                errors.Add(Error(
                    DiagnosticCodes.InvalidOccurrence,
                    $"Structure node '{id}' has minOccurs {min} greater than maxOccurs {maxOccurs}.",
                    path,
                    id));
            }

            var kind = ReadString(obj, "kind") ?? (ReadString(obj, "segment") is null ? "loop" : "segment");
            result.Add(new StructureNodeModel(
                id,
                kind,
                ReadString(obj, "segment"),
                ReadString(obj, "segmentRef"),
                ReadString(obj, "usage") ?? "O",
                min,
                max,
                ReadString(obj, "loopId"),
                ReadNodes(obj["children"] as JsonArray, path, errors)));
        }

        return result;
    }

    private static IReadOnlyList<OverrideOperation> ReadOperations(JsonArray? operations)
    {
        if (operations is null)
        {
            return [];
        }

        var result = new List<OverrideOperation>(operations.Count);
        foreach (var node in operations)
        {
            if (node is not JsonObject operation)
            {
                continue;
            }

            var maxSet = operation.ContainsKey("maxOccurs");
            result.Add(new OverrideOperation(
                ReadString(operation, "op") ?? "",
                ReadString(operation, "target"),
                ReadString(operation, "usage"),
                ReadInt(operation, "minOccurs"),
                maxSet,
                maxSet && operation["maxOccurs"] is not null ? ReadInt(operation, "maxOccurs") : null,
                ReadInt(operation, "maxLength"),
                operation["allowedValues"] is JsonArray values ? ReadStringList(values) : null,
                ReadString(operation, "ruleId"),
                ReadString(operation, "departsFrom"),
                ReadString(operation, "segment"),
                ReadString(operation, "segmentRef")));
        }

        return result;
    }

    private static void CollectRelationalOperators(JsonObject document, string path, List<SpecificationDiagnostic> errors)
    {
        var found = false;
        Walk(document);
        if (found)
        {
            errors.Add(Error(
                DiagnosticCodes.JsonRelationalOperator,
                $"Rule file '{path}' contains a relational operator. Comparisons belong in a C# handler.",
                path));
        }

        void Walk(JsonNode? node)
        {
            if (found || node is null)
            {
                return;
            }

            if (node is JsonObject obj)
            {
                foreach (var (name, value) in obj)
                {
                    if (name == "rules" || RelationalOperators.Contains(name, StringComparer.Ordinal))
                    {
                        found = true;
                        return;
                    }

                    if (name == "type"
                        && value is JsonValue typeValue
                        && typeValue.TryGetValue<string>(out var type)
                        && RelationalOperators.Contains(type, StringComparer.Ordinal))
                    {
                        found = true;
                        return;
                    }

                    Walk(value);
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    Walk(item);
                }
            }
        }
    }

    private static IReadOnlyList<string> ReadStringList(JsonArray? array)
    {
        if (array is null)
        {
            return [];
        }

        var values = new List<string>(array.Count);
        foreach (var item in array)
        {
            if (item is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
            {
                values.Add(text);
            }
        }

        return values;
    }

    private static string? ReadString(JsonObject obj, string name)
    {
        if (obj[name] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return null;
    }

    private static int? ReadInt(JsonObject obj, string name)
    {
        if (obj[name] is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<int>(out var number))
        {
            return number;
        }

        return null;
    }

    private static SpecificationDiagnostic Error(string code, string message, string path, string? target = null)
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
