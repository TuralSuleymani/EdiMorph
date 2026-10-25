using EdiMorph;
using EdiMorph.Diagnostics;

namespace EdiMorph.Specifications;

public sealed record ResolvedDocument(string Id, string Kind, string Path, string ContentHash);

public sealed class ResolutionPlan
{
    internal ResolutionPlan(
        IReadOnlyList<ResolvedDocument> baseDocuments,
        IReadOnlyList<CatalogEntry> baseEntries,
        IReadOnlyList<CatalogEntry> overlayEntries,
        IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        BaseDocuments = baseDocuments;
        BaseEntries = baseEntries;
        OverlayEntries = overlayEntries;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<ResolvedDocument> BaseDocuments { get; }

    public IReadOnlyList<SpecificationDiagnostic> Diagnostics { get; }

    internal IReadOnlyList<CatalogEntry> BaseEntries { get; }

    internal IReadOnlyList<CatalogEntry> OverlayEntries { get; }
}

public sealed class SpecificationResolver
{
    public ResolutionPlan Resolve(SpecificationCatalog catalog, SpecificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<SpecificationDiagnostic>();
        var entries = catalog.Entries;
        var version = entries.FirstOrDefault(entry =>
            entry.Kind == "version"
            && entry.Document.Format == request.Format
            && entry.Document.Version == request.Version);

        if (version is null)
        {
            diagnostics.Add(Error(
                DiagnosticCodes.UnsupportedRuleFormat,
                $"Version '{request.Format} {request.Version}' was not found."));
            return Empty(diagnostics);
        }

        var syntaxId = version.Document.SyntaxRef;
        var syntax = entries.FirstOrDefault(entry => entry.Kind == "syntax" && entry.Id == syntaxId);
        if (syntax is null)
        {
            diagnostics.Add(Error(
                DiagnosticCodes.ReferencedSegmentMissing,
                $"Syntax '{syntaxId ?? "missing"}' was not found.",
                version.Path,
                syntaxId));
        }

        var envelopeId = version.Document.EnvelopeRef;
        var envelope = entries.FirstOrDefault(entry => entry.Kind == "envelope" && entry.Id == envelopeId);
        if (envelope is null)
        {
            diagnostics.Add(Error(
                DiagnosticCodes.ReferencedSegmentMissing,
                $"Envelope '{envelopeId ?? "missing"}' was not found.",
                version.Path,
                envelopeId));
        }

        CheckIncludes(version, entries, diagnostics);

        var segments = entries
            .Where(entry =>
                entry.Kind == "segment"
                && entry.Document.Format == request.Format
                && entry.Document.Version == request.Version)
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .ToArray();

        var selected = new List<CatalogEntry>();
        if (syntax is not null)
        {
            selected.Add(syntax);
        }

        selected.Add(version);
        if (envelope is not null)
        {
            selected.Add(envelope);
        }

        selected.AddRange(segments);

        if (!string.IsNullOrWhiteSpace(request.Transaction))
        {
            var transactions = entries
                .Where(entry =>
                    entry.Kind == "transaction"
                    && entry.Document.Format == request.Format
                    && entry.Document.Version == request.Version
                    && entry.Document.Transaction == request.Transaction)
                .ToArray();

            if (transactions.Length == 0)
            {
                diagnostics.Add(Error(
                    DiagnosticCodes.ReferencedSegmentMissing,
                    $"Transaction '{request.Transaction}' was not found for {request.Format} {request.Version}.",
                    target: request.Transaction));
            }
            else if (transactions.Length > 1)
            {
                diagnostics.Add(Error(
                    DiagnosticCodes.AmbiguousDefinition,
                    $"Transaction '{request.Transaction}' is defined more than once.",
                    target: request.Transaction));
            }
            else
            {
                selected.Add(transactions[0]);
            }
        }

        var overlays = new List<CatalogEntry>();
        if (!string.IsNullOrWhiteSpace(request.Provider))
        {
            var profiles = entries
                .Where(entry => entry.Kind == "provider-profile" && entry.Document.Provider == request.Provider)
                .Where(entry => request.ProfileRevision is null || entry.Document.ProfileRevision == request.ProfileRevision)
                .ToArray();

            if (profiles.Length == 0)
            {
                diagnostics.Add(Policy(
                    DiagnosticCodes.IllegalOverride,
                    $"Provider profile '{request.Provider}' was not found.",
                    target: request.Provider));
            }
            else if (profiles.Length > 1)
            {
                diagnostics.Add(Policy(
                    DiagnosticCodes.AmbiguousDefinition,
                    $"Provider '{request.Provider}' has more than one profile.",
                    target: request.Provider));
            }
            else
            {
                var profile = profiles[0];
                overlays.Add(profile);
                if (request.Transaction is not null
                    && profile.Document.Transactions.Count > 0
                    && !profile.Document.Transactions.Contains(request.Transaction, StringComparer.Ordinal))
                {
                    diagnostics.Add(Policy(
                        DiagnosticCodes.IllegalOverride,
                        $"Provider '{request.Provider}' does not implement transaction '{request.Transaction}'.",
                        profile.Path,
                        request.Transaction));
                }

                foreach (var overrideId in profile.Document.OverrideRefs)
                {
                    var match = entries.FirstOrDefault(entry => entry.Id == overrideId);
                    if (match is null)
                    {
                        diagnostics.Add(Policy(
                            DiagnosticCodes.ReferencedSegmentMissing,
                            $"Override '{overrideId}' was not found.",
                            profile.Path,
                            overrideId));
                        continue;
                    }

                    overlays.Add(match);
                }
            }
        }

        return new ResolutionPlan(
            selected.Select(entry => new ResolvedDocument(entry.Id, entry.Kind, entry.Path, entry.ContentHash)).ToArray(),
            selected,
            overlays,
            diagnostics);
    }

    private static void CheckIncludes(CatalogEntry version, IReadOnlyList<CatalogEntry> entries, List<SpecificationDiagnostic> diagnostics)
    {
        var versionDirectory = DirectoryOf(version.Path);
        if (versionDirectory.Length == 0 || version.Document.Includes.Count == 0)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry.Path == version.Path || !entry.Path.StartsWith(versionDirectory + "/", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = entry.Path[(versionDirectory.Length + 1)..];
            if (version.Document.Includes.Any(pattern => Glob.IsMatch(relative, pattern)))
            {
                continue;
            }

            diagnostics.Add(Error(
                DiagnosticCodes.IllegalOverride,
                $"Rule file '{entry.Path}' is not covered by the version includes.",
                entry.Path));
        }
    }

    private static string DirectoryOf(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? "" : path[..slash];
    }

    private static ResolutionPlan Empty(List<SpecificationDiagnostic> diagnostics)
    {
        return new ResolutionPlan([], [], [], diagnostics);
    }

    private static SpecificationDiagnostic Error(string code, string message, string? path = null, string? target = null)
    {
        return new SpecificationDiagnostic(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            message,
            sourcePath: path,
            target: target);
    }

    private static SpecificationDiagnostic Policy(string code, string message, string? path = null, string? target = null)
    {
        return new SpecificationDiagnostic(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.ProviderPolicy,
            message,
            sourcePath: path,
            target: target);
    }
}

internal static class Glob
{
    public static bool IsMatch(string path, string pattern)
    {
        return Match(
            path.Split('/'),
            0,
            pattern.Replace('\\', '/').Split('/'),
            0);
    }

    private static bool Match(string[] path, int pathIndex, string[] pattern, int patternIndex)
    {
        while (patternIndex < pattern.Length)
        {
            if (pattern[patternIndex] == "**")
            {
                if (patternIndex == pattern.Length - 1)
                {
                    return true;
                }

                for (var index = pathIndex; index <= path.Length; index++)
                {
                    if (Match(path, index, pattern, patternIndex + 1))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (pathIndex >= path.Length || !SegmentMatches(path[pathIndex], pattern[patternIndex]))
            {
                return false;
            }

            pathIndex++;
            patternIndex++;
        }

        return pathIndex == path.Length;
    }

    private static bool SegmentMatches(string text, string pattern)
    {
        var textIndex = 0;
        var patternIndex = 0;
        var star = -1;
        var match = 0;
        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == text[textIndex]))
            {
                textIndex++;
                patternIndex++;
                continue;
            }

            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                star = patternIndex++;
                match = textIndex;
                continue;
            }

            if (star >= 0)
            {
                patternIndex = star + 1;
                textIndex = ++match;
                continue;
            }

            return false;
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
