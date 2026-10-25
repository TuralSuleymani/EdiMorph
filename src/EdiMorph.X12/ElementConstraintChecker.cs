using EdiMorph.Diagnostics;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;

namespace EdiMorph.X12;

internal static class ElementConstraintChecker
{
    public static X12Segment Check(
        X12Token token,
        CompiledSegment definition,
        List<SpecificationDiagnostic> diagnostics)
    {
        foreach (var element in definition.Elements.Values)
        {
            token.Elements.TryGetValue(element.Key, out var value);
            var present = !string.IsNullOrEmpty(value);
            if (string.Equals(element.Usage, "M", StringComparison.Ordinal) && !present)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.ElementConstraint,
                    $"{Target(definition.Tag, element)} is required.",
                    Target(definition.Tag, element)));
                continue;
            }

            if (!present || value is null || HasComponents(definition, element.Key))
            {
                continue;
            }

            if (element.MinLength is int min && value.Length < min
                || element.MaxLength is int max && value.Length > max)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.ElementConstraint,
                    $"{Target(definition.Tag, element)} length {value.Length} is outside {element.MinLength} to {element.MaxLength}.",
                    Target(definition.Tag, element)));
            }

            if (element.AllowedValues is not null
                && !element.AllowedValues.Contains(value, StringComparer.Ordinal))
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.ElementConstraint,
                    $"{Target(definition.Tag, element)} value '{value}' is not allowed.",
                    Target(definition.Tag, element)));
            }
        }

        foreach (var key in token.Elements.Keys)
        {
            if (key.Contains('-', StringComparison.Ordinal))
            {
                continue;
            }

            if (!definition.Elements.ContainsKey(key))
            {
                var target = definition.Tag + key;
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.ElementConstraint,
                    $"{target} is not an element of {definition.Tag}.",
                    target));
            }
        }

        return new X12Segment(token.Tag, token.Elements);
    }

    private static bool HasComponents(CompiledSegment definition, string key)
    {
        var prefix = key + "-";
        return definition.Elements.Keys.Any(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string Target(string tag, CompiledElement element)
    {
        return element.Reference ?? tag + element.Key;
    }
}
