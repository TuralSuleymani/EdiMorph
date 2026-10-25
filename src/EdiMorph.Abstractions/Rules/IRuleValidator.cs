namespace EdiMorph.Rules;

/// <summary>
/// A precompiled rule. The subject is the segment, envelope, or transaction the rule is allowed to see.
/// </summary>
public interface IRuleValidator<in TSubject>
{
    ValueTask<RuleResult> ValidateAsync(
        TSubject subject,
        RuleContext context,
        CancellationToken cancellationToken = default);
}
