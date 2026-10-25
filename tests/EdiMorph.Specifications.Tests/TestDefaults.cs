using EdiMorph;

namespace EdiMorph.Specifications.Tests;

internal static class TestDefaults
{
    public const string X12Format = "X12";

    public const string X12Version = "004010";

    public const string Transaction204 = "204";

    public const string MissingTransaction = "999";

    public const string Transaction204Id = "x12.v4010.transaction.204";

    public const string FunctionalGroup204 = "SM";

    public const string EnvelopeIntegrityRuleId = "X12_V4010_ENVELOPE_INTEGRITY";

    public const string IsaAuthorizationRuleId = "X12_V4010_ISA_AUTH_SEMANTICS";

    public const string FixtureGuideRuleId = "FIXTURE_CARRIER_204";

    public const string FixtureSegmentId = "fixture.segment.ZZ";

    public const string FixtureTransactionOverrideId = "fixture.transaction.204.zz";

    public const string FixtureProviderOverrideId = "fixture.segment.ZZ.provider";

    public const string FixtureCarrier = "FixtureCarrier";

    public const string FixtureCarrierTwo = "FixtureCarrierTwo";

    public const string ProfileRevision = "1";

    public const string OptionalUsage = "O";

    public const string MandatoryUsage = "M";

    public const string CodeA = "A";

    public const string CodeB = "B";

    public const string CodeC = "C";

    public const string DialectCode = "Z";

    public const string DialectSegmentId = "fixture.segment.DIALECT";

    public const string FixtureSubset = "FixtureSubset";

    public const string FixtureDialect = "FixtureDialect";

    public const string MissingParentId = "fixture.missing.parent";

    public static SpecificationRequest Request204(string? provider = null)
    {
        return new SpecificationRequest(
            X12Format,
            X12Version,
            Transaction204,
            provider,
            provider is null ? null : ProfileRevision);
    }
}
