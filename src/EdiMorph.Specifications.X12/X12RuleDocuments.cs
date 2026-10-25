using System.Reflection;
using System.Text;

namespace EdiMorph.Specifications.X12;

public static class X12RuleDocuments
{
    public static IReadOnlyList<(string Path, string Content)> ReadAll()
    {
        var assembly = typeof(X12RuleDocuments).Assembly;
        var documents = new List<(string Path, string Content)>();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".rule", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing embedded rule '{name}'.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            documents.Add((LogicalPath(name), reader.ReadToEnd()));
        }

        return documents;
    }

    private static string LogicalPath(string resourceName)
    {
        var normalized = resourceName.Replace('\\', '/');
        var version = normalized.IndexOf("V4010/", StringComparison.Ordinal);
        var common = normalized.IndexOf("Common/", StringComparison.Ordinal);
        var start = version >= 0 && common >= 0
            ? Math.Min(version, common)
            : Math.Max(version, common);
        return start >= 0 ? normalized[start..] : normalized;
    }
}