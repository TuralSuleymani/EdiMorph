namespace EdiMorph.X12;

internal sealed record X12Token(string Tag, IReadOnlyDictionary<string, string?> Elements);
