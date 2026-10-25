using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

/// <summary>
/// Reads one X12 interchange using a compiled specification and the handlers bound to it.
/// </summary>
public sealed class X12Reader
{
    public async Task<X12ReadResult> ReadAsync(
        CompiledSpecification specification,
        IRuleRegistry rules,
        string interchange,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(interchange);
        cancellationToken.ThrowIfCancellationRequested();
        if (!specification.IsSuccess || specification.Envelope is null || specification.Transaction is null)
        {
            throw new ArgumentException(
                "The X12 reader requires a compiled envelope and transaction.",
                nameof(specification));
        }

        if (!string.Equals(specification.Identity.Format, "X12", StringComparison.Ordinal))
        {
            throw new ArgumentException("The X12 reader requires an X12 specification.", nameof(specification));
        }

        if (interchange.Length > 0 && interchange[0] == '\uFEFF')
        {
            interchange = interchange[1..];
        }

        var diagnostics = new List<SpecificationDiagnostic>();
        var syntax = X12InterchangeSyntax.Load();
        if (!X12Tokenizer.TryTokenize(interchange, syntax, specification, diagnostics, out var tokens))
        {
            return new X12ReadResult(null, diagnostics);
        }

        var parsed = X12StructureReader.Read(specification, tokens, diagnostics);
        await RuleExecutor.ExecuteAsync(specification, rules, parsed, diagnostics, cancellationToken);
        return new X12ReadResult(parsed.Envelope, diagnostics);
    }
}
