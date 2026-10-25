using EdiMorph.Specifications.X12.V4010.Common.Segments.ISA;

namespace EdiMorph.Specifications.X12.Tests;

public sealed class IsaAuthorizationSemanticsTests
{
    private readonly IsaAuthorizationSemantics _sut = new();

    private readonly RuleContext _context = new(new SpecificationRequest("X12", "004010"));

    [Fact]
    public async Task Validate_WhenNoInformationQualifiersUseTenSpaces_ShouldSucceed()
    {
        // Arrange
        var segment = X12Segment.With(
            "ISA",
            ("01", IsaAuthorizationSemantics.NoInformation),
            ("02", new string(' ', IsaAuthorizationSemantics.InformationLength)),
            ("03", IsaAuthorizationSemantics.NoInformation),
            ("04", new string(' ', IsaAuthorizationSemantics.InformationLength)));

        // Act
        var result = await _sut.ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenNoInformationQualifierHasData_ShouldFail()
    {
        // Arrange
        var segment = X12Segment.With(
            "ISA",
            ("01", IsaAuthorizationSemantics.NoInformation),
            ("02", "AUTHINFO  "),
            ("03", "01"),
            ("04", "PASSWORD  "));

        // Act
        var result = await _sut.ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Target == "ISA02");
    }
}
