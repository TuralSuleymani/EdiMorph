namespace EdiMorph;

/// <summary>
/// Selects the standard to compile, and optionally the provider overlay.
/// </summary>
public sealed record SpecificationRequest
{
    public SpecificationRequest(
        string format,
        string version,
        string? transaction = null,
        string? provider = null,
        string? profileRevision = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        RejectBlank(transaction, nameof(transaction));
        RejectBlank(provider, nameof(provider));
        RejectBlank(profileRevision, nameof(profileRevision));

        Format = format;
        Version = version;
        Transaction = transaction;
        Provider = provider;
        ProfileRevision = profileRevision;
    }

    public string Format { get; }

    public string Version { get; }

    public string? Transaction { get; }

    public string? Provider { get; }

    public string? ProfileRevision { get; }

    private static void RejectBlank(string? value, string name)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", name);
        }
    }
}
