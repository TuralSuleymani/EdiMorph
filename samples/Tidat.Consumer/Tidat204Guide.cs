using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications.X12;

namespace Tidat.Consumer;

/// <summary>
/// Contextual restrictions from the supplied Outbound 204 guide, change log V1.01 (2021-11-16).
/// Outbound is from the document sender's perspective; this consumer reads the document.
/// Local element restrictions are in .rule files; comparisons and contextual code lists are here.
/// </summary>
public sealed class Tidat204Guide : IRuleValidator<X12TransactionSet>
{
    public const string RuleId = "TIDAT_204_GUIDE";

    public ValueTask<RuleResult> ValidateAsync(
        X12TransactionSet subject, RuleContext context, CancellationToken cancellationToken = default)
    {
        var failures = new List<SpecificationDiagnostic>();
        string? stopReason = null;
        var inItems = false;
        foreach (var segment in subject.Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (segment.Tag == "S5")
            {
                stopReason = Value(segment, "02");
                inItems = false;
            }
            else if (segment.Tag == "L5")
            {
                inItems = true;
            }
            else if (segment.Tag is "L3" or "SE")
            {
                stopReason = null;
            }

            switch (segment.Tag)
            {
                case "N1":
                    if (stopReason is null)
                    {
                        Check(segment, "01", ["BT", "SH"], failures);
                    }
                    else
                    {
                        Check(segment, "01", stopReason is "LD" or "PA" ? ["SF"] : ["ST"], failures);
                        Check(segment, "03", ["ZZ"], failures);
                    }
                    break;
                case "L11":
                    Check(segment, "02", stopReason is null ? ["ZZ", "CC", "06"] : ["AO", "P8", "DO"], failures);
                    break;
                case "G62":
                    Check(segment, "01", stopReason is null ? ["64"] : ["98", "96", "69", "99", "97", "70", "38", "54"], failures);
                    Check(segment, "03", stopReason is null ? ["1"] : ["EP", "I", "ED", "G", "LP", "LD"], failures);
                    Check(segment, "05", stopReason is null ? ["CT"] : ["LT"], failures);
                    break;
                case "G61":
                    Check(segment, "01", ["IC"], failures);
                    Check(segment, "03", ["TE", "EM"], failures);
                    break;
                case "AT5" when stopReason is not null:
                    Fail("AT5 is not used inside stop loop 0300 in V1.01; use S502 PA/DT.", "AT5", failures);
                    break;
                case "AT5":
                    Check(segment, "01", ["EXQ"], failures);
                    break;
                case "L5" when inItems:
                    Check(segment, "07", ["ZZ"], failures);
                    break;
                case "AT8":
                    Check(segment, "01", ["G"], failures);
                    Check(segment, "02", ["L"], failures);
                    Check(segment, "06", ["E"], failures);
                    break;
                case "L3":
                    Check(segment, "02", ["G"], failures);
                    break;
            }
        }

        return ValueTask.FromResult(failures.Count == 0 ? RuleResult.Success : RuleResult.Fail(failures));
    }

    private static void Check(X12Segment segment, string position, string[] allowed,
        List<SpecificationDiagnostic> failures)
    {
        // Optional values remain optional. Standard syntax handlers validate paired presence.
        var value = Value(segment, position);
        if (value is not null && !allowed.Contains(value, StringComparer.Ordinal))
        {
            Fail($"{segment.Tag}{position} must be one of [{string.Join(", ", allowed)}] in this scope.",
                segment.Tag + position, failures);
        }
    }

    private static string? Value(X12Segment segment, string position) =>
        segment.Elements.TryGetValue(position, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    private static void Fail(string message, string target, List<SpecificationDiagnostic> failures) =>
        failures.Add(new SpecificationDiagnostic(DiagnosticCodes.TransactionSyntax, DiagnosticSeverity.Error,
            DiagnosticCategory.ProviderPolicy, message, RuleId, target: target));
}
