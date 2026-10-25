namespace EdiMorph.Diagnostics;

public enum DiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Distinguishes a failure of the standard from a failure of a provider guide or an explicit dialect departure.
/// </summary>
public enum DiagnosticCategory
{
    Standard = 0,
    ProviderPolicy = 1,
    DialectDeparture = 2
}

public sealed record SpecificationDiagnostic
{
    public SpecificationDiagnostic(
        string code,
        DiagnosticSeverity severity,
        DiagnosticCategory category,
        string message,
        string? ruleId = null,
        string? sourcePath = null,
        string? target = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Severity = severity;
        Category = category;
        Message = message;
        RuleId = ruleId;
        SourcePath = sourcePath;
        Target = target;
    }

    public string Code { get; }

    public DiagnosticSeverity Severity { get; }

    public DiagnosticCategory Category { get; }

    public string Message { get; }

    public string? RuleId { get; }

    public string? SourcePath { get; }

    public string? Target { get; }
}
