using EdiMorph.Specifications.X12.V4010.Transactions;
using EdiMorph.X12.Tests.Factories;

namespace EdiMorph.X12.Tests;

public sealed class LoadTenderMapperTests
{
    [Fact]
    public void Map_When204HasCarrierPurposeAndStops_ShouldExposeThem()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.CarrierAndStops();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        tender.Carrier.Should().Be(LoadTenderTransactionFactory.Carrier);
        tender.ShipmentId.Should().Be(LoadTenderTransactionFactory.ShipmentId);
        tender.PaymentMethod.Should().Be(LoadTenderTransactionFactory.PaymentMethod);
        tender.Purpose.Should().Be(LoadTenderTransactionFactory.Purpose);
        tender.Stops.Should().HaveCount(2);
        AssertStop(tender.Stops[0], LoadTenderTransactionFactory.FirstStop, LoadTenderTransactionFactory.Pickup, LoadTenderTransactionFactory.ShipFrom, LoadTenderTransactionFactory.ShipFromName);
        AssertStop(tender.Stops[1], LoadTenderTransactionFactory.SecondStop, LoadTenderTransactionFactory.Unload, LoadTenderTransactionFactory.ShipTo, LoadTenderTransactionFactory.ShipToName);
        var shipFrom = tender.Stops[0].Party!;
        shipFrom.AddressLines.Should().ContainSingle().Which.Should().Be(LoadTenderTransactionFactory.AddressLine);
        shipFrom.City.Should().Be(LoadTenderTransactionFactory.City);
        shipFrom.State.Should().Be(LoadTenderTransactionFactory.State);
        shipFrom.PostalCode.Should().Be(LoadTenderTransactionFactory.PostalCode);
        shipFrom.Country.Should().Be(LoadTenderTransactionFactory.Country);
    }

    [Fact]
    public void Map_WhenReferenceHasAQualifier_ShouldKeepQualifierAndValue()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.WithReference();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        var reference = tender.References.Should().ContainSingle().Subject;
        reference.Qualifier.Should().Be(LoadTenderTransactionFactory.ReferenceQualifier);
        reference.Value.Should().Be(LoadTenderTransactionFactory.ReferenceValue);
    }

    [Fact]
    public void Map_WhenSegmentIsNotNamed_ShouldKeepItAsAnExtension()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.WithUnnamedSegments();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        tender.Extensions.Select(segment => segment.Tag).Should().Equal(
            LoadTenderTransactionFactory.UnnamedTag,
            LoadTenderTransactionFactory.ExtensionTag);
        tender.Extensions[1].Elements["01"].Should().Be(LoadTenderTransactionFactory.ExtensionValue);
    }

    [Fact]
    public void Map_WhenStopHasAnOrder_ShouldKeepReferenceAndWeight()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.WithOrder();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        var order = tender.Stops.Should().ContainSingle().Subject.Orders.Should().ContainSingle().Subject;
        order.Reference.Should().Be(LoadTenderTransactionFactory.OrderReference);
        order.Weight.Should().Be(LoadTenderTransactionFactory.OrderWeight);
    }

    [Fact]
    public void Map_WhenEquipmentNumberIsPresent_ShouldExposeNumberAndDescription()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.WithEquipment();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        var equipment = tender.Equipment.Should().ContainSingle().Subject;
        equipment.Number.Should().Be(LoadTenderTransactionFactory.EquipmentNumber);
        equipment.Description.Should().Be(LoadTenderTransactionFactory.EquipmentDescription);
        equipment.Extensions.Should().BeEmpty();
    }

    [Fact]
    public void Map_WhenHeadingDateIsPresent_ShouldExposeQualifierAndDate()
    {
        // Arrange
        var transaction = LoadTenderTransactionFactory.WithHeadingDate();

        // Act
        var tender = LoadTenderMapper.Map(transaction);

        // Assert
        tender.Date.Should().NotBeNull();
        tender.Date!.Qualifier.Should().Be(LoadTenderTransactionFactory.DateQualifier);
        tender.Date.Date.Should().Be(LoadTenderTransactionFactory.TenderDate);
    }

    private static void AssertStop(TenderStop stop, string sequence, string reason, string role, string name)
    {
        stop.Sequence.Should().Be(sequence);
        stop.Reason.Should().Be(reason);
        stop.Party.Should().NotBeNull();
        stop.Party!.Role.Should().Be(role);
        stop.Party.Name.Should().Be(name);
    }
}
