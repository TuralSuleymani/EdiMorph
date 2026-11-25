# Tidat package consumer example

This application keeps the existing **Tidat** name and implements a consumer-side adaptation based on the supplied **Ditat Outbound 204 v4010.docx**, whose change log identifies V1.01, 2021-11-16. It receives the sender's outbound 204 as inbound text and returns the package's 004010 `LoadTender`.

Only this non-packable sample contains the adaptation. All four shipping libraries remain provider-independent. This project references `EdiMorph.X12` through **PackageReference**, not through source project references. Copy the profile and validator into your private application and register them there.

## Run through CI-created packages

The repository CI builds four packages, restores this consumer against them, runs `tests/Tidat.Consumer.Tests`, and executes the sample. See [release setup](../../docs/releasing.md).

For local verification of the same pipeline:

```powershell
./eng/Build-Packages.ps1 -Version 0.3.0-preview.2
```

After that build, run another interchange using those restored packages:

```powershell
dotnet run --project samples/Tidat.Consumer -c Release --no-build --no-restore -- C:/path/to/tender.edi
```

The checked-in fixture is synthetic. It is not a copied customer interchange or a claim that a live endpoint accepted it. The original guides and customer files outside this repository are not redistributed or embedded in the NuGet packages.

## Adaptation code

```csharp
var reader = new Tidat204Reader(profileDirectory);
var result = await reader.ReadAsync(interchange, cancellationToken);
if (!result.IsSuccess)
{
    // Reject or quarantine. Partial LoadTender values may still be returned.
    return;
}

foreach (var tender in result.LoadTenders)
{
    // Map the version-scoped package model into your application model.
}
```

`Tidat204Reader` registers standard handlers plus `Tidat204Guide`, then combines embedded standard definitions and the application's profile directory. The selected identity is `X12 / 004010 / 204 / Tidat / 1.01`.

## Documented behavior exercised

| Guide location | Adaptation |
| --- | --- |
| B2A01 | Allow original `00`, cancellation `01`, update `04`, and reissue `15`. |
| S502 and V1.01 change log | Allow `LD`, `UL`, `PA`, `DT`; stop-loop AT5 is prohibited. |
| N101 heading vs. stop loop | Heading permits `BT`/`SH`; pickup/drop-hook pickup uses `SF`, delivery/drop trailer uses `ST` when a stop party is present. The optional stop party loop is not made mandatory. |
| N103 stop loop | If supplied, use `ZZ`; standard paired-presence validation remains active. |
| L1102 | Heading `ZZ`/`CC`/`06`; stop `AO`/`P8`/`DO`. |
| G62 | Heading date/time qualifiers `64`/`1` with `CT`; stop date qualifiers `98`, `96`, `69`, `99`, `97`, `70`, `38`, `54`, time qualifiers `EP`, `I`, `ED`, `G`, `LP`, `LD`, with `LT`. |
| NTE01 | Restrict to `OTH`. |
| G61 | Contact function `IC`; communication qualifier `TE` or `EM`. |
| Heading party loop | N3 and N4 required when the loop occurs, following the overview's Must Use entries. |
| Stop G61 | Four occurrences allowed using a cited dialect departure from the packaged limit of three. |
| L507 / AT8 / L3 | Check the listed qualifier values when present. |

Element constraints remain JSON metadata. Contextual restrictions are compiled C# in `Tidat204Guide.cs`. No relational JSON expression language is introduced.

## Explicit departures and limits

The profile is **partner-dialect**, not standard-subset. The supplied guide omits B206 from its element table and sample, while the packaged standard requires it. `B2.override.rule` makes it optional and cites the exact parent. The guide allows four stop contacts while the packaged definition permits three; `loosenOccurrence` likewise requires a dialect profile and parent citation. These changes do not mutate the standard specification.

The guide is internally inconsistent: its ISA05 table lists `01`, while its sample uses `02`; ISA14 lists `1`, while the sample uses `0`; and the sample expresses charges with decimal points despite declaring N2 fields. This sample does not invent global restrictions from those contradictions or normalize the document into a claim of certified conformance. The fixture uses valid lengths, calculated segment counts, and no ambiguous charge values.

This is tested **204 reader adaptation**, not complete guide certification or a live integration. It does not implement 214/990/210/997, outbound serialization, transport, automatic acceptance, retries, acknowledgment scheduling, or private domain workflows. The broader inbound/outbound library roadmap is unchanged. The actual guide revision, unused elements/segments, equipment codes, and endpoint agreement require validation before production.

Ditat's public [EDI overview](https://help.ditat.com/dtm/edi-details) and [partner configuration](https://help.ditat.com/dtm/maintain-customer-partners) describe workflows and configurable agreement behavior; the local supplied 204 guide provides the field-level basis used here.
