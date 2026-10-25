namespace EdiMorph.Diagnostics;

/// <summary>
/// Stable diagnostic codes shared by the compiler and rule handlers.
/// </summary>
public static class DiagnosticCodes
{
    public const string ReferencedSegmentMissing = "EDI1001";

    public const string CircularInheritance = "EDI1002";

    public const string IllegalOverride = "EDI1003";

    public const string StandardLoosened = "EDI1004";

    public const string DuplicateOccurrence = "EDI1005";

    public const string HandlerMissing = "EDI1006";

    public const string InvalidOccurrence = "EDI1007";

    public const string AmbiguousLoop = "EDI1008";

    public const string AmbiguousDefinition = "EDI1009";

    public const string UnsupportedRuleFormat = "EDI1010";

    public const string JsonRelationalOperator = "EDI1011";

    public const string RelationalSyntax = "EDI1101";

    public const string EnvelopeIntegrity = "EDI1102";

    public const string TransactionSyntax = "EDI1103";

    public const string MalformedInterchange = "EDI1201";

    public const string Structure = "EDI1202";

    public const string ElementConstraint = "EDI1203";

    public const string HandlerSubject = "EDI1204";
}
