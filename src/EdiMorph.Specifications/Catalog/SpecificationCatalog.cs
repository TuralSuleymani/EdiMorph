using System.Security.Cryptography;
using System.Text;
using EdiMorph.Specifications.Authoring;

namespace EdiMorph.Specifications;

public sealed record SpecificationText(string Path, string Content);

public interface ISpecificationSource
{
    IReadOnlyList<SpecificationText> Read();
}

public sealed class InMemorySpecificationSource : ISpecificationSource
{
    private readonly IReadOnlyList<SpecificationText> _documents;

    public InMemorySpecificationSource(IEnumerable<SpecificationText> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        _documents = documents.ToArray();
    }

    public IReadOnlyList<SpecificationText> Read() => _documents;
}

public sealed class FileSystemSpecificationSource : ISpecificationSource
{
    private readonly string _root;

    public FileSystemSpecificationSource(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = root;
    }

    public IReadOnlyList<SpecificationText> Read()
    {
        var root = Path.GetFullPath(_root);
        return Directory.EnumerateFiles(root, "*.rule", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(path => new SpecificationText(RelativePath(root, path), File.ReadAllText(path)))
            .ToArray();
    }

    private static bool IsBuildOutput(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static string RelativePath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative.Replace('\\', '/');
    }
}

public sealed class CatalogEntry
{
    internal CatalogEntry(string path, string contentHash, string content, RuleDocument document)
    {
        Path = path;
        ContentHash = contentHash;
        Content = content;
        Document = document;
    }

    public string Path { get; }

    public string ContentHash { get; }

    public string Content { get; }

    public string Id => Document.Id;

    public string Kind => Document.Kind;

    internal RuleDocument Document { get; }
}

public sealed class SpecificationCatalog
{
    private readonly List<ISpecificationSource> _sources = [];
    private IReadOnlyList<CatalogEntry>? _entries;

    public void Add(ISpecificationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources.Add(source);
        _entries = null;
    }

    public IReadOnlyList<CatalogEntry> Entries => _entries ??= Load();

    private IReadOnlyList<CatalogEntry> Load()
    {
        var entries = new List<CatalogEntry>();
        foreach (var source in _sources)
        {
            foreach (var document in source.Read())
            {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.Content)))
                    .ToLowerInvariant();
                entries.Add(new CatalogEntry(
                    document.Path.Replace('\\', '/'),
                    hash,
                    document.Content,
                    RuleDocumentParser.Parse(document.Path, document.Content)));
            }
        }

        return entries;
    }
}
