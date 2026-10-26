namespace EdiMorph.X12.Tests.Factories;

internal static class X12InterchangeFactory
{
    public const string Format = "X12";

    public const string Version = "004010";

    public const string TransactionCode = "204";

    public const string FunctionalGroup = "SM";

    public const string InterchangeControlNumber = "000000001";

    public const string DifferentControlNumber = "000000002";

    public const string GroupControlNumber = "1";

    public const string TransactionControlNumber = "0001";

    public const string CarrierCode = "SCAC";

    public const string LongCarrierCode = "SCACX";

    public const string PaymentMethod = "PP";

    public const string PurposeCode = "00";

    public const string FirstStop = "1";

    public const string SecondStop = "2";

    public const string Pickup = "LD";

    public const string ShipFromCode = "SH";

    public const string ShipToCode = "CN";

    public const string ShipFromName = "Shipper";

    public const string ShipToName = "Consignee";

    public const string ExtensionTag = "ZZ";

    public const string ExtensionValue = "EXTRA";

    public const string ReferenceQualifier = "VD";

    public const string ReferenceValue = "T5174653";

    public const string StopWeight = "100";

    public const string DisagreeingShipmentWeight = "200";

    public const string WeightUnit = "L";

    public const string SenderId = "SENDER";

    public const string ReceiverId = "RECEIVER";

    public const string NoInformation = "00";

    public const string AuthorizationData = "AUTHINFO  ";

    public const char ElementSeparator = '*';

    public const char ComponentSeparator = '>';

    public const char SegmentTerminator = '~';

    public const int HeaderLength = 106;

    public const int PartyIdWidth = 15;

    public const int InformationWidth = 10;

    public static string Valid204()
    {
        return Create(DefaultBody());
    }

    public static string Valid204WithTwoStops()
    {
        return Create([.. DefaultBody(), Stop(SecondStop)]);
    }

    public static string WithTrailerControl(string controlNumber)
    {
        return Create(DefaultBody(), interchangeTrailerControl: controlNumber);
    }

    public static string WithAuthorizationInformation(string information)
    {
        return Create(DefaultBody(), authorizationInformation: information);
    }

    public static string WithoutBeginningSegment()
    {
        return Create([Purpose(), Stop(FirstStop)]);
    }

    public static string CreateOutOfOrderStops()
    {
        return Create([Beginning(CarrierCode), Purpose(), Stop(SecondStop), Stop(FirstStop)]);
    }

    public static string WithPartyOnEachStop()
    {
        return Create(
        [
            Beginning(CarrierCode),
            Purpose(),
            Stop(FirstStop),
            Party(ShipFromCode, ShipFromName),
            Stop(SecondStop),
            Party(ShipToCode, ShipToName)
        ]);
    }

    public static string WithExtensionOnTheFirstStop()
    {
        return Create(
        [
            Beginning(CarrierCode),
            Purpose(),
            Stop(FirstStop),
            $"{ExtensionTag}*{ExtensionValue}"
        ]);
    }

    public static string WithReference()
    {
        return Create(
        [
            Beginning(CarrierCode),
            Purpose(),
            $"L11*{ReferenceValue}*{ReferenceQualifier}",
            Stop(FirstStop)
        ]);
    }

    public static string WithDisagreeingStopWeight()
    {
        return Create(
        [
            Beginning(CarrierCode),
            Purpose(),
            $"S5*{FirstStop}*{Pickup}*{StopWeight}*{WeightUnit}",
            $"AT8*G*{WeightUnit}*{DisagreeingShipmentWeight}"
        ]);
    }

    public static string WithCarrierCode(string carrierCode)
    {
        return Create([Beginning(carrierCode), Purpose(), Stop(FirstStop)]);
    }

    public static string WithSameSeparators()
    {
        return Create(DefaultBody(), componentSeparator: ElementSeparator);
    }

    public static string ShortHeader()
    {
        return "ISA";
    }

    private static IReadOnlyList<string> DefaultBody()
    {
        return [Beginning(CarrierCode), Purpose(), Stop(FirstStop)];
    }

    private static string Beginning(string carrierCode)
    {
        return $"B2**{carrierCode}****{PaymentMethod}";
    }

    private static string Purpose()
    {
        return $"B2A*{PurposeCode}";
    }

    private static string Stop(string sequence)
    {
        return $"S5*{sequence}*{Pickup}";
    }

    private static string Party(string entityCode, string name)
    {
        return $"N1*{entityCode}*{name}";
    }

    private static string Create(
        IReadOnlyList<string> body,
        string? authorizationInformation = null,
        string? interchangeTrailerControl = null,
        char componentSeparator = ComponentSeparator)
    {
        var information = authorizationInformation ?? new string(' ', InformationWidth);
        if (information.Length != InformationWidth)
        {
            throw new ArgumentException("ISA authorization information must be 10 characters.", nameof(authorizationInformation));
        }

        var header = Isa(information, componentSeparator);
        if (header.Length != HeaderLength)
        {
            throw new InvalidOperationException($"ISA header length {header.Length} is not {HeaderLength}.");
        }

        var trailerControl = interchangeTrailerControl ?? InterchangeControlNumber;
        var count = body.Count + 2;
        var segments = new List<string>
        {
            $"GS*{FunctionalGroup}*{SenderId}*{ReceiverId}*20260101*1200*{GroupControlNumber}*X*{Version}",
            $"ST*{TransactionCode}*{TransactionControlNumber}"
        };
        segments.AddRange(body);
        segments.Add($"SE*{count}*{TransactionControlNumber}");
        segments.Add($"GE*{1}*{GroupControlNumber}");
        segments.Add($"IEA*{1}*{trailerControl}");
        return header + "\n" + string.Join("\n", segments.Select(segment => segment + SegmentTerminator));
    }

    private static string Isa(string authorizationInformation, char componentSeparator)
    {
        var fields = new[]
        {
            NoInformation,
            authorizationInformation,
            NoInformation,
            new string(' ', InformationWidth),
            "ZZ",
            SenderId.PadRight(PartyIdWidth),
            "ZZ",
            ReceiverId.PadRight(PartyIdWidth),
            "260101",
            "1200",
            "U",
            "00401",
            InterchangeControlNumber,
            "0",
            "P",
            componentSeparator.ToString()
        };

        return "ISA" + ElementSeparator + string.Join(ElementSeparator, fields) + SegmentTerminator;
    }
}
