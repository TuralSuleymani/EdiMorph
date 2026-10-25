using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

internal static class RuleExecutor
{
    public static async Task ExecuteAsync(
        CompiledSpecification specification,
        IRuleRegistry rules,
        ParsedInterchange interchange,
        List<SpecificationDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var context = new RuleContext(Request(specification));
        var validators = new Dictionary<Type, object>();
        foreach (var segment in interchange.Segments)
        {
            if (segment.DefinitionId is null)
            {
                continue;
            }

            foreach (var binding in specification.Handlers)
            {
                if (binding.Scope != RuleScope.Segment || !string.Equals(binding.SourceDocumentId, segment.DefinitionId, StringComparison.Ordinal))
                {
                    continue;
                }

                await RunAsync(rules, binding, segment.Segment, context, validators, diagnostics, cancellationToken);
            }
        }

        foreach (var transaction in interchange.Transactions)
        {
            var code = transaction.Transaction.Segments.FirstOrDefault()?.Elements.TryGetValue("01", out var value) == true
                ? value
                : null;
            if (!string.Equals(code, specification.Transaction?.Code, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var binding in specification.Handlers)
            {
                if (binding.Scope != RuleScope.Transaction)
                {
                    continue;
                }

                await RunAsync(rules, binding, transaction.Transaction, context, validators, diagnostics, cancellationToken);
            }
        }

        foreach (var binding in specification.Handlers)
        {
            if (binding.Scope is RuleScope.Envelope)
            {
                await RunAsync(rules, binding, interchange.Envelope, context, validators, diagnostics, cancellationToken);
            }
            else if (binding.Scope is RuleScope.Provider)
            {
                await RunProviderAsync(rules, binding, interchange, context, validators, diagnostics, cancellationToken);
            }
        }
    }

    private static async Task RunProviderAsync(
        IRuleRegistry rules,
        HandlerBinding binding,
        ParsedInterchange interchange,
        RuleContext context,
        Dictionary<Type, object> validators,
        List<SpecificationDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (!rules.TryGet(binding.RuleId, out var descriptor) || descriptor is null)
        {
            diagnostics.Add(Missing(binding.RuleId));
            return;
        }

        if (descriptor.SubjectType == typeof(X12Envelope))
        {
            await RunAsync(rules, binding, interchange.Envelope, context, validators, diagnostics, cancellationToken);
            return;
        }

        if (descriptor.SubjectType == typeof(X12TransactionSet))
        {
            foreach (var transaction in interchange.Transactions)
            {
                await RunAsync(rules, binding, transaction.Transaction, context, validators, diagnostics, cancellationToken);
            }

            return;
        }

        diagnostics.Add(ReaderDiagnostics.Error(
            DiagnosticCodes.HandlerSubject,
            $"Handler '{binding.RuleId}' cannot be applied by the X12 reader.",
            binding.RuleId));
    }

    private static async Task RunAsync(
        IRuleRegistry rules,
        HandlerBinding binding,
        object subject,
        RuleContext context,
        Dictionary<Type, object> validators,
        List<SpecificationDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (!rules.TryGet(binding.RuleId, out var descriptor) || descriptor is null)
        {
            diagnostics.Add(Missing(binding.RuleId));
            return;
        }

        if (!descriptor.SubjectType.IsInstanceOfType(subject))
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.HandlerSubject,
                $"Handler '{binding.RuleId}' cannot be applied to {subject.GetType().Name}.",
                binding.RuleId));
            return;
        }

        var result = await InvokeAsync(descriptor, subject, context, validators, cancellationToken);
        if (!result.IsSuccess)
        {
            diagnostics.AddRange(result.Diagnostics);
        }
    }

    private static ValueTask<RuleResult> InvokeAsync(
        RuleHandlerDescriptor descriptor,
        object subject,
        RuleContext context,
        Dictionary<Type, object> validators,
        CancellationToken cancellationToken)
    {
        var method = typeof(RuleExecutor)
            .GetMethod(nameof(ValidateAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(descriptor.SubjectType);
        return (ValueTask<RuleResult>)method.Invoke(null, [descriptor, subject, context, validators, cancellationToken])!;
    }

    private static ValueTask<RuleResult> ValidateAsync<TSubject>(
        RuleHandlerDescriptor descriptor,
        object subject,
        RuleContext context,
        Dictionary<Type, object> validators,
        CancellationToken cancellationToken)
    {
        if (!validators.TryGetValue(descriptor.ImplementationType, out var validator))
        {
            validator = Activator.CreateInstance(descriptor.ImplementationType)
                ?? throw new InvalidOperationException($"Handler '{descriptor.RuleId}' could not be created.");
            validators[descriptor.ImplementationType] = validator;
        }

        return ((IRuleValidator<TSubject>)validator).ValidateAsync((TSubject)subject, context, cancellationToken);
    }

    private static SpecificationRequest Request(CompiledSpecification specification)
    {
        return new SpecificationRequest(
            specification.Identity.Format,
            specification.Identity.Version,
            specification.Identity.Transaction,
            specification.Identity.Provider,
            specification.Identity.ProfileRevision);
    }

    private static SpecificationDiagnostic Missing(string ruleId)
    {
        return new SpecificationDiagnostic(
            DiagnosticCodes.HandlerMissing,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            $"Handler '{ruleId}' is not registered.",
            ruleId,
            target: ruleId);
    }
}
