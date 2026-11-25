using EdiMorph;
using EdiMorph.Diagnostics;
using EdiMorph.Specifications;
using FluentAssertions;
using Tidat.Consumer;
using Xunit;

namespace Tidat.Consumer.Tests;

public sealed class Tidat204Tests
{
    private static Tidat204Reader Reader() => new(Path.Combine(AppContext.BaseDirectory, "profiles"));
    private static string Tender() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "tender.edi"));

    [Theory]
    [InlineData("00")]
    [InlineData("01")]
    [InlineData("04")]
    [InlineData("15")]
    public async Task Read_WhenPurposeIsDocumented_ShouldReturnVersionedLoadTender(string purpose)
    {
        var result = await Reader().ReadAsync(Tender().Replace("B2A*00~", $"B2A*{purpose}~"));

        result.Diagnostics.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
        var tender = result.LoadTenders.Should().ContainSingle().Subject;
        tender.ShipmentId.Should().Be("TR-EXAMPLE-01");
        tender.Purpose.Should().Be(purpose);
        tender.PaymentMethod.Should().BeNull();
        tender.Stops.Select(stop => stop.Party!.Role).Should().Equal("SF", "ST");
        result.Transactions[0].Stops[0].Party!.Contacts.Should().HaveCount(4);
    }

    [Fact]
    public async Task Read_WhenDropAndHookStopsAreUsed_ShouldAcceptPAAndDT()
    {
        var result = await Reader().ReadAsync(Tender().Replace("S5*1*LD", "S5*1*PA").Replace("S5*2*UL", "S5*2*DT"));
        result.IsSuccess.Should().BeTrue();
        result.LoadTenders[0].Stops.Select(stop => stop.Reason).Should().Equal("PA", "DT");
    }

    [Theory]
    [InlineData("N1*SF*", "N1*SH*", "N101")]
    [InlineData("N1*ST*", "N1*CN*", "N101")]
    [InlineData("L11*APPT01*AO", "L11*APPT01*CC", "L1102")]
    [InlineData("G62*98*", "G62*64*", "G6201")]
    [InlineData("*EP*0800*LT", "*EP*0800*CT", "G6205")]
    [InlineData("G61*IC*", "G61*CN*", "G6101")]
    public async Task Read_WhenContextualGuideConstraintFails_ShouldReportProviderDiagnostic(
        string original, string replacement, string target)
    {
        var result = await Reader().ReadAsync(Tender().Replace(original, replacement));
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.RuleId == Tidat204Guide.RuleId
            && diagnostic.Category == DiagnosticCategory.ProviderPolicy && diagnostic.Target == target);
    }

    [Fact]
    public async Task Read_WhenHeadingUsesShipperRole_ShouldNotApplyStopRoleRestriction()
    {
        var result = await Reader().ReadAsync(Tender());
        result.IsSuccess.Should().BeTrue();
        result.LoadTenders[0].Parties.Select(party => party.Role).Should().Contain("SH");
    }

    [Fact]
    public async Task Read_WhenOptionalStopPartyIsAbsent_ShouldNotInventMandatoryPartyRule()
    {
        var input = Tender();
        var start = input.IndexOf("N1*SF*", StringComparison.Ordinal);
        var end = input.IndexOf("L5*1*", start, StringComparison.Ordinal);
        input = WithCorrectCount(input.Remove(start, end - start));
        var result = await Reader().ReadAsync(input);
        result.IsSuccess.Should().BeTrue();
        result.LoadTenders[0].Stops[0].Party.Should().BeNull();
    }

    [Fact]
    public async Task Read_WhenPurposeIsOutsideGuide_ShouldRejectIt()
    {
        var result = await Reader().ReadAsync(Tender().Replace("B2A*00", "B2A*05"));
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.ElementConstraint);
    }

    [Fact]
    public async Task Read_WhenFifthContactIsAdded_ShouldRejectOccurrence()
    {
        var input = WithCorrectCount(Tender().Replace("L5*1*", "G61*IC*Extra Contact*TE*5550105~\nL5*1*"));
        var result = await Reader().ReadAsync(input);
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.Structure);
    }

    [Fact]
    public async Task Read_WhenStopAT5IsAdded_ShouldRejectRemovedGuideSegment()
    {
        var result = await Reader().ReadAsync(WithCorrectCount(Tender().Replace("NTE*OTH*Pickup", "AT5*EXQ~\nNTE*OTH*Pickup")));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Compile_WhenGuideIsApplied_ShouldKeepStandardUnchanged()
    {
        var reader = Reader();
        var standard = await reader.CompileAsync(standard: true);
        var guide = await reader.CompileAsync();
        var standardAgain = await reader.CompileAsync(standard: true);

        guide.IsSuccess.Should().BeTrue();
        guide.Identity.Provider.Should().Be("Tidat");
        guide.Identity.Compliance.Should().Be(ComplianceMode.PartnerDialect);
        guide.Segments["x12.segment.B2.v4010"].Elements["06"].Usage.Should().Be("O");
        standard.Segments["x12.segment.B2.v4010"].Elements["06"].Usage.Should().Be("M");
        standardAgain.Should().BeSameAs(standard);
        standard.Handlers.Should().NotContain(handler => handler.RuleId == Tidat204Guide.RuleId);
    }

    [Fact]
    public async Task Compile_WhenProfileRevisionIsUnknown_ShouldFail()
    {
        var result = await Reader().CompileAsync(revision: "unknown");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Read_WhenEnvelopeControlDoesNotMatch_ShouldKeepStandardValidation()
    {
        var result = await Reader().ReadAsync(Tender().Replace("IEA*1*000000001", "IEA*1*000000002"));
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == DiagnosticCodes.EnvelopeIntegrity);
    }

    private static string WithCorrectCount(string input)
    {
        var segments = input.Split('~').Select(segment => segment.Trim()).Where(segment => segment.Length > 0).ToArray();
        var start = Array.FindIndex(segments, segment => segment.StartsWith("ST*", StringComparison.Ordinal));
        var end = Array.FindIndex(segments, segment => segment.StartsWith("SE*", StringComparison.Ordinal));
        segments[end] = $"SE*{end - start + 1}*0001";
        return string.Join("~\n", segments) + "~";
    }
}
