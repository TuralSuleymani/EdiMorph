using EdiMorph.Diagnostics;
using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.Specifications.X12.Tests;

public sealed class X204SyntaxTests
{
    private const string FirstStop = "1";

    private const string SecondStop = "2";

    private const string Pickup = "LD";

    private const string Weight = "100";

    private const string DifferentWeight = "90";

    private const string Pounds = "L";

    private readonly X204Syntax _sut = new();

    private readonly RuleContext _context = new(new SpecificationRequest("X12", "004010", "204"));

    [Fact]
    public async Task Validate_WhenStopsIncreaseAndWeightsAgree_ShouldSucceed()
    {
        // Arrange
        var transaction = Transaction(
            Stop(FirstStop, Weight, Pounds),
            X12Segment.With("AT8", ("01", "G"), ("02", Pounds), ("03", Weight)),
            Stop(SecondStop, null, null));

        // Act
        var result = await _sut.ValidateAsync(transaction, _context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenStopSequenceDoesNotIncrease_ShouldFail()
    {
        // Arrange
        var transaction = Transaction(Stop(SecondStop, null, null), Stop(FirstStop, null, null));

        // Act
        var result = await _sut.ValidateAsync(transaction, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.TransactionSyntax && diagnostic.Target == "S501");
    }

    [Fact]
    public async Task Validate_WhenStopWeightDisagreesWithShipmentWeight_ShouldFail()
    {
        // Arrange
        var transaction = Transaction(
            Stop(FirstStop, Weight, Pounds),
            X12Segment.With("AT8", ("02", Pounds), ("03", DifferentWeight)));

        // Act
        var result = await _sut.ValidateAsync(transaction, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Target == "S503");
    }

    private static X12TransactionSet Transaction(params X12Segment[] segments)
    {
        return new X12TransactionSet("0001", "0001", segments.Length, segments.Length, segments);
    }

    private static X12Segment Stop(string sequence, string? weight, string? unit)
    {
        return weight is null
            ? X12Segment.With("S5", ("01", sequence), ("02", Pickup))
            : X12Segment.With("S5", ("01", sequence), ("02", Pickup), ("03", weight), ("04", unit));
    }
}
