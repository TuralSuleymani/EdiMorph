using EdiMorph.Diagnostics;
using EdiMorph.Specifications.X12.V4010.Common.Segments.AT5;
using EdiMorph.Specifications.X12.V4010.Common.Segments.AT8;
using EdiMorph.Specifications.X12.V4010.Common.Segments.G62;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L11;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.L5;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LAD;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LEP;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LFH;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH2;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH4;
using EdiMorph.Specifications.X12.V4010.Common.Segments.LH6;
using EdiMorph.Specifications.X12.V4010.Common.Segments.MEA;
using EdiMorph.Specifications.X12.V4010.Common.Segments.MS3;
using EdiMorph.Specifications.X12.V4010.Common.Segments.N1;
using EdiMorph.Specifications.X12.V4010.Common.Segments.N7;
using EdiMorph.Specifications.X12.V4010.Common.Segments.OID;
using EdiMorph.Specifications.X12.V4010.Common.Segments.PLD;
using EdiMorph.Specifications.X12.V4010.Common.Segments.S5;

namespace EdiMorph.Specifications.X12.Tests;

public sealed class X12SegmentSyntaxTests
{
    private readonly RuleContext _context = new(new SpecificationRequest("X12", "004010", "204"));

    public static TheoryData<IRuleValidator<X12Segment>, X12Segment> ValidSegments => new()
    {
        { new L11Syntax(), X12Segment.With("L11", ("01", "REF"), ("02", "BM")) },
        { new G62Syntax(), X12Segment.With("G62", ("01", "11"), ("02", "20260101")) },
        { new MS3Syntax(), X12Segment.With("MS3", ("01", "SCAC")) },
        { new AT5Syntax(), X12Segment.With("AT5", ("01", "HM")) },
        { new PLDSyntax(), X12Segment.With("PLD", ("01", "1")) },
        { new LH6Syntax(), X12Segment.With("LH6", ("01", "Signer")) },
        { new N1Syntax(), X12Segment.With("N1", ("01", "SH"), ("02", "Acme")) },
        { new N7Syntax(), X12Segment.With("N7", ("02", "123456")) },
        { new S5Syntax(), X12Segment.With("S5", ("01", "1"), ("02", "LD")) },
        { new AT8Syntax(), X12Segment.With("AT8", ("01", "G"), ("02", "L"), ("03", "100")) },
        { new LADSyntax(), X12Segment.With("LAD") },
        { new L5Syntax(), X12Segment.With("L5", ("02", "Freight")) },
        { new LH2Syntax(), X12Segment.With("LH2", ("01", "3")) },
        { new LH3Syntax(), X12Segment.With("LH3", ("01", "Paint"), ("02", "D")) },
        { new LFHSyntax(), X12Segment.With("LFH", ("01", "TEC"), ("02", "Description")) },
        { new LEPSyntax(), X12Segment.With("LEP", ("01", "D001")) },
        { new LH4Syntax(), X12Segment.With("LH4", ("01", "PLAN")) },
        { new OIDSyntax(), X12Segment.With("OID", ("01", "ORDER1")) },
        { new L3Syntax(), X12Segment.With("L3", ("01", "100"), ("02", "G")) },
        { new MEASyntax(), X12Segment.With("MEA", ("03", "10")) }
    };

    public static TheoryData<IRuleValidator<X12Segment>, X12Segment> InvalidSegments => new()
    {
        { new L11Syntax(), X12Segment.With("L11", ("01", "REF")) },
        { new G62Syntax(), X12Segment.With("G62") },
        { new MS3Syntax(), X12Segment.With("MS3", ("05", "TX")) },
        { new AT5Syntax(), X12Segment.With("AT5", ("01", "HM"), ("03", "Handle upright")) },
        { new PLDSyntax(), X12Segment.With("PLD", ("03", "L")) },
        { new LH6Syntax(), X12Segment.With("LH6", ("02", "1")) },
        { new N1Syntax(), X12Segment.With("N1", ("01", "SH")) },
        { new N7Syntax(), X12Segment.With("N7", ("03", "100")) },
        { new S5Syntax(), X12Segment.With("S5", ("03", "100")) },
        { new AT8Syntax(), X12Segment.With("AT8", ("01", "G")) },
        { new LADSyntax(), X12Segment.With("LAD", ("01", "PLT")) },
        { new L5Syntax(), X12Segment.With("L5", ("07", "SM")) },
        { new LH2Syntax(), X12Segment.With("LH2", ("06", "FA")) },
        { new LH3Syntax(), X12Segment.With("LH3", ("01", "Paint")) },
        { new LFHSyntax(), X12Segment.With("LFH", ("05", "LB")) },
        { new LEPSyntax(), X12Segment.With("LEP", ("03", "TX")) },
        { new LH4Syntax(), X12Segment.With("LH4", ("08", "10")) },
        { new OIDSyntax(), X12Segment.With("OID") },
        { new L3Syntax(), X12Segment.With("L3", ("12", "L")) },
        { new MEASyntax(), X12Segment.With("MEA") }
    };

    [Theory]
    [MemberData(nameof(ValidSegments))]
    public async Task Validate_WhenSyntaxNotesAreSatisfied_ShouldSucceed(
        IRuleValidator<X12Segment> validator,
        X12Segment segment)
    {
        // Act
        var result = await validator.ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidSegments))]
    public async Task Validate_WhenSyntaxNoteFails_ShouldReportRelationalSyntax(
        IRuleValidator<X12Segment> validator,
        X12Segment segment)
    {
        // Act
        var result = await validator.ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.RelationalSyntax);
    }

    [Fact]
    public async Task Validate_WhenMeasurementRangeLacksTheComposite_ShouldFail()
    {
        // Arrange
        var segment = X12Segment.With("MEA", ("03", "10"), ("05", "1"));

        // Act
        var result = await new MEASyntax().ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Target == "MEA05");
    }

    [Fact]
    public async Task Validate_WhenMeasurementValueAndAttributeAreBothPresent_ShouldFail()
    {
        // Arrange
        var segment = X12Segment.With("MEA", ("03", "10"), ("08", "A"));

        // Act
        var result = await new MEASyntax().ValidateAsync(segment, _context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Target == "MEA08");
    }
}
