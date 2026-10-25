using EdiMorph.Diagnostics;
using EdiMorph.Rules;
using EdiMorph.Specifications.Tests.Factories;

namespace EdiMorph.Specifications.Tests;

public sealed class X12CompilationTests
{
    private readonly SpecificationCompiler _sut = SpecificationCompilerFactory.CreateX12();

    [Fact]
    public async Task Compile_WhenTransaction204IsRequested_ShouldCompileTheStandard()
    {
        // Act
        var result = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeTrue(because: Format(result));
        result.Diagnostics.Should().BeEmpty();
        result.Identity.Compliance.Should().Be(ComplianceMode.StandardSubset);
        result.SyntaxId.Should().Be("x12.syntax.common");
        result.Envelope!.HeaderSegmentRef.Should().Be("x12.segment.ISA.v4010");
        result.Envelope.TrailerSegmentRef.Should().Be("x12.segment.IEA.v4010");
        result.Segments.Should().ContainKey("x12.segment.B2.v4010");
        result.Transaction!.Code.Should().Be(TestDefaults.Transaction204);
        result.Transaction.FunctionalGroup.Should().Be(TestDefaults.FunctionalGroup204);
        result.Transaction.Heading.Should().Contain(node => node.Segment == "B2" && node.Usage == TestDefaults.MandatoryUsage);
        result.Transaction.Detail.Should().Contain(node =>
            node.LoopId == "0300" && node.MinOccurs == 1 && node.MaxOccurs == 999);
        result.Handlers.Should().ContainSingle(binding =>
            binding.RuleId == TestDefaults.EnvelopeIntegrityRuleId && binding.Mandatory);
    }

    [Fact]
    public async Task Compile_WhenCalledTwice_ShouldReturnTheSameBaseInstance()
    {
        // Act
        var first = await _sut.CompileAsync(TestDefaults.Request204());
        var second = await _sut.CompileAsync(TestDefaults.Request204());

        // Assert
        first.IsSuccess.Should().BeTrue(because: Format(first));
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task Compile_WhenTransactionIsOmitted_ShouldReturnTheEnvelopeAndDictionary()
    {
        // Act
        var result = await _sut.CompileAsync(new SpecificationRequest(TestDefaults.X12Format, TestDefaults.X12Version));

        // Assert
        result.IsSuccess.Should().BeTrue(because: Format(result));
        result.Transaction.Should().BeNull();
        result.Envelope.Should().NotBeNull();
        result.Segments.Should().ContainKey("x12.segment.ISA.v4010");
    }

    [Fact]
    public async Task Compile_WhenTransactionIsMissing_ShouldFail()
    {
        // Act
        var result = await _sut.CompileAsync(new SpecificationRequest(
            TestDefaults.X12Format,
            TestDefaults.X12Version,
            TestDefaults.MissingTransaction));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.ReferencedSegmentMissing
            && diagnostic.Target == TestDefaults.MissingTransaction);
    }

    [Fact]
    public async Task Compile_WhenHandlerIsMissing_ShouldFailWithHandlerMissing()
    {
        // Arrange
        var compiler = SpecificationCompilerFactory.CreateX12(new RuleRegistry());

        // Act
        var result = await compiler.CompileAsync(TestDefaults.Request204());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.HandlerMissing
            && diagnostic.RuleId == TestDefaults.EnvelopeIntegrityRuleId
            && diagnostic.Category == DiagnosticCategory.Standard);
    }

    private static string Format(CompiledSpecification result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message));
    }
}
