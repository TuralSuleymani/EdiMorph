using EdiMorph.Diagnostics;
using EdiMorph.Specifications.X12.V4010.Common.Envelope;

namespace EdiMorph.Specifications.X12.Tests;

public sealed class EnvelopeIntegrityTests
{
    private const string InterchangeControlNumber = "000000001";

    private const string GroupControlNumber = "000000001";

    private const string TransactionControlNumber = "0001";

    private const int OneIncluded = 1;

    private readonly EnvelopeIntegrity _sut = new();

    private readonly RuleContext _context = new(new SpecificationRequest("X12", "004010", "204"));

    [Fact]
    public async Task Validate_WhenControlNumbersAndCountsMatch_ShouldSucceed()
    {
        // Arrange
        var envelope = Envelope(OneIncluded, OneIncluded, OneIncluded);

        // Act
        var result = await _sut.ValidateAsync(envelope, _context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenTrailerControlNumberDiffers_ShouldFail()
    {
        // Arrange
        var envelope = new X12Envelope(InterchangeControlNumber, "000000002", OneIncluded, [Group(OneIncluded, OneIncluded)]);

        // Act
        var result = await _sut.ValidateAsync(envelope, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.EnvelopeIntegrity
            && diagnostic.Message.Contains("ISA13", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Validate_WhenDeclaredGroupCountDiffers_ShouldFail()
    {
        // Arrange
        var envelope = new X12Envelope(InterchangeControlNumber, InterchangeControlNumber, declaredGroupCount: 2, [Group(OneIncluded, OneIncluded)]);

        // Act
        var result = await _sut.ValidateAsync(envelope, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("IEA01", StringComparison.Ordinal));
    }

    private static X12Envelope Envelope(int groupCount, int transactionCount, int segmentCount)
    {
        return new X12Envelope(InterchangeControlNumber, InterchangeControlNumber, groupCount, [Group(transactionCount, segmentCount)]);
    }

    private static X12FunctionalGroup Group(int transactionCount, int segmentCount)
    {
        var transaction = new X12TransactionSet(
            TransactionControlNumber,
            TransactionControlNumber,
            segmentCount,
            segmentCount,
            [X12Segment.With("ST"), X12Segment.With("SE")]);
        return new X12FunctionalGroup(GroupControlNumber, GroupControlNumber, transactionCount, [transaction]);
    }
}
