using EdiMorph.Specifications.X12.V4010;
using EdiMorph.X12.Tests.Factories;

namespace EdiMorph.X12.Tests;

public sealed class X12ReaderTests
{
    private readonly X12Reader _sut = new();

    private readonly RuleRegistry _rules = CreateRules();

    private readonly CompiledSpecification _specification = Compile();

    [Fact]
    public async Task Read_When204InterchangeMatchesTheSpecification_ShouldSucceed()
    {
        // Arrange
        var interchange = X12InterchangeFactory.Valid204();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
        result.Envelope.Should().NotBeNull();
        var envelope = result.Envelope!;
        envelope.HeaderControlNumber.Should().Be(X12InterchangeFactory.InterchangeControlNumber);
        envelope.TrailerControlNumber.Should().Be(X12InterchangeFactory.InterchangeControlNumber);
        var group = envelope.Groups.Should().ContainSingle().Subject;
        group.HeaderControlNumber.Should().Be(X12InterchangeFactory.GroupControlNumber);
        var transaction = group.Transactions.Should().ContainSingle().Subject;
        transaction.Segments.Select(segment => segment.Tag).Should().Equal("ST", "B2", "B2A", "S5", "SE");
        transaction.SegmentCount.Should().Be(transaction.Segments.Count);
        var tender = result.Transactions.Should().ContainSingle().Subject;
        tender.Beginning!.Elements["02"].Should().Be(X12InterchangeFactory.CarrierCode);
        tender.Stops.Should().ContainSingle().Which.StopOff!.Elements["01"].Should().Be(X12InterchangeFactory.FirstStop);
        tender.Parties.Should().BeEmpty();
        tender.Extensions.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_WhenTheDetailLoopRepeats_ShouldKeepBothStops()
    {
        // Arrange
        var interchange = X12InterchangeFactory.Valid204WithTwoStops();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Envelope!.Groups.Single().Transactions.Single();
        transaction.Segments
            .Where(segment => segment.Tag == "S5")
            .Select(segment => segment.Elements["01"])
            .Should()
            .Equal(X12InterchangeFactory.FirstStop, X12InterchangeFactory.SecondStop);
        result.Transactions.Single().Stops
            .Select(stop => stop.StopOff!.Elements["01"])
            .Should()
            .Equal(X12InterchangeFactory.FirstStop, X12InterchangeFactory.SecondStop);
    }

    [Fact]
    public async Task Read_WhenEachStopHasAParty_ShouldKeepThePartyOnThatStop()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithPartyOnEachStop();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tender = result.Transactions.Should().ContainSingle().Subject;
        tender.Parties.Should().BeEmpty();
        tender.Stops.Should().HaveCount(2);
        tender.Stops[0].Party!.Name!.Elements["02"].Should().Be(X12InterchangeFactory.ShipFromName);
        tender.Stops[1].Party!.Name!.Elements["02"].Should().Be(X12InterchangeFactory.ShipToName);
    }

    [Fact]
    public async Task Read_WhenAStopContainsAnUnknownSegment_ShouldKeepItAsAnExtension()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithExtensionOnTheFirstStop();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCodes.Structure);
        diagnostic.Target.Should().Be(X12InterchangeFactory.ExtensionTag);
        var tender = result.Transactions.Should().ContainSingle().Subject;
        tender.Extensions.Should().BeEmpty();
        var extension = tender.Stops.Should().ContainSingle().Subject.Extensions.Should().ContainSingle().Subject;
        extension.Tag.Should().Be(X12InterchangeFactory.ExtensionTag);
        extension.Elements["01"].Should().Be(X12InterchangeFactory.ExtensionValue);
    }

    [Fact]
    public async Task Read_WhenHeaderIsShorterThanTheIsaHeader_ShouldFail()
    {
        // Arrange
        var interchange = X12InterchangeFactory.ShortHeader();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Envelope.Should().BeNull();
        result.Diagnostics.Should().ContainSingle().Which.Code.Should().Be(DiagnosticCodes.MalformedInterchange);
    }

    [Fact]
    public async Task Read_WhenDelimitersAreNotDistinct_ShouldFail()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithSameSeparators();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Envelope.Should().BeNull();
        result.Diagnostics.Should().ContainSingle().Which.Code.Should().Be(DiagnosticCodes.MalformedInterchange);
    }

    [Fact]
    public async Task Read_WhenTrailerControlNumberDiffers_ShouldReportEnvelopeIntegrity()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithTrailerControl(X12InterchangeFactory.DifferentControlNumber);

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle().Which.Code.Should().Be(DiagnosticCodes.EnvelopeIntegrity);
    }

    [Fact]
    public async Task Read_WhenStopSequenceDoesNotIncrease_ShouldReportTransactionSyntax()
    {
        // Arrange
        var interchange = X12InterchangeFactory.CreateOutOfOrderStops();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCodes.TransactionSyntax);
        diagnostic.Target.Should().Be("S501");
    }

    [Fact]
    public async Task Read_WhenMandatorySegmentIsMissing_ShouldReportStructure()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithoutBeginningSegment();

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCodes.Structure);
        diagnostic.Target.Should().Be("heading.B2");
    }

    [Fact]
    public async Task Read_WhenElementExceedsMaxLength_ShouldReportElementConstraint()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithCarrierCode(X12InterchangeFactory.LongCarrierCode);

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCodes.ElementConstraint);
        diagnostic.Target.Should().Be("B202");
    }

    [Fact]
    public async Task Read_WhenNoInformationQualifierHasData_ShouldReportRelationalSyntax()
    {
        // Arrange
        var interchange = X12InterchangeFactory.WithAuthorizationInformation(X12InterchangeFactory.AuthorizationData);

        // Act
        var result = await _sut.ReadAsync(_specification, _rules, interchange);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(DiagnosticCodes.RelationalSyntax);
        diagnostic.Target.Should().Be("ISA02");
    }

    private static RuleRegistry CreateRules()
    {
        var rules = new RuleRegistry();
        X12V4010Rules.Register(rules);
        return rules;
    }

    private static CompiledSpecification Compile()
    {
        var rules = CreateRules();
        var documents = X12RuleDocuments.ReadAll()
            .Select(document => new SpecificationText(document.Path, document.Content));
        var catalog = new SpecificationCatalog();
        catalog.Add(new InMemorySpecificationSource(documents));
        var compiler = new SpecificationCompiler(catalog, rules);
        var specification = compiler.CompileAsync(new SpecificationRequest(
            X12InterchangeFactory.Format,
            X12InterchangeFactory.Version,
            X12InterchangeFactory.TransactionCode)).AsTask().GetAwaiter().GetResult();
        if (!specification.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(
                Environment.NewLine,
                specification.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return specification;
    }
}
