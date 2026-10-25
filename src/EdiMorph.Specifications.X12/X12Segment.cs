namespace EdiMorph.Specifications.X12;

/// <summary>
/// One segment occurrence. Element positions are "01", "02", and composite components are "04-01".
/// A missing or empty value is not present.
/// </summary>
public sealed class X12Segment
{
    public X12Segment(string tag, IReadOnlyDictionary<string, string?>? elements = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        Tag = tag;
        Elements = elements ?? new Dictionary<string, string?>(StringComparer.Ordinal);
    }

    public string Tag { get; }

    public IReadOnlyDictionary<string, string?> Elements { get; }

    public static X12Segment With(string tag, params (string Position, string? Value)[] elements)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (position, value) in elements)
        {
            values[position] = value;
        }

        return new X12Segment(tag, values);
    }
}
