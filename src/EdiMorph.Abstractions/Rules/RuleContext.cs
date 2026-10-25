namespace EdiMorph.Rules;

/// <summary>
/// Identity of the specification being validated, and the occurrence currently in scope.
/// </summary>
public sealed class RuleContext
{
    public RuleContext(
        SpecificationRequest request,
        string? occurrenceId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
        OccurrenceId = occurrenceId;
        Properties = properties ?? EmptyProperties;
    }

    public SpecificationRequest Request { get; }

    public string? OccurrenceId { get; }

    public IReadOnlyDictionary<string, string?> Properties { get; }

    private static readonly IReadOnlyDictionary<string, string?> EmptyProperties =
        new Dictionary<string, string?>();
}
