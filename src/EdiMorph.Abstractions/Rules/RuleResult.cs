using EdiMorph.Diagnostics;

namespace EdiMorph.Rules;

public sealed class RuleResult
{
    private static readonly SpecificationDiagnostic[] Empty = [];

    private RuleResult(IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    public static RuleResult Success { get; } = new(Empty);

    public bool IsSuccess => Diagnostics.Count == 0;

    public IReadOnlyList<SpecificationDiagnostic> Diagnostics { get; }

    public static RuleResult Fail(SpecificationDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new RuleResult([diagnostic]);
    }

    public static RuleResult Fail(IReadOnlyList<SpecificationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.Count == 0)
        {
            throw new ArgumentException("A failed result requires at least one diagnostic.", nameof(diagnostics));
        }

        return new RuleResult(diagnostics);
    }
}
