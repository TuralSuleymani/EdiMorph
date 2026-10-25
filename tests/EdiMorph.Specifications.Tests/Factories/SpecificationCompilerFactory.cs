using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010;

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

    public static RuleRegistry CreateStandardRules(bool includeFixtureGuide = false)
    {
        var registry = new RuleRegistry();
        X12V4010Rules.Register(registry);
        if (includeFixtureGuide)
        {
            registry.Register(RuleHandlerDescriptor.Create<AcceptingValidator, ProviderSubject>(
                TestDefaults.FixtureGuideRuleId,
                RuleScope.Provider,
                mandatory: false,
                TestDefaults.X12Format,
                TestDefaults.X12Version));
        }

        return registry;
    }

    private sealed record ProviderSubject;

    private sealed class AcceptingValidator : IRuleValidator<ProviderSubject>
    {
        public ValueTask<RuleResult> ValidateAsync(
            ProviderSubject subject,
            RuleContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RuleResult.Success);
        }
    }
}
