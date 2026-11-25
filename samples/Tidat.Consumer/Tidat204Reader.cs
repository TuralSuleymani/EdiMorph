using EdiMorph;
using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010;
using EdiMorph.X12;

namespace Tidat.Consumer;

/// <summary>Consumer-side adapter. No shipping EdiMorph assembly references this project.</summary>
public sealed class Tidat204Reader
{
    public const string Provider = "Tidat";
    public const string ProfileRevision = "1.01";
    private readonly RuleRegistry _rules = new();
    private readonly SpecificationCompiler _compiler;

    public Tidat204Reader(string profilesDirectory)
    {
        X12V4010Rules.Register(_rules);
        _rules.Register(RuleHandlerDescriptor.Create<Tidat204Guide, X12TransactionSet>(
            Tidat204Guide.RuleId, RuleScope.Provider, format: "X12", version: "004010",
            transaction: "204", provider: Provider));

        var catalog = new SpecificationCatalog();
        catalog.Add(new InMemorySpecificationSource(X12RuleDocuments.ReadAll()
            .Select(document => new SpecificationText(document.Path, document.Content))));
        catalog.Add(new FileSystemSpecificationSource(profilesDirectory));
        _compiler = new SpecificationCompiler(catalog, _rules);
    }

    public ValueTask<CompiledSpecification> CompileAsync(
        bool standard = false, string revision = ProfileRevision, CancellationToken cancellationToken = default)
    {
        return _compiler.CompileAsync(standard
            ? new SpecificationRequest("X12", "004010", "204")
            : new SpecificationRequest("X12", "004010", "204", Provider, revision), cancellationToken);
    }

    public async Task<X12ReadResult> ReadAsync(string interchange, CancellationToken cancellationToken = default)
    {
        var specification = await CompileAsync(cancellationToken: cancellationToken);
        if (!specification.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                specification.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        }

        return await new X12Reader().ReadAsync(specification, _rules, interchange, cancellationToken);
    }
}
