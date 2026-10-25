using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010.Transactions;

namespace EdiMorph.X12;

internal static class Transaction204Mapper
{
    public static Transaction204 Map(IReadOnlyList<Placement> placements)
    {
        X12Segment? setHeader = null;
        X12Segment? beginning = null;
        X12Segment? purpose = null;
        var references = new List<X12Segment>();
        X12Segment? date = null;
        X12Segment? interline = null;
        var handling = new List<X12Segment>();
        X12Segment? pallet = null;
        var certification = new List<X12Segment>();
        var notes = new List<X12Segment>();
        var parties = new List<PartyLoop>();
        var equipment = new List<EquipmentLoop>();
        var stops = new List<StopLoop>();
        X12Segment? total = null;
        X12Segment? setTrailer = null;
        var extensions = new List<X12Segment>();

        foreach (var placement in placements)
        {
            switch (placement)
            {
                case SegmentPlacement segment when segment.NodeId == "heading.ST":
                    Assign(segment, ref setHeader, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.B2":
                    Assign(segment, ref beginning, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.B2A":
                    Assign(segment, ref purpose, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.L11":
                    references.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.G62":
                    Assign(segment, ref date, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.MS3":
                    Assign(segment, ref interline, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.AT5":
                    handling.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.PLD":
                    Assign(segment, ref pallet, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.LH6":
                    certification.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == "heading.NTE":
                    notes.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == "summary.L3":
                    Assign(segment, ref total, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == "summary.SE":
                    Assign(segment, ref setTrailer, extensions);
                    break;
                case LoopPlacement loop when loop.LoopId == "0100":
                    parties.Add(MapParty(loop));
                    break;
                case LoopPlacement loop when loop.LoopId == "0200":
                    equipment.Add(MapEquipment(loop));
                    break;
                case LoopPlacement loop when loop.LoopId == "0300":
                    stops.Add(MapStop(loop));
                    break;
                default:
                    Flatten(placement, extensions);
                    break;
            }
        }

        return new Transaction204(
            setHeader,
            beginning,
            purpose,
            references,
            date,
            interline,
            handling,
            pallet,
            certification,
            notes,
            parties,
            equipment,
            stops,
            total,
            setTrailer,
            extensions);
    }

    private static PartyLoop MapParty(LoopPlacement loop)
    {
        X12Segment? name = null;
        X12Segment? additionalName = null;
        var addresses = new List<X12Segment>();
        X12Segment? location = null;
        X12Segment? reference = null;
        var contacts = new List<X12Segment>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            if (child is not SegmentPlacement segment || !segment.NodeId.StartsWith(loop.NodeId + ".", StringComparison.Ordinal))
            {
                Flatten(child, extensions);
                continue;
            }

            switch (segment.NodeId[(loop.NodeId.Length + 1)..])
            {
                case "N1":
                    Assign(segment, ref name, extensions);
                    break;
                case "N2":
                    Assign(segment, ref additionalName, extensions);
                    break;
                case "N3":
                    addresses.Add(segment.Segment.Segment);
                    break;
                case "N4":
                    Assign(segment, ref location, extensions);
                    break;
                case "L11":
                    Assign(segment, ref reference, extensions);
                    break;
                case "G61":
                    contacts.Add(segment.Segment.Segment);
                    break;
                default:
                    extensions.Add(segment.Segment.Segment);
                    break;
            }
        }

        return new PartyLoop(name, additionalName, addresses, location, reference, contacts, extensions);
    }

    private static EquipmentLoop MapEquipment(LoopPlacement loop)
    {
        X12Segment? details = null;
        X12Segment? accessorial = null;
        X12Segment? additional = null;
        X12Segment? measurement = null;
        var seals = new List<X12Segment>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            if (child is not SegmentPlacement segment || !segment.NodeId.StartsWith(loop.NodeId + ".", StringComparison.Ordinal))
            {
                Flatten(child, extensions);
                continue;
            }

            switch (segment.NodeId[(loop.NodeId.Length + 1)..])
            {
                case "N7":
                    Assign(segment, ref details, extensions);
                    break;
                case "N7A":
                    Assign(segment, ref accessorial, extensions);
                    break;
                case "N7B":
                    Assign(segment, ref additional, extensions);
                    break;
                case "MEA":
                    Assign(segment, ref measurement, extensions);
                    break;
                case "M7":
                    seals.Add(segment.Segment.Segment);
                    break;
                default:
                    extensions.Add(segment.Segment.Segment);
                    break;
            }
        }

        return new EquipmentLoop(details, accessorial, additional, measurement, seals, extensions);
    }

    private static StopLoop MapStop(LoopPlacement loop)
    {
        X12Segment? stopOff = null;
        var references = new List<X12Segment>();
        var dates = new List<X12Segment>();
        X12Segment? weight = null;
        var lading = new List<X12Segment>();
        var handling = new List<X12Segment>();
        X12Segment? pallet = null;
        var notes = new List<X12Segment>();
        PartyLoop? party = null;
        var details = new List<ShipmentDetailLoop>();
        var orders = new List<OrderLoop>();
        var equipment = new List<EquipmentLoop>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            switch (child)
            {
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".S5":
                    Assign(segment, ref stopOff, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".L11":
                    references.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".G62":
                    dates.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".AT8":
                    Assign(segment, ref weight, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".LAD":
                    lading.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".AT5":
                    handling.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".PLD":
                    Assign(segment, ref pallet, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".NTE":
                    notes.Add(segment.Segment.Segment);
                    break;
                case LoopPlacement nested when nested.LoopId == "0310":
                    if (party is null)
                    {
                        party = MapParty(nested);
                    }
                    else
                    {
                        Flatten(nested, extensions);
                    }

                    break;
                case LoopPlacement nested when nested.LoopId == "0320":
                    details.Add(MapShipmentDetail(nested));
                    break;
                case LoopPlacement nested when nested.LoopId == "0350":
                    orders.Add(MapOrder(nested));
                    break;
                case LoopPlacement nested when nested.LoopId == "0380":
                    equipment.Add(MapEquipment(nested));
                    break;
                default:
                    Flatten(child, extensions);
                    break;
            }
        }

        return new StopLoop(
            stopOff,
            references,
            dates,
            weight,
            lading,
            handling,
            pallet,
            notes,
            party,
            details,
            orders,
            equipment,
            extensions);
    }

    private static ShipmentDetailLoop MapShipmentDetail(LoopPlacement loop)
    {
        X12Segment? description = null;
        X12Segment? weight = null;
        var contacts = new List<ContactLoop>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            switch (child)
            {
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".L5":
                    Assign(segment, ref description, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".AT8":
                    Assign(segment, ref weight, extensions);
                    break;
                case LoopPlacement nested when nested.LoopId is "0325" or "0365":
                    contacts.Add(MapContact(nested));
                    break;
                default:
                    Flatten(child, extensions);
                    break;
            }
        }

        return new ShipmentDetailLoop(description, weight, contacts, extensions);
    }

    private static ContactLoop MapContact(LoopPlacement loop)
    {
        X12Segment? contact = null;
        var references = new List<X12Segment>();
        var certification = new List<X12Segment>();
        var hazardous = new List<HazardousMaterialLoop>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            switch (child)
            {
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".G61":
                    Assign(segment, ref contact, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".L11":
                    references.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".LH6":
                    certification.Add(segment.Segment.Segment);
                    break;
                case LoopPlacement nested when nested.LoopId is "0330" or "0370":
                    hazardous.Add(MapHazardous(nested));
                    break;
                default:
                    Flatten(child, extensions);
                    break;
            }
        }

        return new ContactLoop(contact, references, certification, hazardous, extensions);
    }

    private static HazardousMaterialLoop MapHazardous(LoopPlacement loop)
    {
        X12Segment? identification = null;
        var classifications = new List<X12Segment>();
        var information = new List<X12Segment>();
        var freeForm = new List<X12Segment>();
        var environmental = new List<X12Segment>();
        X12Segment? canadian = null;
        var transborder = new List<X12Segment>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            if (child is not SegmentPlacement segment)
            {
                Flatten(child, extensions);
                continue;
            }

            switch (segment.NodeId)
            {
                case var id when id == loop.NodeId + ".LH1":
                    Assign(segment, ref identification, extensions);
                    break;
                case var id when id == loop.NodeId + ".LH2":
                    classifications.Add(segment.Segment.Segment);
                    break;
                case var id when id == loop.NodeId + ".LH3":
                    information.Add(segment.Segment.Segment);
                    break;
                case var id when id == loop.NodeId + ".LFH":
                    freeForm.Add(segment.Segment.Segment);
                    break;
                case var id when id == loop.NodeId + ".LEP":
                    environmental.Add(segment.Segment.Segment);
                    break;
                case var id when id == loop.NodeId + ".LH4":
                    Assign(segment, ref canadian, extensions);
                    break;
                case var id when id == loop.NodeId + ".LHT":
                    transborder.Add(segment.Segment.Segment);
                    break;
                default:
                    extensions.Add(segment.Segment.Segment);
                    break;
            }
        }

        return new HazardousMaterialLoop(
            identification,
            classifications,
            information,
            freeForm,
            environmental,
            canadian,
            transborder,
            extensions);
    }

    private static OrderLoop MapOrder(LoopPlacement loop)
    {
        X12Segment? identification = null;
        var dates = new List<X12Segment>();
        var lading = new List<X12Segment>();
        var details = new List<ShipmentDetailLoop>();
        var extensions = new List<X12Segment>();
        foreach (var child in loop.Children)
        {
            switch (child)
            {
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".OID":
                    Assign(segment, ref identification, extensions);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".G62":
                    dates.Add(segment.Segment.Segment);
                    break;
                case SegmentPlacement segment when segment.NodeId == loop.NodeId + ".LAD":
                    lading.Add(segment.Segment.Segment);
                    break;
                case LoopPlacement nested when nested.LoopId == "0360":
                    details.Add(MapShipmentDetail(nested));
                    break;
                default:
                    Flatten(child, extensions);
                    break;
            }
        }

        return new OrderLoop(identification, dates, lading, details, extensions);
    }

    private static void Assign(SegmentPlacement placement, ref X12Segment? slot, List<X12Segment> extensions)
    {
        if (slot is null)
        {
            slot = placement.Segment.Segment;
            return;
        }

        extensions.Add(placement.Segment.Segment);
    }

    private static void Flatten(Placement placement, List<X12Segment> target)
    {
        switch (placement)
        {
            case SegmentPlacement segment:
                target.Add(segment.Segment.Segment);
                break;
            case LoopPlacement loop:
                foreach (var child in loop.Children)
                {
                    Flatten(child, target);
                }

                break;
        }
    }
}
