using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.X12.Tests.Factories;

internal static class LoadTenderTransactionFactory
{
    public const string Carrier = "SCAC";

    public const string ShipmentId = "SHIP1";

    public const string PaymentMethod = "PP";

    public const string Purpose = "00";

    public const string DateQualifier = "64";

    public const string TenderDate = "20261020";

    public const string ReferenceQualifier = "VD";

    public const string ReferenceValue = "T5174653";

    public const string FirstStop = "1";

    public const string SecondStop = "2";

    public const string Pickup = "LD";

    public const string Unload = "UL";

    public const string ShipFrom = "SF";

    public const string ShipTo = "ST";

    public const string ShipFromName = "Shipper";

    public const string ShipToName = "Consignee";

    public const string AddressLine = "550 Capitol Way";

    public const string City = "Jacksonville";

    public const string State = "IL";

    public const string PostalCode = "62650";

    public const string Country = "US";

    public const string EquipmentNumber = "5300";

    public const string EquipmentDescription = "TV";

    public const string OrderReference = "UR32P";

    public const string OrderWeight = "6709";

    public const string ExtensionTag = "ZZ";

    public const string ExtensionValue = "EXTRA";

    public const string UnnamedTag = "MS3";

    public static Transaction204 CarrierAndStops()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            stops:
            [
                Stop(FirstStop, Pickup, ShipFrom, ShipFromName, withAddress: true),
                Stop(SecondStop, Unload, ShipTo, ShipToName, withAddress: false)
            ]);
    }

    public static Transaction204 WithReference()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            references: [X12Segment.With("L11", ("01", ReferenceValue), ("02", ReferenceQualifier))]);
    }

    public static Transaction204 WithUnnamedSegments()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            interline: X12Segment.With(UnnamedTag, ("01", Carrier)),
            extensions: [X12Segment.With(ExtensionTag, ("01", ExtensionValue))]);
    }

    public static Transaction204 WithOrder()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            stops:
            [
                Stop(
                    FirstStop,
                    Pickup,
                    ShipFrom,
                    ShipFromName,
                    withAddress: false,
                    orders:
                    [
                        new OrderLoop(
                            X12Segment.With("OID", ("02", OrderReference), ("07", OrderWeight)),
                            [],
                            [],
                            [],
                            [])
                    ])
            ]);
    }

    public static Transaction204 WithEquipment()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            equipment:
            [
                new EquipmentLoop(
                    X12Segment.With("N7", ("02", EquipmentNumber), ("11", EquipmentDescription)),
                    null,
                    null,
                    null,
                    [],
                    [])
            ]);
    }

    public static Transaction204 WithHeadingDate()
    {
        return Transaction(
            beginning: Beginning(),
            purpose: PurposeSegment(),
            date: X12Segment.With("G62", ("01", DateQualifier), ("02", TenderDate)));
    }

    private static Transaction204 Transaction(
        X12Segment? beginning = null,
        X12Segment? purpose = null,
        X12Segment? date = null,
        IReadOnlyList<X12Segment>? references = null,
        X12Segment? interline = null,
        IReadOnlyList<PartyLoop>? parties = null,
        IReadOnlyList<EquipmentLoop>? equipment = null,
        IReadOnlyList<StopLoop>? stops = null,
        IReadOnlyList<X12Segment>? extensions = null)
    {
        return new Transaction204(
            null,
            beginning,
            purpose,
            references ?? [],
            date,
            interline,
            [],
            null,
            [],
            [],
            parties ?? [],
            equipment ?? [],
            stops ?? [],
            null,
            null,
            extensions ?? []);
    }

    private static X12Segment Beginning()
    {
        return X12Segment.With("B2", ("02", Carrier), ("04", ShipmentId), ("06", PaymentMethod));
    }

    private static X12Segment PurposeSegment()
    {
        return X12Segment.With("B2A", ("01", Purpose));
    }

    private static StopLoop Stop(
        string sequence,
        string reason,
        string role,
        string name,
        bool withAddress,
        IReadOnlyList<OrderLoop>? orders = null)
    {
        var addresses = withAddress
            ? new X12Segment[] { X12Segment.With("N3", ("01", AddressLine)) }
            : [];
        var location = withAddress
            ? X12Segment.With("N4", ("01", City), ("02", State), ("03", PostalCode), ("04", Country))
            : null;
        return new StopLoop(
            X12Segment.With("S5", ("01", sequence), ("02", reason)),
            [],
            [],
            null,
            [],
            [],
            null,
            [],
            new PartyLoop(
                X12Segment.With("N1", ("01", role), ("02", name)),
                null,
                addresses,
                location,
                null,
                [],
                []),
            [],
            orders ?? [],
            [],
            []);
    }
}
