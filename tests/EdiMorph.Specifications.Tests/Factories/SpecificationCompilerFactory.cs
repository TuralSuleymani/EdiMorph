using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.Tests.Factories;

internal static class SpecificationCompilerFactory
{
    public static SpecificationCatalog CreateX12Catalog(params SpecificationText[] additional)
    {
        var documents = X12RuleDocuments.ReadAll()
            .Select(document => new SpecificationText(document.Path, document.Content))
            .Concat(additional);
        var catalog = new SpecificationCatalog();
        catalog.Add(new InMemorySpecificationSource(documents));
        return catalog;
    }

    public static SpecificationCompiler CreateX12(IRuleRegistry? rules = null, params SpecificationText[] additional)
    {
        return new SpecificationCompiler(CreateX12Catalog(additional), rules ?? CreateStandardRules());
    }

    public static SpecificationCompiler Create(IEnumerable<SpecificationText> documents, IRuleRegistry? rules = null)
    {
        var catalog = new SpecificationCatalog();
        catalog.Add(new InMemorySpecificationSource(documents));
        return new SpecificationCompiler(catalog, rules ?? new RuleRegistry());
    }

    public static RuleRegistry CreateStandardRules(bool includeIsaAuthorization = false, bool includeFixtureGuide = false)
    {
        var registry = new RuleRegistry();
        registry.Register(Descriptor<EnvelopeSubject>(
            TestDefaults.EnvelopeIntegrityRuleId,
            RuleScope.Envelope,
            mandatory: true));
        if (includeIsaAuthorization)
        {
            registry.Register(Descriptor<SegmentSubject>(
                TestDefaults.IsaAuthorizationRuleId,
                RuleScope.Segment,
                mandatory: true));
        }

        if (includeFixtureGuide)
        {
            registry.Register(Descriptor<ProviderSubject>(
                TestDefaults.FixtureGuideRuleId,
                RuleScope.Provider,
                mandatory: false));
        }

        return registry;
    }

    private static RuleHandlerDescriptor Descriptor<TSubject>(string ruleId, RuleScope scope, bool mandatory)
    {
        return RuleHandlerDescriptor.Create<AcceptingValidator<TSubject>, TSubject>(
            ruleId,
            scope,
            mandatory,
            TestDefaults.X12Format,
            TestDefaults.X12Version);
    }

    private sealed record EnvelopeSubject;

    private sealed record SegmentSubject;

    private sealed record ProviderSubject;

    private sealed class AcceptingValidator<TSubject> : IRuleValidator<TSubject>
    {
        public ValueTask<RuleResult> ValidateAsync(
            TSubject subject,
            RuleContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RuleResult.Success);
        }
    }
}
