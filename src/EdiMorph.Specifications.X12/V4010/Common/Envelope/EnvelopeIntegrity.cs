using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.X12.V4010.Common.Envelope;

public sealed class EnvelopeIntegrity : IRuleValidator<X12Envelope>
{
    public const string RuleId = "X12_V4010_ENVELOPE_INTEGRITY";

    public ValueTask<RuleResult> ValidateAsync(
        X12Envelope subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        cancellationToken.ThrowIfCancellationRequested();

        var failures = new List<SpecificationDiagnostic>();
        RequireSame(failures, "ISA13", subject.HeaderControlNumber, "IEA02", subject.TrailerControlNumber);
        RequireCount(failures, "IEA01", subject.DeclaredGroupCount, subject.Groups.Count, "functional groups");

        for (var groupIndex = 0; groupIndex < subject.Groups.Count; groupIndex++)
        {
            var group = subject.Groups[groupIndex];
            RequireSame(failures, "GS06", group.HeaderControlNumber, "GE02", group.TrailerControlNumber);
            RequireCount(failures, "GE01", group.DeclaredTransactionCount, group.Transactions.Count, "transaction sets");

            foreach (var transaction in group.Transactions)
            {
                RequireSame(failures, "ST02", transaction.HeaderControlNumber, "SE02", transaction.TrailerControlNumber);
                RequireCount(failures, "SE01", transaction.DeclaredSegmentCount, transaction.SegmentCount, "segments");
            }
        }

        return ValueTask.FromResult(failures.Count == 0 ? RuleResult.Success : RuleResult.Fail(failures));
    }

    private static void RequireSame(
        List<SpecificationDiagnostic> failures,
        string headerField,
        string? headerValue,
        string trailerField,
        string? trailerValue)
    {
        var header = headerValue?.Trim();
        var trailer = trailerValue?.Trim();
        if (!string.IsNullOrEmpty(header) && string.Equals(header, trailer, StringComparison.Ordinal))
        {
            return;
        }

        failures.Add(Failure($"{headerField} must match {trailerField}."));
    }

    private static void RequireCount(
        List<SpecificationDiagnostic> failures,
        string field,
        int? declared,
        int actual,
        string name)
    {
        if (declared == actual)
        {
            return;
        }

        failures.Add(Failure($"{field} must equal the number of included {name}."));
    }

    private static SpecificationDiagnostic Failure(string message)
    {
        return new SpecificationDiagnostic(
            DiagnosticCodes.EnvelopeIntegrity,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            message,
            RuleId,
            target: RuleId);
    }
}
