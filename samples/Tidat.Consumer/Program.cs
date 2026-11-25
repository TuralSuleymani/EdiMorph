using Tidat.Consumer;

var inputPath = args.Length == 0
    ? Path.Combine(AppContext.BaseDirectory, "fixtures", "tender.edi")
    : args[0];
var reader = new Tidat204Reader(Path.Combine(AppContext.BaseDirectory, "profiles"));
var result = await reader.ReadAsync(await File.ReadAllTextAsync(inputPath));
foreach (var diagnostic in result.Diagnostics)
{
    Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Code} {diagnostic.Target}: {diagnostic.Message}");
}

// The library also returns partial models on failure. Do not process them as accepted loads.
if (!result.IsSuccess)
{
    return 1;
}

foreach (var tender in result.LoadTenders)
{
    Console.WriteLine($"Tidat 204: {tender.ShipmentId}, purpose {tender.Purpose}, carrier {tender.Carrier}");
    foreach (var stop in tender.Stops)
    {
        Console.WriteLine($"  Stop {stop.Sequence}: {stop.Reason}, {stop.Party?.Role}, {stop.Party?.Name}");
    }
}

return 0;
