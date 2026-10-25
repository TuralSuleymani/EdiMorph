using EdiMorph.Diagnostics;
using EdiMorph.Rules;

namespace EdiMorph.Specifications.X12.Syntax;

internal sealed class SyntaxNote
{
    public SyntaxNote(char kind, params string[] positions)
    {
        Kind = kind;
        Positions = positions;
    }

    public char Kind { get; }

    public IReadOnlyList<string> Positions { get; }
}

internal static class SegmentSyntax
{
    public static ValueTask<RuleResult> Check(
        string ruleId,
        string tag,
        X12Segment subject,
        CancellationToken cancellationToken,
        params SyntaxNote[] notes)
    {
        ArgumentNullException.ThrowIfNull(subject);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(subject.Tag, tag, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Handler '{ruleId}' checks {tag}, not {subject.Tag}.", nameof(subject));
        }

        return ValueTask.FromResult(RelationalSyntax.Evaluate(ruleId, subject, notes));
    }
}

internal static class RelationalSyntax
{
    public static RuleResult Evaluate(string ruleId, X12Segment segment, IReadOnlyList<SyntaxNote> notes)
    {
        var failures = new List<SpecificationDiagnostic>();
        foreach (var note in notes)
        {
            var present = note.Positions.Select(position => IsPresent(segment, position)).ToArray();
            if (Satisfied(note.Kind, present))
            {
                continue;
            }

            failures.Add(new SpecificationDiagnostic(
                DiagnosticCodes.RelationalSyntax,
                DiagnosticSeverity.Error,
                DiagnosticCategory.Standard,
                Message(segment.Tag, note),
                ruleId,
                target: segment.Tag + note.Positions[0]));
        }

        return failures.Count == 0 ? RuleResult.Success : RuleResult.Fail(failures);
    }

    private static bool Satisfied(char kind, bool[] present)
    {
        return kind switch
        {
            'P' => present.All(value => value) || present.All(value => !value),
            'R' => present.Any(value => value),
            'C' => !present[0] || present.Skip(1).All(value => value),
            'E' => present.Count(value => value) <= 1,
            'L' => !present[0] || present.Skip(1).Any(value => value),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown X12 syntax note.")
        };
    }

    private static bool IsPresent(X12Segment segment, string position)
    {
        if (segment.Elements.TryGetValue(position, out var value) && !string.IsNullOrEmpty(value))
        {
            return true;
        }

        var componentPrefix = position + "-";
        return segment.Elements.Any(pair =>
            pair.Key.StartsWith(componentPrefix, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(pair.Value));
    }

    private static string Message(string tag, SyntaxNote note)
    {
        var names = note.Positions.Select(position => tag + position).ToArray();
        return note.Kind switch
        {
            'P' => $"If any of {Join(names)} is present, then the others are required.",
            'R' => $"At least one of {Join(names)} is required.",
            'C' => $"If {names[0]} is present, then {Join(names.Skip(1).ToArray())} is required.",
            'E' => $"Only one of {Join(names)} may be present.",
            'L' => $"If {names[0]} is present, then at least one of {Join(names.Skip(1).ToArray())} is required.",
            _ => "The segment syntax note failed."
        };
    }

    private static string Join(IReadOnlyList<string> names)
    {
        if (names.Count == 1)
        {
            return names[0];
        }

        if (names.Count == 2)
        {
            return names[0] + " or " + names[1];
        }

        return string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
    }
}
