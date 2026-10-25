using System.Text.Json.Nodes;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

internal sealed record X12InterchangeSyntax(
    string HeaderTag,
    int HeaderLength,
    int ElementSeparatorOffset,
    int ComponentSeparatorOffset,
    int SegmentTerminatorOffset)
{
    public static X12InterchangeSyntax Load()
    {
        var document = X12RuleDocuments.ReadAll()
            .Single(candidate => candidate.Path.Equals("Common/syntax.rule", StringComparison.Ordinal));
        var bootstrap = JsonNode.Parse(document.Content)?["bootstrap"] as JsonObject
            ?? throw new InvalidOperationException("X12 syntax.rule has no bootstrap header.");
        return new X12InterchangeSyntax(
            RequiredString(bootstrap, "tag"),
            RequiredInt(bootstrap, "headerLengthIncludingTerminator"),
            RequiredInt(RequiredObject(bootstrap, "elementSeparator"), "offset"),
            RequiredInt(RequiredObject(bootstrap, "componentSeparator"), "offset"),
            RequiredInt(RequiredObject(bootstrap, "segmentTerminator"), "offset"));
    }

    private static JsonObject RequiredObject(JsonObject parent, string name)
    {
        return parent[name] as JsonObject
            ?? throw new InvalidOperationException($"X12 syntax.rule is missing '{name}'.");
    }

    private static string RequiredString(JsonObject parent, string name)
    {
        return parent[name]?.GetValue<string>()
            ?? throw new InvalidOperationException($"X12 syntax.rule is missing '{name}'.");
    }

    private static int RequiredInt(JsonObject parent, string name)
    {
        return parent[name]?.GetValue<int>()
            ?? throw new InvalidOperationException($"X12 syntax.rule is missing '{name}'.");
    }
}
