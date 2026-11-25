using EdiMorph.Diagnostics;
using EdiMorph.Specifications.Tests.Factories;

namespace EdiMorph.Specifications.Tests;

public sealed class OccurrenceDialectTests
{
    [Theory]
    [InlineData("partner-dialect", "x12.v4010.transaction.204", true)]
    [InlineData("standard-subset", "x12.v4010.transaction.204", false)]
    [InlineData("partner-dialect", "", false)]
    [InlineData("partner-dialect", "wrong-parent", false)]
    public async Task Compile_WhenOccurrenceIsWidened_ShouldRequireDialectAndCorrectParent(
        string compliance, string parentCitation, bool succeeds)
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.CreateX12(
            SpecificationCompilerFactory.CreateStandardRules(),
            new SpecificationText("fixture/contact.override.rule", $$"""
            {
              "ruleFormatVersion": 1, "id": "fixture.contacts",
              "kind": "transaction-override", "extends": "x12.v4010.transaction.204",
              "operations": [{ "op": "loosenOccurrence", "target": "detail.0300.0310.G61",
                "maxOccurs": 4, "departsFrom": "{{parentCitation}}" }]
            }
            """),
            new SpecificationText("fixture/contact.profile.rule", $$"""
            {
              "ruleFormatVersion": 1, "id": "fixture.contacts.profile", "kind": "provider-profile",
              "format": "X12", "version": "004010", "provider": "FixtureContacts",
              "profileRevision": "1", "transactions": ["204"], "compliance": "{{compliance}}",
              "overrides": ["fixture.contacts"]
            }
            """));
        var standard = await compiler.CompileAsync(new SpecificationRequest("X12", "004010", "204"));

        // Act
        var result = await compiler.CompileAsync(new SpecificationRequest("X12", "004010", "204", "FixtureContacts", "1"));
        var standardAgain = await compiler.CompileAsync(new SpecificationRequest("X12", "004010", "204"));

        // Assert
        result.IsSuccess.Should().Be(succeeds);
        standardAgain.Should().BeSameAs(standard);
        Find(standard.Transaction!.Detail, "detail.0300.0310.G61")!.MaxOccurs.Should().Be(3);
        if (succeeds)
        {
            var contact = Find(result.Transaction!.Detail, "detail.0300.0310.G61")!;
            contact.MaxOccurs.Should().Be(4);
            contact.Origins.Should().Contain(origin => origin.DocumentId == "fixture.contacts");
        }
        else
        {
            result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.StandardLoosened);
        }
    }

    private static CompiledNode? Find(IReadOnlyList<CompiledNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id) return node;
            var child = Find(node.Children, id);
            if (child is not null) return child;
        }

        return null;
    }
}
