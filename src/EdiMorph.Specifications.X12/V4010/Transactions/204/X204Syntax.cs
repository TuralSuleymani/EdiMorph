using System.Globalization;
using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.X12.V4010.Transactions;

public sealed class X204Syntax : IRuleValidator<X12TransactionSet>
{
    public const string RuleId = "X12_V4010_204_SYNTAX";

    public ValueTask<RuleResult> ValidateAsync(
        X12TransactionSet subject,
        RuleContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        cancellationToken.ThrowIfCancellationRequested();

        var failures = new List<SpecificationDiagnostic>();
        var previousSequence = 0;
        var hasPrevious = false;
        foreach (var stop in Stops(subject.Segments))
        {
            var stopSegment = stop[0];
            if (!TrySequence(stopSegment, out var sequence))
            {
                failures.Add(Failure("S501 must be a stop sequence number.", "S501"));
                continue;
            }

            if (hasPrevious && sequence <= previousSequence)
            {
                failures.Add(Failure("S501 stop sequence numbers must increase from one stop to the next.", "S501"));
            }

            previousSequence = sequence;
            hasPrevious = true;
            CompareWeight(stop, failures);
        }

        return ValueTask.FromResult(failures.Count == 0 ? RuleResult.Success : RuleResult.Fail(failures));
    }

    private static void CompareWeight(IReadOnlyList<X12Segment> stop, List<SpecificationDiagnostic> failures)
    {
        var stopSegment = stop[0];
        if (!TryDecimal(stopSegment, "03", out var stopWeight))
        {
            return;
        }

        var stopUnit = Value(stopSegment, "04");
        foreach (var weightSegment in stop.Where(segment => segment.Tag == "AT8"))
        {
            if (!TryDecimal(weightSegment, "03", out var shipmentWeight))
            {
                continue;
            }

            if (stopWeight != shipmentWeight)
            {
                failures.Add(Failure("S503 must equal AT803 when both weights are present on the same stop.", "S503"));
            }

            var shipmentUnit = Value(weightSegment, "02");
            if (stopUnit is not null
                && shipmentUnit is not null
                && !string.Equals(stopUnit, shipmentUnit, StringComparison.Ordinal))
            {
                failures.Add(Failure("S504 must equal AT802 when both weight units are present on the same stop.", "S504"));
            }
        }
    }

    private static List<List<X12Segment>> Stops(IReadOnlyList<X12Segment> segments)
    {
        var stops = new List<List<X12Segment>>();
        List<X12Segment>? current = null;
        foreach (var segment in segments)
        {
            if (segment.Tag == "S5")
            {
                current = [segment];
                stops.Add(current);
                continue;
            }

            current?.Add(segment);
        }

        return stops;
    }

    private static bool TrySequence(X12Segment segment, out int sequence)
    {
        sequence = 0;
        return Value(segment, "01") is string text
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out sequence);
    }

    private static bool TryDecimal(X12Segment segment, string position, out decimal number)
    {
        number = 0;
        return Value(segment, position) is string text
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    private static string? Value(X12Segment segment, string position)
    {
        return segment.Elements.TryGetValue(position, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : null;
    }

    private static SpecificationDiagnostic Failure(string message, string target)
    {
        return new SpecificationDiagnostic(
            DiagnosticCodes.TransactionSyntax,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Standard,
            message,
            RuleId,
            target: target);
    }
}
