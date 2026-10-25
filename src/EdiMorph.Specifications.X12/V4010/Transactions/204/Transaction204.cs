using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.X12.V4010.Transactions;

/// <summary>
/// X12 004010 transaction 204, Motor Carrier Load Tender.
/// Segments stay in X12 element order. A segment the 204 structure does not name is an extension.
/// </summary>
public sealed class Transaction204
{
    public const string Code = "204";

    public Transaction204(
        X12Segment? setHeader,
        X12Segment? beginning,
        X12Segment? purpose,
        IReadOnlyList<X12Segment> references,
        X12Segment? date,
        X12Segment? interlineInformation,
        IReadOnlyList<X12Segment> billOfLadingHandling,
        X12Segment? palletInformation,
        IReadOnlyList<X12Segment> hazardousCertification,
        IReadOnlyList<X12Segment> notes,
        IReadOnlyList<PartyLoop> parties,
        IReadOnlyList<EquipmentLoop> equipment,
        IReadOnlyList<StopLoop> stops,
        X12Segment? total,
        X12Segment? setTrailer,
        IReadOnlyList<X12Segment> extensions)
    {
        SetHeader = setHeader;
        Beginning = beginning;
        Purpose = purpose;
        References = Copy(references);
        Date = date;
        InterlineInformation = interlineInformation;
        BillOfLadingHandling = Copy(billOfLadingHandling);
        PalletInformation = palletInformation;
        HazardousCertification = Copy(hazardousCertification);
        Notes = Copy(notes);
        Parties = Copy(parties);
        Equipment = Copy(equipment);
        Stops = Copy(stops);
        Total = total;
        SetTrailer = setTrailer;
        Extensions = Copy(extensions);
    }

    public X12Segment? SetHeader { get; }

    public X12Segment? Beginning { get; }

    public X12Segment? Purpose { get; }

    public IReadOnlyList<X12Segment> References { get; }

    public X12Segment? Date { get; }

    public X12Segment? InterlineInformation { get; }

    public IReadOnlyList<X12Segment> BillOfLadingHandling { get; }

    public X12Segment? PalletInformation { get; }

    public IReadOnlyList<X12Segment> HazardousCertification { get; }

    public IReadOnlyList<X12Segment> Notes { get; }

    public IReadOnlyList<PartyLoop> Parties { get; }

    public IReadOnlyList<EquipmentLoop> Equipment { get; }

    public IReadOnlyList<StopLoop> Stops { get; }

    public X12Segment? Total { get; }

    public X12Segment? SetTrailer { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }

    internal static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.ToArray();
    }
}

public sealed class PartyLoop
{
    public PartyLoop(
        X12Segment? name,
        X12Segment? additionalName,
        IReadOnlyList<X12Segment> addresses,
        X12Segment? geographicLocation,
        X12Segment? reference,
        IReadOnlyList<X12Segment> contacts,
        IReadOnlyList<X12Segment> extensions)
    {
        Name = name;
        AdditionalName = additionalName;
        Addresses = Transaction204.Copy(addresses);
        GeographicLocation = geographicLocation;
        Reference = reference;
        Contacts = Transaction204.Copy(contacts);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? Name { get; }

    public X12Segment? AdditionalName { get; }

    public IReadOnlyList<X12Segment> Addresses { get; }

    public X12Segment? GeographicLocation { get; }

    public X12Segment? Reference { get; }

    public IReadOnlyList<X12Segment> Contacts { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class EquipmentLoop
{
    public EquipmentLoop(
        X12Segment? equipmentDetails,
        X12Segment? accessorial,
        X12Segment? additionalEquipment,
        X12Segment? measurement,
        IReadOnlyList<X12Segment> sealNumbers,
        IReadOnlyList<X12Segment> extensions)
    {
        EquipmentDetails = equipmentDetails;
        Accessorial = accessorial;
        AdditionalEquipment = additionalEquipment;
        Measurement = measurement;
        SealNumbers = Transaction204.Copy(sealNumbers);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? EquipmentDetails { get; }

    public X12Segment? Accessorial { get; }

    public X12Segment? AdditionalEquipment { get; }

    public X12Segment? Measurement { get; }

    public IReadOnlyList<X12Segment> SealNumbers { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class StopLoop
{
    public StopLoop(
        X12Segment? stopOff,
        IReadOnlyList<X12Segment> references,
        IReadOnlyList<X12Segment> dates,
        X12Segment? shipmentWeight,
        IReadOnlyList<X12Segment> ladingDetails,
        IReadOnlyList<X12Segment> billOfLadingHandling,
        X12Segment? palletInformation,
        IReadOnlyList<X12Segment> notes,
        PartyLoop? party,
        IReadOnlyList<ShipmentDetailLoop> shipmentDetails,
        IReadOnlyList<OrderLoop> orders,
        IReadOnlyList<EquipmentLoop> equipment,
        IReadOnlyList<X12Segment> extensions)
    {
        StopOff = stopOff;
        References = Transaction204.Copy(references);
        Dates = Transaction204.Copy(dates);
        ShipmentWeight = shipmentWeight;
        LadingDetails = Transaction204.Copy(ladingDetails);
        BillOfLadingHandling = Transaction204.Copy(billOfLadingHandling);
        PalletInformation = palletInformation;
        Notes = Transaction204.Copy(notes);
        Party = party;
        ShipmentDetails = Transaction204.Copy(shipmentDetails);
        Orders = Transaction204.Copy(orders);
        Equipment = Transaction204.Copy(equipment);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? StopOff { get; }

    public IReadOnlyList<X12Segment> References { get; }

    public IReadOnlyList<X12Segment> Dates { get; }

    public X12Segment? ShipmentWeight { get; }

    public IReadOnlyList<X12Segment> LadingDetails { get; }

    public IReadOnlyList<X12Segment> BillOfLadingHandling { get; }

    public X12Segment? PalletInformation { get; }

    public IReadOnlyList<X12Segment> Notes { get; }

    public PartyLoop? Party { get; }

    public IReadOnlyList<ShipmentDetailLoop> ShipmentDetails { get; }

    public IReadOnlyList<OrderLoop> Orders { get; }

    public IReadOnlyList<EquipmentLoop> Equipment { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class ShipmentDetailLoop
{
    public ShipmentDetailLoop(
        X12Segment? description,
        X12Segment? shipmentWeight,
        IReadOnlyList<ContactLoop> contacts,
        IReadOnlyList<X12Segment> extensions)
    {
        Description = description;
        ShipmentWeight = shipmentWeight;
        Contacts = Transaction204.Copy(contacts);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? Description { get; }

    public X12Segment? ShipmentWeight { get; }

    public IReadOnlyList<ContactLoop> Contacts { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class ContactLoop
{
    public ContactLoop(
        X12Segment? contact,
        IReadOnlyList<X12Segment> references,
        IReadOnlyList<X12Segment> hazardousCertification,
        IReadOnlyList<HazardousMaterialLoop> hazardousMaterials,
        IReadOnlyList<X12Segment> extensions)
    {
        Contact = contact;
        References = Transaction204.Copy(references);
        HazardousCertification = Transaction204.Copy(hazardousCertification);
        HazardousMaterials = Transaction204.Copy(hazardousMaterials);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? Contact { get; }

    public IReadOnlyList<X12Segment> References { get; }

    public IReadOnlyList<X12Segment> HazardousCertification { get; }

    public IReadOnlyList<HazardousMaterialLoop> HazardousMaterials { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class HazardousMaterialLoop
{
    public HazardousMaterialLoop(
        X12Segment? identification,
        IReadOnlyList<X12Segment> classifications,
        IReadOnlyList<X12Segment> information,
        IReadOnlyList<X12Segment> freeForm,
        IReadOnlyList<X12Segment> environmentalProtection,
        X12Segment? canadianRequirements,
        IReadOnlyList<X12Segment> transborder,
        IReadOnlyList<X12Segment> extensions)
    {
        Identification = identification;
        Classifications = Transaction204.Copy(classifications);
        Information = Transaction204.Copy(information);
        FreeForm = Transaction204.Copy(freeForm);
        EnvironmentalProtection = Transaction204.Copy(environmentalProtection);
        CanadianRequirements = canadianRequirements;
        Transborder = Transaction204.Copy(transborder);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? Identification { get; }

    public IReadOnlyList<X12Segment> Classifications { get; }

    public IReadOnlyList<X12Segment> Information { get; }

    public IReadOnlyList<X12Segment> FreeForm { get; }

    public IReadOnlyList<X12Segment> EnvironmentalProtection { get; }

    public X12Segment? CanadianRequirements { get; }

    public IReadOnlyList<X12Segment> Transborder { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class OrderLoop
{
    public OrderLoop(
        X12Segment? orderIdentification,
        IReadOnlyList<X12Segment> dates,
        IReadOnlyList<X12Segment> ladingDetails,
        IReadOnlyList<ShipmentDetailLoop> shipmentDetails,
        IReadOnlyList<X12Segment> extensions)
    {
        OrderIdentification = orderIdentification;
        Dates = Transaction204.Copy(dates);
        LadingDetails = Transaction204.Copy(ladingDetails);
        ShipmentDetails = Transaction204.Copy(shipmentDetails);
        Extensions = Transaction204.Copy(extensions);
    }

    public X12Segment? OrderIdentification { get; }

    public IReadOnlyList<X12Segment> Dates { get; }

    public IReadOnlyList<X12Segment> LadingDetails { get; }

    public IReadOnlyList<ShipmentDetailLoop> ShipmentDetails { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}
