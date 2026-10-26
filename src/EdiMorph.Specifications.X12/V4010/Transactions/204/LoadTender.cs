using EdiMorph.Specifications.X12;

namespace EdiMorph.Specifications.X12.V4010.Transactions;

/// <summary>
/// Version-scoped 004010 load tender. Element positions stay in the mapper.
/// A segment this type does not name remains an <see cref="X12Segment"/> extension.
/// </summary>
public sealed class LoadTender
{
    public const string Version = "004010";

    public const string Code = "204";

    public LoadTender(
        string? transactionControlNumber,
        string? carrier,
        string? shipmentId,
        string? paymentMethod,
        string? purpose,
        TenderDate? date,
        IReadOnlyList<TenderReference> references,
        IReadOnlyList<TenderNote> notes,
        IReadOnlyList<TenderParty> parties,
        IReadOnlyList<TenderEquipment> equipment,
        IReadOnlyList<TenderStop> stops,
        IReadOnlyList<X12Segment> extensions)
    {
        TransactionControlNumber = transactionControlNumber;
        Carrier = carrier;
        ShipmentId = shipmentId;
        PaymentMethod = paymentMethod;
        Purpose = purpose;
        Date = date;
        References = Copy(references);
        Notes = Copy(notes);
        Parties = Copy(parties);
        Equipment = Copy(equipment);
        Stops = Copy(stops);
        Extensions = Copy(extensions);
    }

    public string? TransactionControlNumber { get; }

    public string? Carrier { get; }

    public string? ShipmentId { get; }

    public string? PaymentMethod { get; }

    public string? Purpose { get; }

    public TenderDate? Date { get; }

    public IReadOnlyList<TenderReference> References { get; }

    public IReadOnlyList<TenderNote> Notes { get; }

    public IReadOnlyList<TenderParty> Parties { get; }

    public IReadOnlyList<TenderEquipment> Equipment { get; }

    public IReadOnlyList<TenderStop> Stops { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }

    internal static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.ToArray();
    }
}

public sealed class TenderDate
{
    public TenderDate(string? qualifier, string? date, string? timeQualifier, string? time, string? timeCode)
    {
        Qualifier = qualifier;
        Date = date;
        TimeQualifier = timeQualifier;
        Time = time;
        TimeCode = timeCode;
    }

    public string? Qualifier { get; }

    public string? Date { get; }

    public string? TimeQualifier { get; }

    public string? Time { get; }

    public string? TimeCode { get; }
}

public sealed class TenderReference
{
    public TenderReference(string? qualifier, string? value)
    {
        Qualifier = qualifier;
        Value = value;
    }

    public string? Qualifier { get; }

    public string? Value { get; }
}

public sealed class TenderNote
{
    public TenderNote(string? qualifier, string? text)
    {
        Qualifier = qualifier;
        Text = text;
    }

    public string? Qualifier { get; }

    public string? Text { get; }
}

public sealed class TenderParty
{
    public TenderParty(
        string? role,
        string? name,
        string? identificationQualifier,
        string? identification,
        IReadOnlyList<string> addressLines,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        IReadOnlyList<X12Segment> extensions)
    {
        Role = role;
        Name = name;
        IdentificationQualifier = identificationQualifier;
        Identification = identification;
        AddressLines = LoadTender.Copy(addressLines);
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        Extensions = LoadTender.Copy(extensions);
    }

    public string? Role { get; }

    public string? Name { get; }

    public string? IdentificationQualifier { get; }

    public string? Identification { get; }

    public IReadOnlyList<string> AddressLines { get; }

    public string? City { get; }

    public string? State { get; }

    public string? PostalCode { get; }

    public string? Country { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class TenderEquipment
{
    public TenderEquipment(string? number, string? description, IReadOnlyList<X12Segment> extensions)
    {
        Number = number;
        Description = description;
        Extensions = LoadTender.Copy(extensions);
    }

    public string? Number { get; }

    public string? Description { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class TenderStop
{
    public TenderStop(
        string? sequence,
        string? reason,
        string? weight,
        string? weightUnit,
        IReadOnlyList<TenderDate> dates,
        IReadOnlyList<TenderReference> references,
        IReadOnlyList<TenderNote> notes,
        TenderParty? party,
        IReadOnlyList<TenderOrder> orders,
        IReadOnlyList<X12Segment> extensions)
    {
        Sequence = sequence;
        Reason = reason;
        Weight = weight;
        WeightUnit = weightUnit;
        Dates = LoadTender.Copy(dates);
        References = LoadTender.Copy(references);
        Notes = LoadTender.Copy(notes);
        Party = party;
        Orders = LoadTender.Copy(orders);
        Extensions = LoadTender.Copy(extensions);
    }

    public string? Sequence { get; }

    public string? Reason { get; }

    public string? Weight { get; }

    public string? WeightUnit { get; }

    public IReadOnlyList<TenderDate> Dates { get; }

    public IReadOnlyList<TenderReference> References { get; }

    public IReadOnlyList<TenderNote> Notes { get; }

    public TenderParty? Party { get; }

    public IReadOnlyList<TenderOrder> Orders { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}

public sealed class TenderOrder
{
    public TenderOrder(string? reference, string? weight, IReadOnlyList<X12Segment> extensions)
    {
        Reference = reference;
        Weight = weight;
        Extensions = LoadTender.Copy(extensions);
    }

    public string? Reference { get; }

    public string? Weight { get; }

    public IReadOnlyList<X12Segment> Extensions { get; }
}
