namespace EdiMorph.Rules;

/// <summary>
/// Registration metadata for one precompiled rule handler. The compiler binds ids. It does not execute the handler.
/// </summary>
public sealed record RuleHandlerDescriptor
{
    public RuleHandlerDescriptor(
        string ruleId,
        RuleScope scope,
        Type implementationType,
        Type subjectType,
        bool mandatory,
        string? format = null,
        string? version = null,
        string? transaction = null,
        string? provider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(subjectType);
        RejectBlank(format, nameof(format));
        RejectBlank(version, nameof(version));
        RejectBlank(transaction, nameof(transaction));
        RejectBlank(provider, nameof(provider));

        var validator = typeof(IRuleValidator<>).MakeGenericType(subjectType);
        if (!validator.IsAssignableFrom(implementationType))
        {
            throw new ArgumentException(
                $"{implementationType.FullName} must implement {validator.FullName}.",
                nameof(implementationType));
        }

        RuleId = ruleId;
        Scope = scope;
        ImplementationType = implementationType;
        SubjectType = subjectType;
        Mandatory = mandatory;
        Format = format;
        Version = version;
        Transaction = transaction;
        Provider = provider;
    }

    public string RuleId { get; }

    public RuleScope Scope { get; }

    public Type ImplementationType { get; }

    public Type SubjectType { get; }

    /// <summary>A mandatory standard handler cannot be disabled by a provider profile.</summary>
    public bool Mandatory { get; }

    public string? Format { get; }

    public string? Version { get; }

    public string? Transaction { get; }

    public string? Provider { get; }

    public static RuleHandlerDescriptor Create<TValidator, TSubject>(
        string ruleId,
        RuleScope scope,
        bool mandatory = false,
        string? format = null,
        string? version = null,
        string? transaction = null,
        string? provider = null)
        where TValidator : IRuleValidator<TSubject>
    {
        return new RuleHandlerDescriptor(
            ruleId,
            scope,
            typeof(TValidator),
            typeof(TSubject),
            mandatory,
            format,
            version,
            transaction,
            provider);
    }

    private static void RejectBlank(string? value, string name)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", name);
        }
    }
}
