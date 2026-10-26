# EdiMorph

EdiMorph reads and validates EDI from a compiled specification. This release reads an X12 004010 transaction 204 and returns that typed transaction.

## Install

```xml
<PackageReference Include="EdiMorph.X12" Version="0.1.1" />
```

`EdiMorph.X12` brings in the other three packages. All four target `net10.0`.

| Package | Contents |
| --- | --- |
| `EdiMorph.Abstractions` | Requests, diagnostics, and the handler contract |
| `EdiMorph.Specifications` | Catalog, resolver, and compiler |
| `EdiMorph.Specifications.X12` | 004010 rule documents, syntax handlers, and transaction 204 |
| `EdiMorph.X12` | The reader |

## Read a 204

```csharp
using EdiMorph;
using EdiMorph.Rules;
using EdiMorph.Specifications;
using EdiMorph.Specifications.X12;
using EdiMorph.Specifications.X12.V4010;
using EdiMorph.X12;

var rules = new RuleRegistry();
X12V4010Rules.Register(rules);

var documents = X12RuleDocuments.ReadAll()
    .Select(document => new SpecificationText(document.Path, document.Content));
var catalog = new SpecificationCatalog();
catalog.Add(new InMemorySpecificationSource(documents));

var compiler = new SpecificationCompiler(catalog, rules);
var specification = await compiler.CompileAsync(new SpecificationRequest("X12", "004010", "204"));

var result = await new X12Reader().ReadAsync(specification, rules, interchange);
if (result.IsSuccess)
{
    var carrier = result.Transactions[0].Beginning?.Elements["02"];
}
```

`result.Diagnostics` lists every standard failure. `result.Transactions` contains each 204 in the interchange.
