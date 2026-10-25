using EdiMorph.Diagnostics;
using EdiMorph.Specifications;

namespace EdiMorph.X12;

internal static class X12Tokenizer
{
    public static bool TryTokenize(
        string interchange,
        X12InterchangeSyntax syntax,
        CompiledSpecification specification,
        List<SpecificationDiagnostic> diagnostics,
        out List<X12Token> tokens)
    {
        tokens = [];
        if (interchange.Length < syntax.HeaderLength
            || !interchange.StartsWith(syntax.HeaderTag, StringComparison.Ordinal))
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.MalformedInterchange,
                $"The interchange must begin with a {syntax.HeaderLength} character {syntax.HeaderTag} header.",
                syntax.HeaderTag));
            return false;
        }

        var elementSeparator = interchange[syntax.ElementSeparatorOffset];
        var componentSeparator = interchange[syntax.ComponentSeparatorOffset];
        var segmentTerminator = interchange[syntax.SegmentTerminatorOffset];
        if (elementSeparator == componentSeparator
            || elementSeparator == segmentTerminator
            || componentSeparator == segmentTerminator)
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.MalformedInterchange,
                "The element separator, component separator, and segment terminator must be distinct.",
                syntax.HeaderTag));
            return false;
        }

        if (!specification.Segments.TryGetValue(specification.Envelope!.HeaderSegmentRef, out var header))
        {
            throw new InvalidOperationException("The compiled envelope does not include its header segment.");
        }

        if (!TryReadFixedHeader(interchange, syntax, header, elementSeparator, segmentTerminator, diagnostics, out var headerToken))
        {
            return false;
        }

        tokens.Add(headerToken);
        var index = syntax.HeaderLength;
        while (index < interchange.Length)
        {
            while (index < interchange.Length && interchange[index] is '\r' or '\n')
            {
                index++;
            }

            if (index >= interchange.Length)
            {
                break;
            }

            var start = index;
            while (index < interchange.Length && interchange[index] != segmentTerminator)
            {
                index++;
            }

            if (index >= interchange.Length)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.MalformedInterchange,
                    "A segment is missing its terminator.",
                    syntax.HeaderTag));
                return false;
            }

            var raw = interchange[start..index];
            index++;
            if (raw.Length == 0)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.MalformedInterchange,
                    "The interchange contains an empty segment.",
                    syntax.HeaderTag));
                return false;
            }

            tokens.Add(ReadDelimited(raw, elementSeparator, componentSeparator));
        }

        return true;
    }

    private static bool TryReadFixedHeader(
        string interchange,
        X12InterchangeSyntax syntax,
        CompiledSegment header,
        char elementSeparator,
        char segmentTerminator,
        List<SpecificationDiagnostic> diagnostics,
        out X12Token token)
    {
        var elements = new Dictionary<string, string?>(StringComparer.Ordinal);
        var cursor = syntax.ElementSeparatorOffset;
        foreach (var (key, width) in FixedWidths(header))
        {
            if (cursor >= syntax.HeaderLength || interchange[cursor] != elementSeparator)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.MalformedInterchange,
                    "The ISA header does not match the compiled element widths.",
                    header.Tag));
                token = new X12Token(header.Tag, elements);
                return false;
            }

            cursor++;
            if (cursor + width > syntax.SegmentTerminatorOffset)
            {
                diagnostics.Add(ReaderDiagnostics.Error(
                    DiagnosticCodes.MalformedInterchange,
                    "The ISA header does not match the compiled element widths.",
                    header.Tag));
                token = new X12Token(header.Tag, elements);
                return false;
            }

            var value = interchange.Substring(cursor, width);
            if (value.Length > 0)
            {
                elements[key] = value;
            }

            cursor += width;
        }

        if (cursor != syntax.SegmentTerminatorOffset || interchange[cursor] != segmentTerminator)
        {
            diagnostics.Add(ReaderDiagnostics.Error(
                DiagnosticCodes.MalformedInterchange,
                "The ISA header does not match the compiled element widths.",
                header.Tag));
            token = new X12Token(header.Tag, elements);
            return false;
        }

        token = new X12Token(header.Tag, elements);
        return true;
    }

    private static IEnumerable<(string Key, int Width)> FixedWidths(CompiledSegment header)
    {
        foreach (var pair in header.Elements.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (pair.Key.Length != 2 || !pair.Key.All(char.IsDigit))
            {
                continue;
            }

            if (pair.Value.MinLength is not int width || width != pair.Value.MaxLength || width <= 0)
            {
                throw new InvalidOperationException($"ISA element {pair.Key} has no fixed width.");
            }

            yield return (pair.Key, width);
        }
    }

    private static X12Token ReadDelimited(string raw, char elementSeparator, char componentSeparator)
    {
        var separator = raw.IndexOf(elementSeparator);
        var tag = separator < 0 ? raw : raw[..separator];
        var elements = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (separator >= 0)
        {
            var fields = raw[(separator + 1)..].Split(elementSeparator);
            for (var index = 0; index < fields.Length; index++)
            {
                if (fields[index].Length == 0)
                {
                    continue;
                }

                var position = (index + 1).ToString("00");
                elements[position] = fields[index];
                if (!fields[index].Contains(componentSeparator))
                {
                    continue;
                }

                var components = fields[index].Split(componentSeparator);
                for (var component = 0; component < components.Length; component++)
                {
                    if (components[component].Length == 0)
                    {
                        continue;
                    }

                    elements[$"{position}-{(component + 1).ToString("00")}"] = components[component];
                }
            }
        }

        return new X12Token(tag, elements);
    }
}
