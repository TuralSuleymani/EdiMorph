using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.X12;

/// <summary>
/// Projects a parsed 004010 204 into the version-scoped load tender.
/// Validation stays on the transaction. This map does not drop a failed 204.
/// </summary>
public static class LoadTenderMapper
{
    public static LoadTender Map(Transaction204 transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return new LoadTender(
            Value(transaction.SetHeader, "02"),
            Value(transaction.Beginning, "02"),
            Value(transaction.Beginning, "04"),
            Value(transaction.Beginning, "06"),
            Value(transaction.Purpose, "01"),
            Date(transaction.Date),
            References(transaction.References),
            Notes(transaction.Notes),
            transaction.Parties.Select(Party).ToArray(),
            transaction.Equipment.Select(Equipment).ToArray(),
            transaction.Stops.Select(Stop).ToArray(),
            HeadingExtensions(transaction));
    }

    private static TenderStop Stop(StopLoop stop)
    {
        return new TenderStop(
            Value(stop.StopOff, "01"),
            Value(stop.StopOff, "02"),
            Value(stop.StopOff, "03"),
            Value(stop.StopOff, "04"),
            stop.Dates.Select(Date).Where(date => date is not null).Cast<TenderDate>().ToArray(),
            References(stop.References),
            Notes(stop.Notes),
            stop.Party is null ? null : Party(stop.Party),
            stop.Orders.Select(Order).ToArray(),
            StopExtensions(stop));
    }

    private static TenderParty Party(PartyLoop party)
    {
        var location = party.GeographicLocation;
        return new TenderParty(
            Value(party.Name, "01"),
            Value(party.Name, "02"),
            Value(party.Name, "03"),
            Value(party.Name, "04"),
            AddressLines(party.Addresses),
            Value(location, "01"),
            Value(location, "02"),
            Value(location, "03"),
            Value(location, "04"),
            PartyExtensions(party));
    }

    private static TenderEquipment Equipment(EquipmentLoop equipment)
    {
        var details = equipment.EquipmentDetails;
        var extensions = new List<X12Segment>();
        if (details is not null && HasUnnamedEquipmentElement(details))
        {
            extensions.Add(details);
        }

        Add(extensions, equipment.Accessorial);
        Add(extensions, equipment.AdditionalEquipment);
        Add(extensions, equipment.Measurement);
        extensions.AddRange(equipment.SealNumbers);
        extensions.AddRange(equipment.Extensions);
        return new TenderEquipment(Value(details, "02"), Value(details, "11"), extensions);
    }

    private static TenderOrder Order(OrderLoop order)
    {
        var identification = order.OrderIdentification;
        var extensions = new List<X12Segment>();
        if (identification is not null && HasUnnamedOrderElement(identification))
        {
            extensions.Add(identification);
        }

        extensions.AddRange(order.Dates);
        extensions.AddRange(order.LadingDetails);
        foreach (var detail in order.ShipmentDetails)
        {
            AddShipment(extensions, detail);
        }

        extensions.AddRange(order.Extensions);
        return new TenderOrder(
            Value(identification, "02") ?? Value(identification, "01"),
            Value(identification, "07"),
            extensions);
    }

    private static IReadOnlyList<TenderReference> References(IReadOnlyList<X12Segment> segments)
    {
        return segments
            .Select(segment => new TenderReference(Value(segment, "02"), Value(segment, "01")))
            .Where(reference => reference.Qualifier is not null || reference.Value is not null)
            .ToArray();
    }

    private static IReadOnlyList<TenderNote> Notes(IReadOnlyList<X12Segment> segments)
    {
        return segments
            .Select(segment => new TenderNote(Value(segment, "01"), Value(segment, "02")))
            .Where(note => note.Qualifier is not null || note.Text is not null)
            .ToArray();
    }

    private static TenderDate? Date(X12Segment? segment)
    {
        if (segment is null)
        {
            return null;
        }

        var date = new TenderDate(
            Value(segment, "01"),
            Value(segment, "02"),
            Value(segment, "03"),
            Value(segment, "04"),
            Value(segment, "05"));
        return date.Qualifier is null && date.Date is null && date.TimeQualifier is null && date.Time is null && date.TimeCode is null
            ? null
            : date;
    }

    private static IReadOnlyList<string> AddressLines(IReadOnlyList<X12Segment> addresses)
    {
        var lines = new List<string>();
        foreach (var address in addresses)
        {
            AddLine(lines, Value(address, "01"));
            AddLine(lines, Value(address, "02"));
        }

        return lines;
    }

    private static X12Segment[] HeadingExtensions(Transaction204 transaction)
    {
        var extensions = new List<X12Segment>();
        Add(extensions, transaction.InterlineInformation);
        extensions.AddRange(transaction.BillOfLadingHandling);
        Add(extensions, transaction.PalletInformation);
        extensions.AddRange(transaction.HazardousCertification);
        Add(extensions, transaction.Total);
        Add(extensions, transaction.SetTrailer);
        extensions.AddRange(transaction.Extensions);
        return extensions.ToArray();
    }

    private static X12Segment[] PartyExtensions(PartyLoop party)
    {
        var extensions = new List<X12Segment>();
        Add(extensions, party.AdditionalName);
        Add(extensions, party.Reference);
        extensions.AddRange(party.Contacts);
        extensions.AddRange(party.Extensions);
        return extensions.ToArray();
    }

    private static X12Segment[] StopExtensions(StopLoop stop)
    {
        var extensions = new List<X12Segment>();
        extensions.AddRange(stop.LadingDetails);
        extensions.AddRange(stop.BillOfLadingHandling);
        Add(extensions, stop.PalletInformation);
        foreach (var detail in stop.ShipmentDetails)
        {
            AddShipment(extensions, detail);
        }

        foreach (var equipment in stop.Equipment)
        {
            Add(extensions, equipment.EquipmentDetails);
            Add(extensions, equipment.Accessorial);
            Add(extensions, equipment.AdditionalEquipment);
            Add(extensions, equipment.Measurement);
            extensions.AddRange(equipment.SealNumbers);
            extensions.AddRange(equipment.Extensions);
        }

        extensions.AddRange(stop.Extensions);
        return extensions.ToArray();
    }

    private static void AddShipment(List<X12Segment> extensions, ShipmentDetailLoop detail)
    {
        Add(extensions, detail.Description);
        Add(extensions, detail.ShipmentWeight);
        foreach (var contact in detail.Contacts)
        {
            Add(extensions, contact.Contact);
            extensions.AddRange(contact.References);
            extensions.AddRange(contact.HazardousCertification);
            foreach (var material in contact.HazardousMaterials)
            {
                Add(extensions, material.Identification);
                extensions.AddRange(material.Classifications);
                extensions.AddRange(material.Information);
                extensions.AddRange(material.FreeForm);
                extensions.AddRange(material.EnvironmentalProtection);
                Add(extensions, material.CanadianRequirements);
                extensions.AddRange(material.Transborder);
                extensions.AddRange(material.Extensions);
            }

            extensions.AddRange(contact.Extensions);
        }

        extensions.AddRange(detail.Extensions);
    }

    private static bool HasUnnamedEquipmentElement(X12Segment segment)
    {
        return segment.Elements.Keys.Any(key => key is not "02" and not "11");
    }

    private static bool HasUnnamedOrderElement(X12Segment segment)
    {
        return segment.Elements.Keys.Any(key => key is not "01" and not "02" and not "07");
    }

    private static void Add(List<X12Segment> segments, X12Segment? segment)
    {
        if (segment is not null)
        {
            segments.Add(segment);
        }
    }

    private static void AddLine(List<string> lines, string? line)
    {
        if (line is not null)
        {
            lines.Add(line);
        }
    }

    private static string? Value(X12Segment? segment, string position)
    {
        if (segment is null || !segment.Elements.TryGetValue(position, out var value) || string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value;
    }
}
