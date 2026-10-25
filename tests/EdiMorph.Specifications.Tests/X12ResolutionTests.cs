using EdiMorph.Specifications.Tests.Factories;
using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.Tests;

public sealed class X12ResolutionTests
{
    private readonly SpecificationResolver _sut = new();

    private readonly SpecificationCatalog _catalog = SpecificationCompilerFactory.CreateX12Catalog();

    [Fact]
    public void ReadAll_WhenX12RulesAreEmbedded_ShouldExposeTheVersionPaths()
    {
        // Act
        var paths = X12RuleDocuments.ReadAll().Select(document => document.Path).ToArray();

        // Assert
        paths.Should().Contain("Common/syntax.rule");
        paths.Should().Contain("V4010/version.rule");
        paths.Should().Contain("V4010/Transactions/204/structure.rule");
    }

    [Fact]
    public void Resolve_WhenX12V4010Transaction204IsRequested_ShouldOrderSyntaxVersionEnvelopeThenSegments()
    {
        // Act
        var plan = _sut.Resolve(_catalog, TestDefaults.Request204());

        // Assert
        plan.Diagnostics.Should().BeEmpty();
        var kinds = plan.BaseDocuments.Select(document => document.Kind).ToArray();
        kinds[0].Should().Be("syntax");
        kinds[1].Should().Be("version");
        kinds[2].Should().Be("envelope");
        kinds[^1].Should().Be("transaction");
        kinds.Skip(3).SkipLast(1).Should().NotBeEmpty().And.OnlyContain(kind => kind == "segment");
        plan.BaseDocuments.Should().Contain(document => document.Id == TestDefaults.Transaction204Id);
        plan.BaseDocuments.Should().OnlyContain(document =>
            document.Path.Contains('/') && document.ContentHash.Length == 64);
    }
}
