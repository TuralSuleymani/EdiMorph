namespace EdiMorph.Rules;

public interface IRuleRegistry
{
    void Register(RuleHandlerDescriptor descriptor);

    bool TryGet(string ruleId, out RuleHandlerDescriptor? descriptor);

    IReadOnlyCollection<RuleHandlerDescriptor> Handlers { get; }

    /// <summary>Changes when the registered set changes. Cache keys include this value.</summary>
    string Revision { get; }
}

public sealed class RuleRegistry : IRuleRegistry
{
    private readonly Dictionary<string, RuleHandlerDescriptor> _handlers = new(StringComparer.Ordinal);
    private string _revision = string.Empty;

    public IReadOnlyCollection<RuleHandlerDescriptor> Handlers => _handlers.Values;

    public string Revision => _revision;

    public void Register(RuleHandlerDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!_handlers.TryAdd(descriptor.RuleId, descriptor))
        {
            throw new InvalidOperationException($"Rule '{descriptor.RuleId}' is already registered.");
        }

        _revision = ComputeRevision();
    }

    public bool TryGet(string ruleId, out RuleHandlerDescriptor? descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        return _handlers.TryGetValue(ruleId, out descriptor);
    }

    private string ComputeRevision()
    {
        return string.Join(
            ';',
            _handlers.Values
                .OrderBy(handler => handler.RuleId, StringComparer.Ordinal)
                .Select(handler => string.Join(
                    '|',
                    handler.RuleId,
                    handler.Scope,
                    handler.Mandatory,
                    handler.ImplementationType.AssemblyQualifiedName)));
    }
}
