using EdiMorph.Diagnostics;
using EdiMorph.Specifications.Tests.Factories;

namespace EdiMorph.Specifications.Tests;

public sealed class RuleAuthoringTests
{
    [Fact]
    public async Task Compile_WhenJsonContainsFieldEquals_ShouldFailWithJsonRelationalOperator()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.CreateX12(
            additional: FixtureRuleDocuments.RelationalSegment());

        // Act
        var result = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.JsonRelationalOperator
            && diagnostic.Category == DiagnosticCategory.Standard);
    }

    [Fact]
    public async Task Compile_WhenCompilationFails_ShouldNotReuseTheResult()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.CreateX12(
            additional: FixtureRuleDocuments.RelationalSegment());

        // Act
        var first = await compiler.CompileAsync(TestDefaults.Request204());
        var second = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        first.IsSuccess.Should().BeFalse();
        second.IsSuccess.Should().BeFalse();
        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public async Task Compile_WhenRuleAndOverrideShareAScope_ShouldFailWithAmbiguousDefinition()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.Create(
            [FixtureRuleDocuments.SameScopeRule(), FixtureRuleDocuments.SameScopeOverride()]);

        // Act
        var result = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCodes.AmbiguousDefinition);
    }

    [Fact]
    public async Task Compile_WhenOverrideParentIsMissing_ShouldFailWithReferencedSegmentMissing()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.Create([FixtureRuleDocuments.MissingParentOverride()]);

        // Act
        var result = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ReferencedSegmentMissing
            && diagnostic.Target == TestDefaults.MissingParentId);
    }

    [Fact]
    public async Task Compile_WhenExtendsIsCircular_ShouldFailWithCircularInheritance()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.Create(
            [FixtureRuleDocuments.CycleLeft(), FixtureRuleDocuments.CycleRight()]);

        // Act
        var result = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.CircularInheritance);
    }
}
