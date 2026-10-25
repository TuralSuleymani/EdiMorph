using EdiMorph.Specifications;

namespace EdiMorph.Specifications.Tests.Factories;

internal static class FixtureRuleDocuments
{
    public static SpecificationText Segment()
    {
        return Document(
            "fixture/ZZ.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "{{TestDefaults.FixtureSegmentId}}",
              "kind": "segment",
              "format": "{{TestDefaults.X12Format}}",
              "version": "{{TestDefaults.X12Version}}",
              "tag": "ZZ",
              "customRules": ["{{TestDefaults.IsaAuthorizationRuleId}}"],
              "elements": {
                "01": {
                  "reference": "ZZ01",
                  "usage": "{{TestDefaults.OptionalUsage}}",
                  "constraints": { "length": { "min": 1, "max": 4 }, "allowedValues": ["{{TestDefaults.CodeA}}", "{{TestDefaults.CodeB}}", "{{TestDefaults.CodeC}}"] }
                }
              }
            }
            """);
    }

    public static SpecificationText TransactionOverride()
    {
        return Document(
            "fixture/ZZ.transaction.override.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "{{TestDefaults.FixtureTransactionOverrideId}}",
              "kind": "transaction-override",
              "extends": "{{TestDefaults.Transaction204Id}}",
              "operations": [
                { "op": "changeUsage", "target": "{{TestDefaults.FixtureSegmentId}}.elements.01", "usage": "{{TestDefaults.MandatoryUsage}}" },
                { "op": "addRuleBinding", "ruleId": "{{TestDefaults.FixtureGuideRuleId}}" }
              ]
            }
            """);
    }

    public static SpecificationText ProviderOverride()
    {
        return Document(
            "fixture/ZZ.provider.override.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "{{TestDefaults.FixtureProviderOverrideId}}",
              "kind": "segment-override",
              "extends": "{{TestDefaults.FixtureSegmentId}}",
              "operations": [
                { "op": "restrictAllowedValues", "target": "elements.01", "allowedValues": ["{{TestDefaults.CodeA}}", "{{TestDefaults.CodeB}}"] }
              ]
            }
            """);
    }

    public static SpecificationText CarrierProfile()
    {
        return Profile(
            "fixture/carrier/profile.rule",
            "fixture.provider.carrier",
            TestDefaults.FixtureCarrier,
            "standard-subset",
            $$"""["{{TestDefaults.FixtureTransactionOverrideId}}", "{{TestDefaults.FixtureProviderOverrideId}}"]""");
    }

    public static SpecificationText SecondCarrierProfile()
    {
        return Profile(
            "fixture/carrier-two/profile.rule",
            "fixture.provider.carrier-two",
            TestDefaults.FixtureCarrierTwo,
            "standard-subset",
            "[]");
    }

    public static SpecificationText DialectSegment()
    {
        return Document(
            "fixture/dialect/SEG.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "{{TestDefaults.DialectSegmentId}}",
              "kind": "segment",
              "format": "{{TestDefaults.X12Format}}",
              "version": "{{TestDefaults.X12Version}}",
              "tag": "SEG",
              "elements": {
                "01": {
                  "reference": "SEG01",
                  "usage": "{{TestDefaults.MandatoryUsage}}",
                  "constraints": { "length": { "min": 1, "max": 2 }, "allowedValues": ["{{TestDefaults.CodeA}}", "{{TestDefaults.CodeB}}"] }
                }
              }
            }
            """);
    }

    public static SpecificationText DialectWidenOverride()
    {
        return Document(
            "fixture/dialect/SEG.widen.override.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "fixture.segment.DIALECT.widen",
              "kind": "segment-override",
              "extends": "{{TestDefaults.DialectSegmentId}}",
              "operations": [
                {
                  "op": "widenAllowedValues",
                  "target": "elements.01",
                  "allowedValues": ["{{TestDefaults.DialectCode}}"],
                  "departsFrom": "{{TestDefaults.DialectSegmentId}}"
                }
              ]
            }
            """);
    }

    public static SpecificationText SubsetProfile()
    {
        return Profile(
            "fixture/dialect/subset.profile.rule",
            "fixture.provider.subset",
            TestDefaults.FixtureSubset,
            "standard-subset",
            """["fixture.segment.DIALECT.widen"]""");
    }

    public static SpecificationText DialectProfile()
    {
        return Profile(
            "fixture/dialect/dialect.profile.rule",
            "fixture.provider.dialect",
            TestDefaults.FixtureDialect,
            "partner-dialect",
            """["fixture.segment.DIALECT.widen"]""");
    }

    public static SpecificationText RelationalSegment()
    {
        return Document(
            "fixture/BAD.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "fixture.segment.BAD",
              "kind": "segment",
              "format": "{{TestDefaults.X12Format}}",
              "version": "{{TestDefaults.X12Version}}",
              "tag": "BAD",
              "rules": [{ "type": "fieldEquals", "left": "01", "right": "02" }],
              "elements": { "01": { "reference": "BAD01", "usage": "{{TestDefaults.OptionalUsage}}" } }
            }
            """);
    }

    public static SpecificationText SameScopeRule()
    {
        return Document(
            "fixture/ZZ.rule",
            """
            {
              "ruleFormatVersion": 1,
              "id": "fixture.segment.ZZ",
              "kind": "segment",
              "format": "X12",
              "version": "004010",
              "tag": "ZZ"
            }
            """);
    }

    public static SpecificationText SameScopeOverride()
    {
        return Document(
            "fixture/ZZ.override.rule",
            """
            {
              "ruleFormatVersion": 1,
              "id": "fixture.segment.ZZ.override",
              "kind": "segment-override",
              "extends": "fixture.segment.ZZ",
              "operations": []
            }
            """);
    }

    public static SpecificationText MissingParentOverride()
    {
        return Document(
            "fixture/missing.override.rule",
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "fixture.segment.missing",
              "kind": "segment-override",
              "extends": "{{TestDefaults.MissingParentId}}",
              "operations": []
            }
            """);
    }

    public static SpecificationText CycleLeft()
    {
        return Document(
            "fixture/cycle-left.override.rule",
            """
            {
              "ruleFormatVersion": 1,
              "id": "fixture.override.left",
              "kind": "segment-override",
              "extends": "fixture.override.right",
              "operations": []
            }
            """);
    }

    public static SpecificationText CycleRight()
    {
        return Document(
            "fixture/cycle-right.override.rule",
            """
            {
              "ruleFormatVersion": 1,
              "id": "fixture.override.right",
              "kind": "segment-override",
              "extends": "fixture.override.left",
              "operations": []
            }
            """);
    }

    private static SpecificationText Profile(
        string path,
        string id,
        string provider,
        string compliance,
        string overrides)
    {
        return Document(
            path,
            $$"""
            {
              "ruleFormatVersion": 1,
              "id": "{{id}}",
              "kind": "provider-profile",
              "format": "{{TestDefaults.X12Format}}",
              "version": "{{TestDefaults.X12Version}}",
              "provider": "{{provider}}",
              "profileRevision": "{{TestDefaults.ProfileRevision}}",
              "compliance": "{{compliance}}",
              "transactions": ["{{TestDefaults.Transaction204}}"],
              "overrides": {{overrides}},
              "customRules": ["{{TestDefaults.FixtureGuideRuleId}}"]
            }
            """);
    }

    private static SpecificationText Document(string path, string content)
    {
        return new SpecificationText(path, content);
    }
}
