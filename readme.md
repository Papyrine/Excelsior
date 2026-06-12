# <img src="/src/icon.png" height="30px"> Excelsior

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/Excelsior)](https://ci.appveyor.com/project/SimonCropp/Excelsior)
[![NuGet Status](https://img.shields.io/nuget/v/Excelsior.svg?label=Excelsior)](https://www.nuget.org/packages/Excelsior/)

Excelsior is a Excel spreadsheet generation library with a distinctive data-driven approach. Uses [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) for spreadsheet creation and [OpenXmlHtml](https://github.com/SimonCropp/OpenXmlHtml) for HTML cell rendering.

**See [Milestones](../../milestones?state=closed) for release notes.**


## Open Source Maintenance Fee

This project participates in the [Open Source Maintenance Fee](https://opensourcemaintenancefee.org). The source code is freely available under the terms of the [license](license.txt). To support sustainable maintenance, use of the project's official binary releases in revenue-generating activities and all government agencies requires adherence to the [Open Source Maintenance Fee EULA](OsmfEula.txt). The fee is paid by [sponsoring Papyrine](https://github.com/sponsors/Papyrine).

This project uses [SponsorCheck](https://github.com/SimonCropp/SponsorCheck) to surface a build-time reminder in consuming projects that are not yet sponsoring.


## Usage


### Model

Given an input class:

<!-- snippet: Employee.cs -->
<a id='snippet-Employee.cs'></a>
```cs
using Excelsior;

public class Employee
{
    [Column(Heading = "Employee ID", Order = 1)]
    public required int Id;

    [Column(Heading = "Full Name", Order = 2)]
    public required string Name;

    [Column(Heading = "Email Address", Order = 3)]
    public required string Email;

    [Column(Heading = "Hire Date", Order = 4)]
    public Date? HireDate;

    [Column(Heading = "Annual Salary", Order = 5)]
    public int Salary;

    public bool IsActive;

    public EmployeeStatus Status;
}
```
<sup><a href='/src/Model/Employee.cs#L1-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-Employee.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[ColumnAttribute]` is optional. If omitted:

 * Order is based on the order of the properties defined in the class. Order can be [programmatically controlled](#column-ordering)
 * Heading text is based on the property names that is camel case split. Headings can be [programmatically controlled](#custom-headings)


### Builder

`BookBuilder` is the root entry point.

Once instantiated, the data for multiple sheets can be added.

<!-- snippet: Usage -->
<a id='snippet-Usage'></a>
```cs
var builder = new BookBuilder();

List<Employee> data =
[
    new()
    {
        Id = 1,
        Name = "John Doe",
        Email = "john@company.com",
        HireDate = new(2020, 1, 15),
        Salary = 75000,
        IsActive = true,
        Status = EmployeeStatus.FullTime
    },
    new()
    {
        Id = 2,
        Name = "Jane Smith",
        Email = "jane@company.com",
        HireDate = new(2019, 3, 22),
        Salary = 120000,
        IsActive = true,
        Status = EmployeeStatus.FullTime
    },
];
builder.AddSheet(data);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/UsageTests.cs#L7-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-Usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Result:

<img src="/src/Excelsior.Tests/UsageTests.Test_Sheet1.png">


### Worksheet Name

Worksheet defaults to `SheetN`, when `N` is a counter. So the first sheet is `Sheet1`, the second is `Sheet2`, etc.

The name can be controlled by passing an explicit value.

<!-- snippet: WorksheetName -->
<a id='snippet-WorksheetName'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees, "Employee Report");
```
<sup><a href='/src/Excelsior.Tests/WorksheetName.cs#L9-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-WorksheetName' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### DataAnnotations Attributes

`DisplayAttribute` and `DisplayNameAttribute` from [System.ComponentModel.DataAnnotations](https://www.nuget.org/packages/system.componentmodel.annotations/) are supported.

`DisplayAttribute` and `DisplayNameAttribute` are support for scenarios where it is not convenient to reference Excelsior from that assembly.<!-- singleLineInclude: DisplayAttributeScenario. path: /docs/DisplayAttributeScenario.include.md -->

<!-- snippet: DataAnnotationsModel -->
<a id='snippet-DataAnnotationsModel'></a>
```cs
public class Employee
{
    [Display(Name = "Employee ID", Order = 1)]
    public required int Id;

    [Display(Name = "Full Name", Order = 2)]
    public required string Name;

    [Display(Name = "Email Address", Order = 3)]
    public required string Email;

    [Display(Name = "Hire Date", Order = 4)]
    public Date? HireDate;

    [Display(Name = "Annual Salary", Order = 5)]
    public int Salary;

    [DisplayName("IsActive")]
    public bool IsActive { get; init; }

    public EmployeeStatus Status;
}

public enum EmployeeStatus
{
    [Display(Name = "Full Time")]
    FullTime,

    [Display(Name = "Part Time")]
    PartTime,

    [Display(Name = "Contract")]
    Contract,

    [Display(Name = "Terminated")]
    Terminated
}
```
<sup><a href='/src/Excelsior.Tests/DataAnnotationsTests.cs#L43-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-DataAnnotationsModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/DataAnnotationsTests.Simple_Sheet1.png">


### Saving to a stream

To save to a stream use `ToStream()`.

<!-- snippet: ToStream -->
<a id='snippet-ToStream'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(data);

var stream = new MemoryStream();
await builder.ToStream(stream);
```
<sup><a href='/src/Excelsior.Tests/Saving.cs#L10-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-ToStream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Saving to bytes

To save to a byte array use `ToBytes()`.

<!-- snippet: ToBytes -->
<a id='snippet-ToBytes'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(data);

var bytes = await builder.ToBytes();
```
<sup><a href='/src/Excelsior.Tests/Saving.cs#L28-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-ToBytes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Saving to a MemoryStream

To save to a `MemoryStream` use `ToMemoryStream()`. The returned stream is positioned at zero, ready to read.

<!-- snippet: ToMemoryStream -->
<a id='snippet-ToMemoryStream'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(data);

var stream = await builder.ToMemoryStream();
```
<sup><a href='/src/Excelsior.Tests/Saving.cs#L45-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-ToMemoryStream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Reading xlsx

`BookReader` is the inverse of `BookBuilder`: register the sheets to read, then `Convert` (throws on failure) or `TryConvert` (returns a result).


#### Strong-typed

The same property-discovery pipeline that drives writes also drives reads — `[Column]`, `[Display]`, `[DisplayName]`, ordering, and inclusion all carry over. When the workbook was produced by Excelsior, the column→property mapping is recovered from a custom XML metadata part written at build time, so renaming a heading on either side does not break the round-trip.

<!-- snippet: BookReaderUsage -->
<a id='snippet-BookReaderUsage'></a>
```cs
var stream = new MemoryStream();
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees());
await builder.ToStream(stream);

stream.Position = 0;

var reader = new BookReader();
var sheet = reader.AddSheet<Employee>();
reader.Convert(stream);

var employees = sheet.Rows;
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderTests.cs#L7-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


##### Model construction

Strong-typed rows are instantiated in one of two ways:

1. **Parameterless constructor (preferred)** — public or non-public. After construction, every parsed column value is applied via its property setter. `init` setters and `required` members are honoured: construction goes through `ConstructorInfo.Invoke`, which bypasses the runtime `required`-members check that `Activator.CreateInstance` would enforce.
2. **Longest matching public constructor (fallback)** — used when no parameterless constructor exists. Constructor parameters are filled by name from the parsed column values; any column whose name does not match a constructor parameter is then applied via its property setter.

This means records, primary-constructor classes, and other immutable models work without extra configuration as long as constructor parameter names match the property names.

<!-- snippet: BookReaderPositionalRecord -->
<a id='snippet-BookReaderPositionalRecord'></a>
```cs
public record PersonRecord(string Name, int Age);

[Test]
public async Task PositionalRecord()
{
    var stream = await Write(
        new PersonRecord("Alice", 30),
        new PersonRecord("Bob", 25));

    var reader = new BookReader();
    var sheet = reader.AddSheet<PersonRecord>();
    reader.Convert(stream);

    Assert.That(
        sheet.Rows,
        Is.EqualTo<PersonRecord>(
        [
            new("Alice", 30),
            new("Bob", 25)
        ]));
}
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderConstructionTests.cs#L15-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderPositionalRecord' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Limitations:

- Parameter matching is case-sensitive — a constructor parameter `name` will not bind to property `Name`.
- Public instance properties and fields are both bound. `readonly` and `const` fields are read on write but skipped on read (they keep their initializer value).
- A type with no public constructors and no parameterless constructor (public or non-public) throws on the first row.


##### Source-generated activators

Types marked with `[SheetModel]` get a source-generated factory that replaces the reflection path at row time. The generator follows the same parameterless-preferred / longest-matching-public-ctor rules and emits direct constructor calls and setter assignments — no reflection per row. Registration happens via a `[ModuleInitializer]` in the consuming assembly, so the fast path is automatic.


#### Anonymous / dictionary

For sheets without a backing model, declare every column explicitly. Each parsed row is an `IReadOnlyDictionary<string, object?>` keyed by the column name.

The `name` passed to `Column<T>` serves two roles: it is matched against the file's header row (case-insensitively) and it is the key under which the parsed value is exposed in each row dictionary. The simplest choice is the file's heading text itself. For files written by `BookBuilder`, the underlying property name can be passed instead; the workbook's metadata resolves it back to the correct column.

Because header matching is case-insensitive, names must be unique within a sheet under the same comparison — `"Name"` and `"name"` collide — and must not have leading or trailing whitespace. Either violation throws when the column is declared.

<!-- snippet: BookReaderDictionary -->
<a id='snippet-BookReaderDictionary'></a>
```cs
var reader = new BookReader();
var sheet = reader.AddSheet();
sheet
    .Column<int>("Employee ID")
    .Column<string>("Full Name")
    .Column<string>("Email Address")
    .Column<Date?>("Hire Date")
    .Column<int>("Annual Salary")
    .Column<bool>("IsActive")
    .Column<EmployeeStatus>("Status");

reader.Convert(stream);

var first = sheet.Rows[0];
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderAnonymousTests.cs#L13-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderDictionary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Multiple sheets

Register multiple sheets on the same `BookReader`. Pass a name to `AddSheet` to bind to a specific sheet by name; omit it to bind by registration order. Strong-typed and dictionary readers can be mixed freely.

Strong-typed:

<!-- snippet: BookReaderMultipleSheets -->
<a id='snippet-BookReaderMultipleSheets'></a>
```cs
var reader = new BookReader();
var staff = reader.AddSheet<Employee>("Staff");
var departments = reader.AddSheet<Department>("Departments");
reader.Convert(stream);

var employees = staff.Rows;
var depts = departments.Rows;
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderTests.cs#L92-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderMultipleSheets' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Dictionary:

<!-- snippet: BookReaderDictionaryMultipleSheets -->
<a id='snippet-BookReaderDictionaryMultipleSheets'></a>
```cs
var reader = new BookReader();

var staff = reader.AddSheet("Staff");
staff
    .Column<int>("Employee ID")
    .Column<string>("Full Name")
    .Column<string>("Email Address")
    .Column<Date?>("Hire Date")
    .Column<int>("Annual Salary")
    .Column<bool>("IsActive")
    .Column<EmployeeStatus>("Status");

var departments = reader.AddSheet("Departments");
departments
    .Column<string>("Name")
    .Column<int>("HeadCount");

reader.Convert(stream);

Assert.That(staff.Rows[0]["Employee ID"], Is.EqualTo(1));
Assert.That(staff.Rows[0]["Full Name"], Is.EqualTo("John Doe"));
Assert.That(departments.Rows.Select(_ => _["Name"]), Is.EqualTo(new object[] { "Eng", "Sales" }));
Assert.That(departments.Rows.Select(_ => _["HeadCount"]), Is.EqualTo(new object[] { 12, 7 }));
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderAnonymousTests.cs#L62-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderDictionaryMultipleSheets' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If a sheet's declared columns don't match what's in the file, that sheet's row parsing is skipped (one error per missing column, plus one error per unrecognized header column, is recorded against it), but subsequent sheets are still processed. Per-row parse errors don't have this short-circuit — they are collected per failing cell.


#### Duplicate columns

If more than one header cell in a sheet resolves to the same declared column, that's reported as an error and the sheet's rows are skipped. Both resolution paths are checked:

- **Heading match** — two header cells whose text matches the same declared column (`[Column(Heading = "...")]`, `[DisplayName]`, or property name) produce an error citing both cell references.
- **Metadata match** — the round-trip metadata XML written by `BookBuilder` is also checked. If two columns in the metadata point at the same property, that's reported separately so a corrupted or hand-edited workbook surfaces clearly rather than silently last-writes-wins.

The error message names both cell references involved (e.g. `A1 and B1`), the declared column it collided on, and which path detected it. Like the missing-column / unrecognized-header errors, a duplicate stops row parsing for that sheet but does not affect other sheets in the workbook.


#### Per-cell delegate conversion

Override the default parsing for a single column with a delegate that receives the underlying OpenXml `Cell`.

Strong-typed:

<!-- snippet: ReaderDelegate -->
<a id='snippet-ReaderDelegate'></a>
```cs
var reader = new BookReader();
var sheet = reader.AddSheet<Target>();
sheet.Convert(
    _ => _.Priority,
    cell =>
    {
        var raw = cell.InnerText.Trim().ToLowerInvariant();
        return raw switch
        {
            "low" => Priority.Low,
            "medium" => Priority.Medium,
            "high" => Priority.High,
            _ => Priority.Low
        };
    });
reader.Convert(stream);
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderDelegateTests.cs#L39-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReaderDelegate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Dictionary:

<!-- snippet: ReaderDictionaryDelegate -->
<a id='snippet-ReaderDictionaryDelegate'></a>
```cs
var reader = new BookReader();
var sheet = reader.AddSheet();
sheet.Column<string>("Code");
sheet.Column(
    "Priority",
    cell =>
    {
        var text = cell.InnerText;
        return text.Trim().ToLowerInvariant() switch
        {
            "low" => 1,
            "medium" => 2,
            "high" => 3,
            _ => 0
        };
    });
reader.Convert(stream);
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderDelegateTests.cs#L79-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReaderDictionaryDelegate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Convert vs TryConvert

`Convert` throws `ReadException` on the first batch of conversion failures. The exception's `Errors` property is the same collection that `TryConvert` exposes.

`TryConvert` never throws on data errors. It returns a `ReadResult` that is implicitly convertible to `bool` (success) and to `ReadError[]`.

<!-- snippet: BookReaderTryConvert -->
<a id='snippet-BookReaderTryConvert'></a>
```cs
var stream = await WriteStringNumber();
var reader = new BookReader();
var sheet = reader.AddSheet<IntTarget>();

var result = reader.TryConvert(stream);
if (!result)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderErrorTests.cs#L60-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookReaderTryConvert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Embedded metadata

An arbitrary instance can be embedded in the workbook itself, serialized with `System.Text.Json`. Useful for round-tripping out-of-band context — report headers, schema versions, audit info — that doesn't belong in any sheet.

The payload is written into a custom XML part with a dedicated namespace, so it coexists with the column-mapping metadata and any other custom parts.

<!-- snippet: UserMetadataUsage -->
<a id='snippet-UserMetadataUsage'></a>
```cs
var stream = new MemoryStream();
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees());
builder.SetMetadata(
    new BookHeader
    {
        Title = "Q1 staff snapshot",
        Version = 3,
        GeneratedAt = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc)
    });
await builder.ToStream(stream);

stream.Position = 0;

var reader = new BookReader();
reader.AddSheet<Employee>();
reader.Convert(stream);

var header = reader.GetMetadata<BookHeader>();
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderTests.cs#L118-L140' title='Snippet source file'>snippet source</a> | <a href='#snippet-UserMetadataUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Calling `SetMetadata` a second time with a non-null value throws — metadata is treated as a single intentional payload, so accidental double-assignment is surfaced loudly rather than silently last-wins. To overwrite deliberately, pass `null` first to clear and then set the new value.

On the reader, `GetMetadata<T>()` throws if no payload is present in the workbook. When absence is expected, use `TryGetMetadata<T>(out var value)`.


##### Raw JSON access

For callers who already hold a JSON string — or who want to inspect or rewrite the embedded payload without going through `JsonSerializer` — `BookBuilder.SetMetadata(string)`, `BookReader.GetMetadata()` and `BookReader.TryGetMetadata(out string)` operate on the raw string directly. No validation is performed on the write side.

<!-- snippet: RawMetadataUsage -->
<a id='snippet-RawMetadataUsage'></a>
```cs
var stream = new MemoryStream();
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees());
builder.SetMetadata(
    """
    {
      "title": "raw",
      "version": 7
    }
    """);
await builder.ToStream(stream);

stream.Position = 0;

var reader = new BookReader();
reader.AddSheet<Employee>();
reader.Convert(stream);

var json = reader.GetMetadata();
```
<sup><a href='/src/Excelsior.Tests/Reading/BookReaderTests.cs#L165-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-RawMetadataUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Rich text

Cells written with run-level formatting (mixed bold / colors / fonts within a single cell — Excel's "rich string" feature, which is what `IsHtml = true` produces on the write side) are flattened to plain text on read. The runs are concatenated and formatting attributes are discarded; a `string` property receives the joined text.

For formatted text that must round-trip through a workbook, store the markup as plain text in the cell rather than relying on Excel rich-text formatting. Markdown or HTML stored as a regular string is preserved exactly across write → read and can be rendered downstream. Excel's own rich-text data model has no equivalent on the .NET side and therefore cannot be reconstructed by `BookReader`.

To inspect the runs directly (e.g. to extract formatting), wire up a per-cell delegate (`sheet.Convert(_ => _.Prop, cell => ...)`) and walk the OpenXml `Run` elements.


#### Supported types

`BookReader` understands all standard .NET primitives and their nullable variants:

`bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `string`, `char`, `Guid`, `DateTime`, `DateOnly` (`Date`), `TimeOnly` (`Time`), `TimeSpan`, `DateTimeOffset`, and any `enum` (matched against the humanised display string used by the writer).


### Grouping Column Configuration

When applying multiple settings to the same column, prefer grouping them in a single `Column` call rather than using separate method calls. This makes it clearer which settings belong together.

```cs
// prefer
sheet.Column(
    _ => _.Name,
    _ =>
    {
        _.Width = 25;
        _.IsHtml = true;
        _.Render = (row, _) => $"<a href='/people/{row.Id}'>{row.Name}</a>";
    });

// over
sheet.Width(_ => _.Name, 25);
sheet.IsHtml(_ => _.Name);
sheet.Render(_ => _.Name, (row, _) => $"<a href='/people/{row.Id}'>{row.Name}</a>");
```


### Custom Headings

The heading text for a column can be overridden:


#### Fluent

<!-- snippet: CustomHeadings -->
<a id='snippet-CustomHeadings'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(
        _ => _.Name,
        _ => _.Heading = "Employee Name");
```
<sup><a href='/src/Excelsior.Tests/Headings.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomHeadings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### ColumnAttribute

```
public class Employee
{
    [Column(Heading = "Employee Name")]
    public required string Name;
```


#### DisplayNameAttribute

```
public class Employee
{
    [DisplayName("Employee Name")]
    public required string Name { get; init; }
```


#### DisplayAttribute

```
public class Employee
{
    [Display(Name = "Employee Name")]
    public required string Name;
```


#### Result:

<img src="/src/Excelsior.Tests/Headings.Fluent_Sheet1.png">


#### Order of precedence

 1. Fluent
 1. `ColumnAttribute`
 1. `DisplayAttribute`
 1. `DisplayNameAttribute`


### Column Ordering

The column order can be overridden:

<!-- snippet: ColumnOrdering -->
<a id='snippet-ColumnOrdering'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(_ => _.Email, _ => _.Order = 1)
    .Column(_ => _.Name, _ => _.Order = 2)
    .Column(_ => _.Salary, _ => _.Order = 3);
```
<sup><a href='/src/Excelsior.Tests/ColumnOrdering.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/ColumnOrdering.Fluent_Sheet1.png">


### Heading Style

<!-- snippet: HeadingStyle -->
<a id='snippet-HeadingStyle'></a>
```cs
var builder = new BookBuilder(
    headingStyle: style =>
    {
        style.Font.Bold = true;
        style.Font.Color = "FFFFFF";
        style.BackgroundColor = "00008B";
    });
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/StyleTests.cs#L10-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-HeadingStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/StyleTests.HeadingStyle_Sheet1.png">


### Global Style

<!-- snippet: GlobalStyle -->
<a id='snippet-GlobalStyle'></a>
```cs
var builder = new BookBuilder(
    globalStyle: style =>
    {
        style.Font.Bold = true;
        style.Font.Color = "FFFFFF";
        style.BackgroundColor = "00008B";
    });
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/StyleTests.cs#L33-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-GlobalStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/StyleTests.GlobalStyle_Sheet1.png">


### Cell Styling

<!-- snippet: CellStyle -->
<a id='snippet-CellStyle'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(
        _ => _.Salary,
        config =>
        {
            config.CellStyle = (style, employee, salary) =>
            {
                if (salary > 100000)
                {
                    style.Font.Color = "006400";
                    style.Font.Bold = true;
                }
            };
        })
    .Column(
        _ => _.IsActive,
        config =>
        {
            config.CellStyle = (style, employee, isActive) =>
            {
                if (isActive)
                {
                    style.BackgroundColor = "90EE90";
                }
                else
                {
                    style.BackgroundColor = "FFB6C1";
                }
            };
        });
```
<sup><a href='/src/Excelsior.Tests/StyleTests.cs#L56-L90' title='Snippet source file'>snippet source</a> | <a href='#snippet-CellStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/StyleTests.CellStyle_Sheet1.png">


### Render

<!-- snippet: CustomRender -->
<a id='snippet-CustomRender'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(
        _ => _.Name,
        _ => _.Render = (employee, name) => name.ToUpper())
    .Column(
        _ => _.IsActive,
        _ => _.Render = (employee, active) => active ? "Active" : "Inactive")
    .Column(
        _ => _.HireDate,
        _ => _.Format = "yyyy-MM-dd");
```
<sup><a href='/src/Excelsior.Tests/Render.cs#L10-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomRender' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/Render.Fluent_Sheet1.png">


### Formula

A column can emit an Excel formula per row instead of a computed value. The
callback receives a `FormulaContext<TModel>` that exposes the current
1-based Excel `Row` number and helpers to build cell references to other
columns in the same row:

 * `Ref(_ => _.OtherProperty)` — full cell reference (e.g. `B5`).
 * `Column(_ => _.OtherProperty)` — column letter only (e.g. `B`).

Formulas take precedence over the normal value rendering, and may still use
`Format` for number formatting and `CellStyle` for styling.

<!-- snippet: FormulaFluent -->
<a id='snippet-FormulaFluent'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(
        _ => _.Salary,
        _ =>
        {
            _.Formula = (employee, context) =>
                $"={context.Ref(_ => _.Id)} * 10000";
            _.Format = "#,##0";
            _.Width = 15;
        });
```
<sup><a href='/src/Excelsior.Tests/FormulaTests.cs#L10-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-FormulaFluent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The shorter `Formula()` overload on `ISheetBuilder<TModel>` can be used when
the formula does not depend on the model:

```cs
builder.AddSheet(employees)
    .Formula(
        _ => _.Salary,
        context => $"={context.Ref(_ => _.Id)} * 1000");
```

#### Result:

<img src="/src/Excelsior.Tests/FormulaTests.Fluent_Sheet1.png">

**Note:** Formulas are an Excel-only feature. They are not supported in [Word tables](#word-tables) and will throw when `Build()` is called.


### Column Widths


#### Fluent

<!-- snippet: ColumnWidths -->
<a id='snippet-ColumnWidths'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(_ => _.Name, _ => _.Width = 25)
    .Column(_ => _.Email, _ => _.Width = 30)
    .Column(_ => _.HireDate, _ => _.Width = 15);
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnWidths' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### ColumnAttribute

```
public class Employee
{
    [Column(Width = 25)]
    public required string Name;
```


#### Result:

<img src="/src/Excelsior.Tests/ColumnWidths.Fluent_Sheet1.png">


#### Order of precedence

 1. Fluent
 1. `ColumnAttribute`


### Column Min/Max Widths

Columns can be constrained to a minimum or maximum width while still auto-sizing based on content.

When `Width` is explicitly set, `MinWidth`/`MaxWidth` are ignored.

A book-wide or per-sheet `defaultMinColumnWidth` can also be set; it applies to every auto-sized column that does not have its own `MinWidth`. This pairs with the existing `defaultMaxColumnWidth` (default `50`).


#### Per-sheet default min width

<!-- snippet: SheetDefaultMinColumnWidth -->
<a id='snippet-SheetDefaultMinColumnWidth'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees, defaultMinColumnWidth: 25);
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L141-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-SheetDefaultMinColumnWidth' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Book-wide default min width

<!-- snippet: BookDefaultMinColumnWidth -->
<a id='snippet-BookDefaultMinColumnWidth'></a>
```cs
var builder = new BookBuilder(defaultMinColumnWidth: 25);
builder.AddSheet(employees);
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L158-L163' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookDefaultMinColumnWidth' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### MinWidth

<!-- snippet: ColumnMinWidth -->
<a id='snippet-ColumnMinWidth'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(_ => _.Name, _ => _.MinWidth = 40);
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L57-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnMinWidth' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### MaxWidth

<!-- snippet: ColumnMaxWidth -->
<a id='snippet-ColumnMaxWidth'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(_ => _.Name, _ => _.MaxWidth = 5);
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L99-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnMaxWidth' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Standalone methods

```cs
var sheet = builder.AddSheet(employees);
sheet.MinWidth(_ => _.Name, 40);
sheet.MaxWidth(_ => _.Email, 20);
```


#### ColumnAttribute

<!-- snippet: ColumnMinMaxWidthModel -->
<a id='snippet-ColumnMinMaxWidthModel'></a>
```cs
public class EmployeeWithMinMaxWidth
{
    [Column(MinWidth = 40)]
    public required string Name;

    [Column(MaxWidth = 20)]
    public required string Email;
}
```
<sup><a href='/src/Excelsior.Tests/ColumnWidths.cs#L471-L482' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnMinMaxWidthModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Max Row Height

Cells with wrapped or multi-line content can cause rows to grow very tall. A maximum row height (in points) can be set on the `BookBuilder` (applied to every sheet) or per sheet on `AddSheet`. Rows whose estimated content fits within the limit are left to auto-size; rows that would exceed it are capped.

The header row is exempt — it always auto-sizes, regardless of `MaxRowHeight`.


#### Per-sheet

<!-- snippet: MaxRowHeight -->
<a id='snippet-MaxRowHeight'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(notes, maxRowHeight: 60);
```
<sup><a href='/src/Excelsior.Tests/RowHeights.cs#L34-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-MaxRowHeight' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Book-wide

<!-- snippet: BookMaxRowHeight -->
<a id='snippet-BookMaxRowHeight'></a>
```cs
var builder = new BookBuilder(maxRowHeight: 60);
builder.AddSheet(notes);
```
<sup><a href='/src/Excelsior.Tests/RowHeights.cs#L51-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-BookMaxRowHeight' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Protection

Pass a `SheetProtectionOptions` to produce a [password-protected](https://support.microsoft.com/en-us/office/protect-a-worksheet-3179efdb-1285-4d49-a9c3-f4ca36276de6) workbook. By default:

 * data cells are editable, header cells are locked
 * the sheet structure (add / remove / rename / reorder) is locked
 * cell formatting, inserting, and deleting are blocked
 * sorting and using the auto-filter remain available

<!-- snippet: Protected -->
<a id='snippet-Protected'></a>
```cs
var builder = new BookBuilder(
    protection: new()
    {
        Password = "secret"
    });
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ProtectionTests.cs#L9-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-Protected' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Password is optional

If `Password` is omitted, a fresh GUID is used for each `SheetProtectionOptions` instance. That produces a workbook the user cannot manually unprotect — useful when the goal is to lock structure rather than share an unlock code.

<!-- snippet: ProtectedNoPassword -->
<a id='snippet-ProtectedNoPassword'></a>
```cs
var builder = new BookBuilder(
    protection: new());
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ProtectionTests.cs#L30-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-ProtectedNoPassword' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Customizing what is allowed

Every `SheetProtection` flag is exposed on `SheetProtectionOptions`. Booleans use OOXML's "disabled" semantics: `true` means the action is blocked when the sheet is protected.

<!-- snippet: ProtectedCustomOptions -->
<a id='snippet-ProtectedCustomOptions'></a>
```cs
var builder = new BookBuilder(
    protection: new()
    {
        Password = "secret",
        FormatCells = false,
        Sort = true,
        AutoFilter = true
    });
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ProtectionTests.cs#L49-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-ProtectedCustomOptions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### SheetProtectionOptions definition

<!-- snippet: SheetProtectionOptions.cs -->
<a id='snippet-SheetProtectionOptions.cs'></a>
```cs
namespace Excelsior;

/// <summary>
/// Defaults match a "data-entry" workbook: the user can edit unlocked data cells,
/// sort, and filter, but cannot change structure or formatting. Booleans that map to
/// SheetProtection attributes use OOXML's "disabled" semantics: true = the action is
/// blocked when the sheet is protected.
/// </summary>
public class SheetProtectionOptions
{
    /// <summary>
    /// If not provided, a fresh GUID is used. That gives a workbook the user can't
    /// accidentally unprotect — fine when the goal is to lock structure rather than
    /// share an unlock code.
    /// </summary>
    public string Password { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Block editing of embedded objects (shapes, charts, controls).
    /// </summary>
    public bool Objects { get; init; } = true;

    /// <summary>
    /// Block editing of saved scenarios (Data &gt; What-If Analysis &gt; Scenario Manager).
    /// </summary>
    public bool Scenarios { get; init; } = true;

    /// <summary>
    /// Block changing cell formatting (font, fill, number format, etc).
    /// </summary>
    public bool FormatCells { get; init; }

    /// <summary>
    /// Block changing column width / hiding columns.
    /// </summary>
    public bool FormatColumns { get; init; }

    /// <summary>
    /// Block changing row height / hiding rows.
    /// </summary>
    public bool FormatRows { get; init; } = true;

    /// <summary>
    /// Block inserting new columns.
    /// </summary>
    public bool InsertColumns { get; init; } = true;

    /// <summary>
    /// Block inserting new rows.
    /// </summary>
    public bool InsertRows { get; init; } = true;

    /// <summary>
    /// Block inserting hyperlinks.
    /// </summary>
    public bool InsertHyperlinks { get; init; } = true;

    /// <summary>
    /// Block deleting columns.
    /// </summary>
    public bool DeleteColumns { get; init; } = true;

    /// <summary>
    /// Block deleting rows.
    /// </summary>
    public bool DeleteRows { get; init; } = true;

    /// <summary>
    /// Allow selecting locked cells (e.g. headers) so users can copy from them.
    /// </summary>
    public bool SelectLockedCells { get; init; }

    /// <summary>
    /// Allow selecting unlocked (data) cells so they can be edited.
    /// </summary>
    public bool SelectUnlockedCells { get; init; }

    /// <summary>
    /// Allow sorting; useful for reviewing the data.
    /// </summary>
    public bool Sort { get; init; }

    /// <summary>
    /// Allow using the auto-filter dropdowns added to the header row.
    /// </summary>
    public bool AutoFilter { get; init; }

    /// <summary>
    /// Block editing pivot tables and pivot charts.
    /// </summary>
    public bool PivotTables { get; init; } = true;
}
```
<sup><a href='/src/Excelsior/SheetProtectionOptions.cs#L1-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-SheetProtectionOptions.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

| Option | Default | When `true`, blocks |
| --- | --- | --- |
| `Objects` | `true` | Editing embedded objects (shapes, charts, controls) |
| `Scenarios` | `true` | Editing saved scenarios (Data > What-If Analysis > Scenario Manager) |
| `FormatCells` | `false` | Changing cell formatting (font, fill, number format) |
| `FormatColumns` | `false` | Changing column width / hiding columns |
| `FormatRows` | `true` | Changing row height / hiding rows |
| `InsertColumns` | `true` | Inserting new columns |
| `InsertRows` | `true` | Inserting new rows |
| `InsertHyperlinks` | `true` | Inserting hyperlinks |
| `DeleteColumns` | `true` | Deleting columns |
| `DeleteRows` | `true` | Deleting rows |
| `SelectLockedCells` | `false` | Selecting locked cells (e.g. headers) |
| `SelectUnlockedCells` | `false` | Selecting unlocked (data) cells |
| `Sort` | `false` | Sorting |
| `AutoFilter` | `false` | Using the auto-filter dropdowns |
| `PivotTables` | `true` | Editing pivot tables and pivot charts |


### Document Properties

`BookBuilder.SetProperties` sets the workbook's document properties — the standard fields Excel surfaces in the File > Info pane, plus any user-defined entries shown on the "Custom" tab of the Advanced Properties dialog. Every field is optional; only values that are set get written, so an unset property leaves that part of the workbook untouched.

<!-- snippet: DocumentPropertiesUsage -->
<a id='snippet-DocumentPropertiesUsage'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees());
builder.SetProperties(
    new()
    {
        Title = "Q1 staff snapshot",
        Author = "Simon",
        Subject = "Headcount",
        Keywords = "staff, q1",
        Comments = "Generated by Excelsior",
        Category = "HR",
        Status = "Final",
        LastModifiedBy = "Simon",
        Company = "Papyrine",
        Manager = "Jane",
        Custom =
        {
            ["Project"] = "Excelsior",
            ["Reviewed"] = true,
            ["Revision"] = 3,
            ["Score"] = 9.5,
            ["GeneratedAt"] = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            ["DatasetId"] = new Guid("9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d")
        }
    });

using var stream = await builder.ToMemoryStream();
```
<sup><a href='/src/Excelsior.Tests/DocumentPropertiesTests.cs#L9-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-DocumentPropertiesUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The standard fields map onto the workbook's core and extended (app) property parts:

| `DocumentProperties` member | Written to |
| --- | --- |
| `Title`, `Subject`, `Keywords`, `Category`, `Status` | Core properties |
| `Author` | Core `Creator` |
| `Comments` | Core `Description` |
| `LastModifiedBy` | Core `LastModifiedBy` |
| `Company`, `Manager` | Extended (app) properties |
| `Custom` | "Custom" tab — user-defined name/value pairs |

Custom values may be a `string`, `bool`, an integral or floating-point number, `DateTime`, `DateOnly` (`Date`), or `Guid`. Any other value type throws an `ArgumentException` when the workbook is built, so convert it to a supported type first. A `null` value is written as empty text.

The last call to `SetProperties` wins; pass a fresh `DocumentProperties` to replace previously set values.

#### Reading custom properties

`BookReader` reads custom properties back with `GetCustomProperty<T>(name)` (throws when the property is absent or its value cannot be converted to `T`) and `TryGetCustomProperty<T>(name, out value)` (returns `false` in those cases instead). The type argument drives the conversion — the inverse of the value type passed in — so a `Guid` written as text is parsed back to a `Guid`. Names are matched case-insensitively. Call after `Convert`/`TryConvert`.

<!-- snippet: ReadCustomProperties -->
<a id='snippet-ReadCustomProperties'></a>
```cs
var datasetId = new Guid("9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d");

var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees());
builder.SetProperties(
    new()
    {
        Custom =
        {
            ["DatasetId"] = datasetId,
            ["Project"] = "Excelsior",
            ["Revision"] = 3,
            ["Reviewed"] = true,
            ["Score"] = 9.5
        }
    });

using var stream = await builder.ToMemoryStream();

var reader = new BookReader();
reader.AddSheet<Employee>();
reader.Convert(stream);

// The type argument drives the conversion: a Guid is parsed back from its text form.
var id = reader.GetCustomProperty<Guid>("DatasetId");
var revision = reader.GetCustomProperty<int>("Revision");
```
<sup><a href='/src/Excelsior.Tests/DocumentPropertiesTests.cs#L83-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReadCustomProperties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Cell Notes

`Note` attaches an Excel [note](https://support.microsoft.com/en-us/office/insert-comments-and-notes-in-excel-bdcc9f5d-38e2-45b4-9a92-0b2b5c7bf6f8) — the classic red-triangle annotation, [not a threaded comment](https://support.microsoft.com/en-us/office/the-difference-between-threaded-comments-and-notes-75a51eec-4092-42ab-abf8-7669077b7be3) — to a column's **heading** cell. Unlike an input hint — which only appears on an editable cell — a note stays visible on hover even when the sheet is protected and the cell is locked. That makes it the natural place to explain a constraint, document a column, or say *why* a column is read-only.

<!-- snippet: Note -->
<a id='snippet-Note'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees())
    .Note(_ => _.Salary, "Gross annual salary in USD, before tax.");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/NoteTests.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-Note' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Notes work on every sheet kind (data-bound, dictionary, and template) and combine freely with [protection](#protection) and [validation](#input-hints-and-error-messages).


### Banner Row

`Banner` adds a single merged row of arbitrary text *above* the header — a place to surface instructions to whoever edits the sheet. The row spans every column, and under [protection](#protection) it is read-only.

<!-- snippet: Banner -->
<a id='snippet-Banner'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees())
    .Banner("Confidential - for internal distribution only.");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/BannerTests.cs#L17-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-Banner' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

By default the banner and header are frozen together, so the instructions stay visible while scrolling. Pass `freeze: false` to leave the top of the sheet unpinned — Excel can only freeze contiguous rows from the top, so a scrolling banner means the header scrolls with it.

The banner row grows to fit its wrapped text, since Excel does not auto-size merged cells. Pass `maxHeight` (in points) to cap how tall it can get; text beyond the cap is clipped.

A second overload hands back the raw `Cell` instead of a string, so rich text (multiple formatted runs) can be placed in the banner:

<!-- snippet: BannerRichText -->
<a id='snippet-BannerRichText'></a>
```cs
builder.AddSheet(SampleData.Employees())
    .Banner(cell =>
    {
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new(
            new Run(
                new RunProperties(new Bold()),
                new Text("Action required: ")),
            new Run(
                new Text("complete every highlighted field.")));
    });
```
<sup><a href='/src/Excelsior.Tests/BannerTests.cs#L35-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-BannerRichText' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Banners work on every sheet kind (data-bound, dictionary, and template). The banner row count is recorded in the workbook's column metadata, so `BookReader` skips past it automatically and a bannered sheet round-trips cleanly.


### Complex Types

For complex types, by default is to render via `.ToString()`.


#### Models

<!-- snippet: ComplexTypeModels -->
<a id='snippet-ComplexTypeModels'></a>
```cs
public record Person(string Name, Address Address);

public record Address(int Number, string Street, string City, State State, ushort PostCode);
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithToString.cs#L10-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeModels' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Build

<!-- snippet: ComplexTypeWithToString -->
<a id='snippet-ComplexTypeWithToString'></a>
```cs
var builder = new BookBuilder();

List<Person> data =
[
    new("John Doe",
        new Address(
            Number: 900,
            Street: "Victoria Square",
            City: "Adelaide",
            State: State.SA,
            PostCode: 5000)),
];
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithToString.cs#L21-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeWithToString' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/ComplexTypeWithToString.Test_Sheet1.png">


### Custom render for Complex Types

<!-- snippet: ComplexTypeWithCustomRenderInit -->
<a id='snippet-ComplexTypeWithCustomRenderInit'></a>
```cs
[ModuleInitializer]
public static void Init() =>
    ValueRenderer.For<Address>(_ => $"{_.Number}, {_.Street}, {_.City}, {_.State}, {_.PostCode}");
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithCustomRender.cs#L14-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeWithCustomRenderInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: ComplexTypeWithCustomRender -->
<a id='snippet-ComplexTypeWithCustomRender'></a>
```cs
var builder = new BookBuilder();

List<Person> data =
[
    new("John Doe",
        new Address(
            Number: 900,
            Street: "Victoria Square",
            City: "Adelaide",
            State: State.SA,
            PostCode: 5000)),
];
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithCustomRender.cs#L25-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeWithCustomRender' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result:

<img src="/src/Excelsior.Tests/ComplexTypeWithCustomRender.Test_Sheet1.png">


### Links

Excelsior has first-class support for hyperlinks via the `Link` type.

```cs
namespace Excelsior;

public record Link(string Url, string? Text = null);
```

A single `Link` property renders as a clickable hyperlink. When `Text` is provided it is shown as the display text; otherwise the URL is displayed.

For `IEnumerable<Link>`, items are rendered as blue-styled rich text with URLs visible (e.g., `● Google (https://google.com)`) but are not individually clickable since Excel supports only one hyperlink per cell.


#### Model

<!-- snippet: LinkModel -->
<a id='snippet-LinkModel'></a>
```cs
public record LinkTarget(
    string Name,
    Link Link,
    Link? NullableLink,
    IEnumerable<Link> Links,
    IEnumerable<Link>? NullableLinks,
    IEnumerable<Link?> LinksWithNulls);
```
<sup><a href='/src/Excelsior.Tests/LinkTests.cs#L4-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-LinkModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Build

<!-- snippet: LinkUsage -->
<a id='snippet-LinkUsage'></a>
```cs
List<LinkTarget> data =
[
    new(
        "Test",
        new("https://google.com", "Google"),
        new("https://github.com", "GitHub"),
        [
            new("https://google.com", "Google"),
            new("https://github.com", "GitHub")
        ],
        [
            new("https://google.com", "Google")
        ],
        [
            new("https://google.com", "Google"),
            null,
            new("https://github.com", "GitHub")
        ])
];

var builder = new BookBuilder();
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/LinkTests.cs#L19-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-LinkUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/LinkTests.Test_Sheet1.png">


### HTML Cells

A column can be marked as HTML so its string values are parsed and rendered as rich text via [OpenXmlHtml](https://github.com/SimonCropp/OpenXmlHtml).

There are four equivalent ways to opt in:

```cs
// ColumnAttribute
public class Employee
{
    [Column(IsHtml = true)]
    public required string Notes;
}

// StringSyntax attribute (case-insensitive match on "html")
public class Employee
{
    [StringSyntax("html")]
    public required string Notes;
}

// Any attribute whose type name is `HtmlAttribute` (namespace ignored, matched by name)
public class Employee
{
    [Html]
    public required string Notes;
}

// Fluent
sheet.Column(
    _ => _.Notes,
    _ => _.IsHtml = true);
```

`[StringSyntax("html")]` is useful when the property is already being annotated for IDE/analyzer support and a second attribute would be redundant.

`[Html]` detection is provided as a convenience for codebases that already define a custom `HtmlAttribute` for other purposes (e.g. sanitization, templating). Excelsior does not ship this attribute — it matches any attribute whose type name is `HtmlAttribute`, regardless of namespace. This path has the lowest precedence: both `[Column(IsHtml = ...)]` and `[StringSyntax("html")]` override it.

If any two of these opt-in paths disagree — for example `[Column(IsHtml = false)]` combined with `[StringSyntax("html")]`, or a fluent `IsHtml = false` on a column where the attribute says `true` — Excelsior throws at runtime. The `EXCEL003` analyzer catches the attribute-level form of this mismatch at compile time.


### Whitespace

By default whitespace is trimmed

<!-- snippet: Whitespace -->
<a id='snippet-Whitespace'></a>
```cs
var builder = new BookBuilder();

List<Employee> data =
[
    new()
    {
        Id = 1,
        Name = "    John Doe   ",
        Email = "    john@company.com    ",
    }
];
builder.AddSheet(data);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/WhitespaceTests.cs#L7-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-Whitespace' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/WhitespaceTests.Whitespace_Sheet1.png">


#### Disable whitespace trimming

<!-- snippet: DisableWhitespaceTrim -->
<a id='snippet-DisableWhitespaceTrim'></a>
```cs
static void DisableTrimWhitespace() =>
    ValueRenderer.DisableWhitespaceTrimming();
```
<sup><a href='/src/StaticSettingsTests/DisableWhitespaceTrimmingTests.cs#L12-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableWhitespaceTrim' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/DisableWhitespaceTrimmingTests.Whitespace_Sheet1.png">


### Enumerable string properties

Properties that are castable to an `IEnumerable<string>` will automatically be rendered as a point form list.


#### Model

<!-- snippet: EnumerableModel -->
<a id='snippet-EnumerableModel'></a>
```cs
public record Person(string Name, IEnumerable<string> PhoneNumbers);
```
<sup><a href='/src/Excelsior.Tests/EnumerableStringTests.cs#L4-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnumerableModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Render

<!-- snippet: EnumerableUsage -->
<a id='snippet-EnumerableUsage'></a>
```cs
List<Person> data =
[
    new("John Doe",
        PhoneNumbers:
        [
            "+1 3057380950",
            "+1 5056169368",
            "+1 8634446859"
        ]),
];

var builder = new BookBuilder();
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/EnumerableStringTests.cs#L13-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnumerableUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/EnumerableStringTests.Test_Sheet1.png">


## Binding Model

The recommended approach is to use a specific type for binding.

This will make configuration and rendering simpler. It will often also result in better performance. The reason being that the projection into the binding type can be done by the database via an ORM. This will result in a faster query response and less data being transferred from the database.

Take for example of rendering employees to a sheet. A potential model could be `Company`, `Employee`, and `Address`.

<!-- snippet: DataModel -->
<a id='snippet-DataModel'></a>
```cs
public class Address
{
    public required int StreetNumber { get; init; }
    public required string Street { get; init; }
}

public class Company
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public class Employee
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required Company Company { get; init; }
    public required Address Address { get; init; }
    public required string Email { get; init; }
}
```
<sup><a href='/src/Excelsior.Tests/BindingModel.cs#L3-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-DataModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then a custom binding type can be used.

<!-- snippet: EmployeeBindingModel -->
<a id='snippet-EmployeeBindingModel'></a>
```cs
public class EmployeeBindingModel
{
    public required string Name;
    public required string Email;
    public required string Company;
    public required string Address;
}
```
<sup><a href='/src/Excelsior.Tests/BindingModel.cs#L28-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-EmployeeBindingModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The custom binding type can be queried and  rendered into a sheet.

<!-- snippet: ModelProjection -->
<a id='snippet-ModelProjection'></a>
```cs
var employees = dbContext
    .Employees
    .Select(_ =>
        new EmployeeBindingModel
        {
            Name = _.Name,
            Email = _.Email,
            Company = _.Company.Name,
            Address = $"{_.Address.StreetNumber} {_.Address.Street}",
        });
var builder = new BookBuilder();
builder.AddSheet(employees);
```
<sup><a href='/src/Excelsior.Tests/BindingModel.cs#L50-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-ModelProjection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Anonymous Types

Anonymous types can be used as binding models. Type-safe column configuration via `Column(_ => _.Property, ...)` works as long as it is chained directly off `AddSheet(...)` so the compiler can infer the model type.

<!-- snippet: AnonymousType -->
<a id='snippet-AnonymousType'></a>
```cs
var employees = SampleData.Employees()
    .Select(_ => new
    {
        _.Name,
        _.Email,
        _.Salary
    });

var builder = new BookBuilder();
builder.AddSheet(employees)
    .Column(
        _ => _.Salary,
        _ => _.Heading = "Annual Salary");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/AnonymousTypeTests.cs#L7-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-AnonymousType' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Dictionary Sheets

When the data isn't backed by a class, use `AddDictionarySheet` to write rows from `IReadOnlyDictionary<string, object?>`. Columns are declared explicitly via `Column<TProperty>("key", ...)`; the key is the dictionary lookup *and* the default heading. `TProperty` drives type-based defaults (date format, enum dropdown, numeric ISNUMBER validation) the same way a strong-typed property does. Keys missing from a row are written as null cells.

Keys must be unique within a sheet — compared **case-insensitively**, so `"Name"` and `"name"` collide — and must not have leading or trailing whitespace. Either violation throws when the column is declared.

<!-- snippet: DictionarySheetBasic -->
<a id='snippet-DictionarySheetBasic'></a>
```cs
var rows = new IReadOnlyDictionary<string, object?>[]
{
    new Dictionary<string, object?>
    {
        ["Name"] = "John Doe",
        ["Email"] = "john@company.com",
        ["HireDate"] = new DateTime(2020, 1, 15),
        ["Salary"] = 75_000m
    },
    new Dictionary<string, object?>
    {
        ["Name"] = "Jane Smith",
        ["Email"] = "jane@company.com",
        ["HireDate"] = new DateTime(2019, 3, 22),
        ["Salary"] = 120_000m
    }
};

var builder = new BookBuilder();
builder.AddDictionarySheet(rows)
    .Column<string>("Name", _ => _.Width = 25)
    .Column<string>("Email", _ => _.Width = 30)
    .Column<DateTime>("HireDate", _ => _.Heading = "Hire Date")
    .Column<decimal>(
        "Salary",
        _ =>
        {
            _.Heading = "Annual Salary";
            _.Format = "$#,##0.00";
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/DictionarySheetTests.cs#L7-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-DictionarySheetBasic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Type-Based Inference

A column's `TProperty` drives the same defaults as the strong-typed path. For example, an `enum`-typed column auto-derives its allowed values into a dropdown:

<!-- snippet: DictionarySheetEnumDropdown -->
<a id='snippet-DictionarySheetEnumDropdown'></a>
```cs
var rows = new IReadOnlyDictionary<string, object?>[]
{
    new Dictionary<string, object?>
    {
        ["Name"] = "Alice",
        ["Status"] = EmployeeStatus.FullTime
    },
    new Dictionary<string, object?>
    {
        ["Name"] = "Bob",
        ["Status"] = EmployeeStatus.PartTime
    },
};

var builder = new BookBuilder();
builder.AddDictionarySheet(rows)
    .Column<string>("Name")
    .Column<EmployeeStatus>("Status");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/DictionarySheetTests.cs#L78-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-DictionarySheetEnumDropdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Formulas

Formulas can reference other columns by key via string-keyed overloads on `FormulaContext`. `ctx.Ref("Quantity")` resolves to the cell reference (e.g. `B2`) for the `Quantity` column on the current row.

<!-- snippet: DictionarySheetFormula -->
<a id='snippet-DictionarySheetFormula'></a>
```cs
var rows = new IReadOnlyDictionary<string, object?>[]
{
    new Dictionary<string, object?>
    {
        ["Item"] = "Widget",
        ["Quantity"] = 3,
        ["UnitPrice"] = 10m
    },
    new Dictionary<string, object?>
    {
        ["Item"] = "Gadget",
        ["Quantity"] = 5,
        ["UnitPrice"] = 8m
    },
};

var builder = new BookBuilder();
builder.AddDictionarySheet(rows)
    .Column<string>("Item")
    .Column<int>("Quantity")
    .Column<decimal>("UnitPrice")
    .Column<decimal>(
        "Total",
        _ =>
        {
            _.Format = "$#,##0.00";
            _.Formula = (_, context) => $"={context.Ref("Quantity")}*{context.Ref("UnitPrice")}";
            _.Width = 12;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/DictionarySheetTests.cs#L109-L143' title='Snippet source file'>snippet source</a> | <a href='#snippet-DictionarySheetFormula' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Round-Trip with `BookReader`

Dictionary sheets written by `BookBuilder` can be read back by the dictionary path on `BookReader` (`reader.AddSheet()`). Column metadata records each column's key, so headings can be renamed independently of the keys without breaking the round-trip.

<!-- snippet: DictionarySheetRoundTrip -->
<a id='snippet-DictionarySheetRoundTrip'></a>
```cs
var rows = new IReadOnlyDictionary<string, object?>[]
{
    new Dictionary<string, object?>
    {
        ["Name"] = "Alice",
        ["HireDate"] = new Date(2020, 1, 15),
        ["Status"] = EmployeeStatus.FullTime
    },
    new Dictionary<string, object?>
    {
        ["Name"] = "Bob",
        ["HireDate"] = new Date(2021, 6, 1),
        ["Status"] = EmployeeStatus.PartTime
    }
};

var stream = new MemoryStream();
var builder = new BookBuilder();
builder.AddDictionarySheet(rows)
    .Column<string>("Name")
    .Column<Date>("HireDate", _ => _.Heading = "Hire Date")
    .Column<EmployeeStatus>("Status");
await builder.ToStream(stream);
stream.Position = 0;

var reader = new BookReader();
var sheet = reader.AddSheet();
sheet
    .Column<string>("Name")
    .Column<Date>("HireDate")
    .Column<EmployeeStatus>("Status");
reader.Convert(stream);

var first = sheet.Rows[0];
```
<sup><a href='/src/Excelsior.Tests/DictionarySheetTests.cs#L151-L188' title='Snippet source file'>snippet source</a> | <a href='#snippet-DictionarySheetRoundTrip' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Template Sheets

`AddTemplateSheet` produces an empty spreadsheet for the user to fill in — known column names, types, widths, formats, and validation but no data rows. Validation, locked-cell behavior, and conditional formatting all extend down `templateRowCount` rows below the header (defaults to 1000).

Column names must be unique within a sheet — compared **case-insensitively**, so `"Name"` and `"name"` collide — and must not have leading or trailing whitespace. Either violation throws when the column is declared.

<!-- snippet: TemplateSheetBasic -->
<a id='snippet-TemplateSheetBasic'></a>
```cs
var builder = new BookBuilder();
builder.AddTemplateSheet("Employees")
    .Column<string>("Name", _ => _.Width = 25)
    .Column<string>("Email", _ => _.Width = 30)
    .Column<DateTime>(
        "HireDate",
        _ =>
        {
            _.Heading = "Hire Date";
            _.Width = 15;
        })
    .Column<decimal>(
        "Salary",
        _ =>
        {
            _.Heading = "Annual Salary";
            _.Format = "$#,##0.00";
            _.Width = 18;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L7-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetBasic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Type-Based Inference

Template sheets infer common validation rules from the column's type:

| Signal | Inferred | Gated by `inferValidationFromTypes` |
| --- | --- | :---: |
| `enum` / `enum?` | dropdown list of enum members | no — always on |
| `bool` / `bool?` | dropdown of `TRUE` / `FALSE` (see [note](#bool-dropdown-vs-strict-boolean) below) | no — always on |
| Numeric (`int`, `decimal`, `double`, etc.) | `ISNUMBER` constraint — manually-typed non-numeric values are blocked | no — always on |
| Date/time (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) | date constraint across Excel's full date span — manually-typed non-dates are blocked | no — always on |
| C# `required` modifier or `[Required]` attribute | `Required = true` | no — always on |
| Non-nullable value type (`int`, `decimal`, `DateTime`, `bool`, enum) | `Required = true` | yes |
| Non-nullable reference type (NRT-aware, data-bound only) | `Required = true` | yes |

When a column is `Required` and has no other validation type (e.g. a non-empty string), Excelsior emits a `LEN(TRIM(...))>0` custom validation with `allowBlank="0"`. Excel blocks blank entries with the default message *"This field is required."* — clearing the cell triggers the Stop popup. Set `ErrorMessage` to override.

```cs
public class Employee
{
   // always-on Required
    public required string Name;

    // always-on Required
    [Required]
    public string Email = "";

    // not Required
    public string? Notes { get; init; }
}
```

<!-- snippet: TemplateInferenceDefaults -->
<a id='snippet-TemplateInferenceDefaults'></a>
```cs
var builder = new BookBuilder();
builder.AddTemplateSheet("Employees", templateRowCount: 10)
    .Column<string>("Name")
    .Column<int>("Age")
    .Column<bool>("IsActive")
    .Column<DateTime>("HireDate");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TypeInferenceTests.cs#L17-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateInferenceDefaults' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inference is **on by default for `AddTemplateSheet`** and **off by default for `AddSheet`**. Pass `inferValidationFromTypes: false` to disable on a template, or `inferValidationFromTypes: true` to opt in on a data-bound sheet:

<!-- snippet: DataBoundInferenceEnabled -->
<a id='snippet-DataBoundInferenceEnabled'></a>
```cs
InferenceModel[] data =
[
    new()
    {
        Name = "Alice",
        Age = 30,
        IsActive = true,
        HireDate = new(2020, 1, 1)
    }
];

var builder = new BookBuilder();
builder.AddSheet(
    data,
    templateRowCount: 5,
    inferValidationFromTypes: true);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TypeInferenceTests.cs#L145-L166' title='Snippet source file'>snippet source</a> | <a href='#snippet-DataBoundInferenceEnabled' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Per-column overrides always win — set `Required = false` or `DisableAllowedValues = true` to opt out for one column.

<a id="bool-dropdown-vs-strict-boolean"></a>
**Note on the bool dropdown.** Excel does have a native Boolean cell type (OOXML `t="b"`) and Excelsior writes `bool` values that way. The auto-derived dropdown is a *string list* of `TRUE,FALSE`, so when a user picks an entry Excel inserts the literal text — in practice it auto-coerces back to a Boolean cell on edit, but the result can be mixed cell types in the same column (Booleans written by Excelsior vs. strings the user picked) which can affect formulas like `=A2*1` or `COUNTIF(A:A, TRUE)`. For strict Boolean enforcement, set `DisableAllowedValues = true` on the column — this drops the dropdown so a custom `=ISLOGICAL(A2)` constraint can be supplied via a custom data validation. For typical data-entry templates the dropdown is the better UX.

#### Auto-Derived Enum Dropdowns

Enum-typed columns automatically render as dropdown lists (regardless of the inference flag). The list values match the same rendering used for cell content, so `[Display(Name = "Full Time")]` shows "Full Time" in the dropdown.

<!-- snippet: TemplateSheetEnumDropdown -->
<a id='snippet-TemplateSheetEnumDropdown'></a>
```cs
var builder = new BookBuilder(headingStyle: _ => _.Font.Bold = true);
builder.AddTemplateSheet("Employees", templateRowCount: 50)
    .Column<string>("Name", _ => _.Width = 25)
    .Column<EmployeeStatus>("Status", _ => _.Width = 14);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L39-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetEnumDropdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This applies to data-bound sheets too — set `templateRowCount` on `AddSheet` to extend dropdowns past the data rows so users adding new rows still get validation.

<!-- snippet: ValidationEnumDropdown -->
<a id='snippet-ValidationEnumDropdown'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees(), templateRowCount: 25);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L7-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValidationEnumDropdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To suppress the auto-derived dropdown for a specific column, set `DisableAllowedValues = true`. Setting `AllowedValues` explicitly to a list overrides the auto-derived values.

#### Numeric and Date Ranges

Restrict entry to a numeric or date range via `Range(min, max)`, or set `NumericMin`/`NumericMax`/`DateMin`/`DateMax` individually for one-sided constraints.

<!-- snippet: TemplateSheetNumericRange -->
<a id='snippet-TemplateSheetNumericRange'></a>
```cs
var builder = new BookBuilder();
builder.AddTemplateSheet("Scorecard", templateRowCount: 25)
    .Column<string>("Name", _ => _.Width = 25)
    .Column<int>(
        "Score",
        _ =>
        {
            _.Width = 10;
            _.Range(0, 100);
            _.InputTitle = "Score";
            _.InputMessage = "Whole number between 0 and 100.";
            _.ErrorTitle = "Invalid score";
            _.ErrorMessage = "Score must be between 0 and 100.";
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L56-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetNumericRange' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: TemplateSheetDateRange -->
<a id='snippet-TemplateSheetDateRange'></a>
```cs
var builder = new BookBuilder();
builder.AddTemplateSheet("Hires", templateRowCount: 25)
    .Column<string>("Name", _ => _.Width = 25)
    .Column<DateTime>(
        "HireDate",
        _ =>
        {
            _.Heading = "Hire Date";
            _.Width = 15;
            _.Range(new(2020, 1, 1), new DateTime(2030, 12, 31));
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L83-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetDateRange' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Input Hints and Error Messages

Excel speaks to editors through two channels, and Excelsior fills both in automatically so a constraint is never a hidden surprise:

- **Input hint** — the tooltip shown *proactively* when a cell is selected, before anything is typed. This is the only channel that tells an editor the rule *before* they break it.
- **Error alert** — the popup shown *reactively* after an invalid value is rejected.

Both ride on Excel's [data validation](https://support.microsoft.com/en-us/office/apply-data-validation-to-cells-29fecbcc-d1b9-42c1-9d76-eff3ce5f7249): the input hint is its *input message*; the rejection popup is its *error alert*.

Every constrained column advertises its rule as an input hint with no configuration. A hint is derived for:

- **allowed-value lists / dropdowns** — `"Select one of: A, B, C."`
- **numeric ranges** — `"Enter a number between X and Y."` (a one-sided bound becomes *greater than or equal to* / *less than or equal to*)
- **date ranges** — `"Enter a date between X and Y."`, with the same one-sided variants
- **required cells** — `"This field is required."`, shown only when no range or list also applies to the column

The one constraint left **without** a hint is the bare `ISNUMBER` check on a plain numeric column — that a number column holds numbers is self-evident, so it is not surfaced on every cell select.

<!-- snippet: AutoInputMessage -->
<a id='snippet-AutoInputMessage'></a>
```cs
// No InputMessage is set, yet the Range constraint is surfaced as an input hint
// ("Enter a number between 0 and 1000000.") shown when the cell is selected.
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees(), templateRowCount: 10)
    .Column(_ => _.Salary, _ => _.Range(0, 1_000_000));

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L45-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-AutoInputMessage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Set `InputTitle` / `InputMessage` to replace the auto-generated hint with custom wording, or suppress the auto hints entirely — `DisableInputMessage` for a single column, or `DisableInputMessages` for the whole sheet. An explicit `InputMessage` always survives either.

<!-- snippet: DisableInputMessage -->
<a id='snippet-DisableInputMessage'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 10);
sheet.Range(_ => _.Salary, 0, 1_000_000);
sheet.DisableInputMessage(_ => _.Salary);

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L63-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableInputMessage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`DisableInputMessages` turns the auto hints off across the sheet in one call (handy for a dense data-entry sheet where every column would otherwise carry a tooltip):

<!-- snippet: DisableInputMessages -->
<a id='snippet-DisableInputMessages'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 10);
sheet.Range(_ => _.Salary, 0, 1_000_000);
// Turn the auto-generated hints off for every column on the sheet...
sheet.DisableInputMessages();
// ...but an explicit InputMessage still applies.
sheet.InputMessage(_ => _.Email, "Use the corporate address.");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L80-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableInputMessages' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ErrorTitle` / `ErrorMessage` override the rejection popup. When unset, Excelsior fills in a default based on the validation type — `"Must be one of: A, B, C."` for dropdowns, `"Must be a number between X and Y."` for ranges, `"Must be a number."` for the auto-`ISNUMBER` constraint, etc.

`ErrorStyle` controls Excel's response to invalid input:

| Style | Behavior |
| --- | --- |
| `Stop` (default) | Block the entry — user must enter a valid value or cancel. |
| `Warning` | Warn the user; they can choose to keep the invalid value. |
| `Information` | Inform the user; the value is accepted regardless. |

<!-- snippet: ValidationErrorStyleWarning -->
<a id='snippet-ValidationErrorStyleWarning'></a>
```cs
var builder = new BookBuilder();
builder.AddSheet(SampleData.Employees(), templateRowCount: 5)
    .Column(
        _ => _.Salary,
        _ =>
        {
            _.Range(0, 1_000_000);
            _.ErrorStyle = ValidationErrorStyle.Warning;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L140-L154' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValidationErrorStyleWarning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Required Cells

`Required = true` does two things:

1. Highlights blank cells in the column with a soft red conditional-format fill, drawing attention to fields the user has not yet filled in.
2. When the column has no other validation type (e.g. a free-text string), emits a `LEN(TRIM(...))>0` custom validation with `allowBlank="0"` so Excel rejects blank values typed into the cell.

**Excel limitation.** Excel's data validation only fires on *typed* entry. It does **not** fire when the user clears a cell with the Delete key, pastes a blank value in, fills via drag, or writes from a macro — that's documented Excel behavior, not something Excelsior can override. So:

- Typing a blank value and pressing Enter → the Stop popup fires and the entry is rejected.
- Pressing Delete to clear an existing value → the cell goes blank silently, but the conditional-formatting highlight makes the gap visible.

For true enforcement at save time (block save while any required cell is blank), a workbook-level VBA macro on `Workbook_BeforeSave` is required — that's out of scope for a template generator.

<!-- snippet: TemplateSheetRequired -->
<a id='snippet-TemplateSheetRequired'></a>
```cs
var builder = new BookBuilder();
builder.AddTemplateSheet("Employees", templateRowCount: 25)
    .Column<string>(
        "Name",
        _ =>
        {
            _.Width = 25;
            _.Required = true;
        })
    .Column<string>("Email", _ => _.Width = 30)
    .Column<DateTime>(
        "HireDate",
        _ =>
        {
            _.Heading = "Hire Date";
            _.Width = 15;
            _.Required = true;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L107-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetRequired' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Locked Cells under Protection

When the workbook is built with `SheetProtectionOptions`, headings are locked and data cells are unlocked by default. Set `Locked = true` on a column to lock its data cells too — useful for read-only identifier columns or pre-filled formula results.

<!-- snippet: TemplateSheetProtected -->
<a id='snippet-TemplateSheetProtected'></a>
```cs
var builder = new BookBuilder(
    protection: new()
    {
        Password = "secret"
    });
builder.AddTemplateSheet("Employees", templateRowCount: 25)
    .Column<string>("Name", _ => _.Width = 25)
    .Column<string>(
        "EmployeeId",
        _ =>
        {
            _.Heading = "Employee ID";
            _.Width = 14;
            _.Locked = true;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L138-L158' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetProtected' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Combining Features

A typical "data entry" workbook combines several of these features:

<!-- snippet: TemplateSheetFullFeatured -->
<a id='snippet-TemplateSheetFullFeatured'></a>
```cs
var builder = new BookBuilder(
    headingStyle: _ =>
    {
        _.Font.Bold = true;
        _.BackgroundColor = "FFEFEFEF";
    });
builder.AddTemplateSheet("Employees", templateRowCount: 100)
    .Column<string>(
        "Name",
        _ =>
        {
            _.Width = 25;
            _.Required = true;
            _.InputMessage = "Full name of the employee.";
        })
    .Column<string>(
        "Email",
        _ =>
        {
            _.Width = 30;
            _.Required = true;
        })
    .Column<DateTime>(
        "HireDate",
        _ =>
        {
            _.Heading = "Hire Date";
            _.Width = 15;
            _.Required = true;
            _.Range(new(2020, 1, 1), new DateTime(2030, 12, 31));
            _.ErrorMessage = "Hire date must be on or after 2020-01-01.";
        })
    .Column<decimal>(
        "Salary",
        _ =>
        {
            _.Heading = "Annual Salary";
            _.Format = "$#,##0.00";
            _.Width = 18;
            _.Range(0m, 1_000_000m);
        })
    .Column<EmployeeStatus>(
        "Status",
        _ =>
        {
            _.Width = 14;
            _.Required = true;
        });

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/TemplateSheetTests.cs#L166-L219' title='Snippet source file'>snippet source</a> | <a href='#snippet-TemplateSheetFullFeatured' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

All of the above features (`AllowedValues`, `Range`, `Required`, `Locked`, `InputMessage`, `ErrorMessage`) work the same way on the data-bound `AddSheet(...).Column(_ => _.Foo, c => ...)` API.

#### Shortcut Methods

The data-bound `ISheetBuilder<TModel>` exposes each validation feature as a one-line shortcut, mirroring the existing `Width`, `Format`, `Filter` etc. shortcuts.

| Shortcut | Configures |
| --- | --- |
| `AllowedValues(p, values)` | dropdown list |
| `DisableAllowedValues(p)` | suppress the auto-derived enum dropdown |
| `Range(p, decimal min, decimal max)` | numeric range |
| `Range(p, DateTime min, DateTime max)` | date range |
| `Required(p)` | conditional-format blank highlight |
| `Locked(p, value = true)` | per-column lock under protection |
| `InputMessage(p, message, title = null)` | input-hint tooltip |
| `ErrorMessage(p, message, title = null)` | error popup on invalid input |

<!-- snippet: ValidationShortcuts -->
<a id='snippet-ValidationShortcuts'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 25);
sheet.Range(_ => _.Salary, 0, 1_000_000);
sheet.Required(_ => _.Email);
sheet.InputMessage(_ => _.Salary, "Annual salary in USD.", "Salary");
sheet.ErrorMessage(_ => _.Salary, "Salary must be between 0 and 1,000,000.", "Invalid salary");

using var book = await builder.Build();
```
<sup><a href='/src/Excelsior.Tests/ValidationTests.cs#L162-L173' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValidationShortcuts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### ColumnAttribute

`ColumnAttribute` allows customization of rendering at the model level.

It is intended as the preferred approach over usage of `DisplayAttribute` and `DisplayNameAttribute`.

`DisplayAttribute` and `DisplayNameAttribute` are support for scenarios where it is not convenient to reference Excelsior from that assembly.<!-- singleLineInclude: DisplayAttributeScenario. path: /docs/DisplayAttributeScenario.include.md -->


#### ColumnAttribute definition

<!-- snippet: ColumnAttribute.cs -->
<a id='snippet-ColumnAttribute.cs'></a>
```cs
using JetBrains.Annotations;

namespace Excelsior;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class ColumnAttribute :
    Attribute
{
    public string? Heading { get; set; }
    public int Order { get; set; } = -1;
    public int Width { get; set; } = -1;
    public int MinWidth { get; set; } = -1;
    public int MaxWidth { get; set; } = -1;
    public string? Format { get; set; }
    public string? NullDisplay { get; set; }

    public bool IsHtml
    {
        get;
        set
        {
            field = value;
            IsHtmlHasValue = true;
        }
    }

    internal bool IsHtmlHasValue { get; private set; }

    public bool Filter
    {
        get;
        set
        {
            field = value;
            FilterHasValue = true;
        }
    }

    internal bool FilterHasValue { get; private set; }

    public bool Include
    {
        get;
        set
        {
            field = value;
            IncludeHasValue = true;
        }
    } = true;

    internal bool IncludeHasValue { get; private set; }

    /// <summary>
    /// When <see cref="Include"/> is explicitly <c>false</c> the column is dropped before
    /// any other setting is read, so every other setting is silently ignored. Returns the
    /// names of any such redundant settings (empty when there is no conflict).
    /// </summary>
    internal IReadOnlyList<string> ConflictingExclusionSettings()
    {
        if (!IncludeHasValue || Include)
        {
            return [];
        }

        var conflicts = new List<string>();
        if (Heading != null)
        {
            conflicts.Add(nameof(Heading));
        }

        if (Order > -1)
        {
            conflicts.Add(nameof(Order));
        }

        if (Width > -1)
        {
            conflicts.Add(nameof(Width));
        }

        if (MinWidth > -1)
        {
            conflicts.Add(nameof(MinWidth));
        }

        if (MaxWidth > -1)
        {
            conflicts.Add(nameof(MaxWidth));
        }

        if (Format != null)
        {
            conflicts.Add(nameof(Format));
        }

        if (NullDisplay != null)
        {
            conflicts.Add(nameof(NullDisplay));
        }

        if (IsHtmlHasValue)
        {
            conflicts.Add(nameof(IsHtml));
        }

        if (FilterHasValue)
        {
            conflicts.Add(nameof(Filter));
        }

        return conflicts;
    }
}
```
<sup><a href='/src/Excelsior/Attributes/ColumnAttribute.cs#L1-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnAttribute.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Usage

<!-- snippet: ColumnAttributeModel -->
<a id='snippet-ColumnAttributeModel'></a>
```cs
public class Employee
{
    [Column(Heading = "Employee ID", Order = 1, Format = "0000")]
    public required int Id;

    [Column(Heading = "Full Name", Order = 2, Width = 20)]
    public required string Name;

    [Column(Heading = "Email Address", Width = 30)]
    public required string Email;

    [Column(Order = 3, NullDisplay = "unknown")]
    public Date? HireDate;
}
```
<sup><a href='/src/Excelsior.Tests/ColumnAttributeTests.cs#L4-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnAttributeModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: ColumnAttribute -->
<a id='snippet-ColumnAttribute'></a>
```cs
var builder = new BookBuilder();

List<Employee> data =
[
    new()
    {
        Id = 1,
        Name = "John Doe",
        Email = "john@company.com",
        HireDate = new(2020, 1, 15),
    },
    new()
    {
        Id = 2,
        Name = "Jane Smith",
        Email = "jane@company.com",
        HireDate = null,
    }
];

builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/ColumnAttributeTests.cs#L26-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-ColumnAttribute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/ColumnAttributeTests.Test_Sheet1.png">


### ValueRenderer.ForEnums

`ValueRenderer.ForEnums` can be used to control the rendering for all enums


#### Config in a ModuleInitializer

<!-- snippet: ValueRendererForEnumsInit -->
<a id='snippet-ValueRendererForEnumsInit'></a>
```cs
static void CustomEnumRender() =>
    ValueRenderer.ForEnums(_ => _.ToString().ToUpper());
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForEnums.cs#L12-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForEnumsInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example use

<!-- snippet: ValueRendererForEnums -->
<a id='snippet-ValueRendererForEnums'></a>
```cs
var builder = new BookBuilder();

List<Car> data =
[
    new()
    {
        Manufacturer = Manufacturer.BuildYourDream,
        Color = Color.AntiqueWhite,
        NullableColor = Color.AntiqueWhite,
    },
    new()
    {
        Manufacturer = Manufacturer.BuildYourDream,
        Color = Color.AntiqueWhite,
    }
];
builder.AddSheet(data);
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForEnums.cs#L22-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForEnums' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/ValueRendererForEnums.Test_Sheet1.png">


### ValueRenderer.ForEnums using Humanizer

Using [Humanizer](https://github.com/Humanizr/Humanizer) to convert enums to strings:

Humanizer is not a dependency of Excelsior, so a reference to the [Humanizer](https://nuget.org/packages/Humanizer) NuGet package (or `Humanizer.Core`) must be added to make the `Humanize()` extension method available.


#### Config in a ModuleInitializer

<!-- snippet: ValueRendererForEnumsHumanizerInit -->
<a id='snippet-ValueRendererForEnumsHumanizerInit'></a>
```cs
static void CustomEnumRender() =>
    ValueRenderer.ForEnums(_ => _.Humanize());
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForEnumsHumanizer.cs#L12-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForEnumsHumanizerInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example use

<!-- snippet: ValueRendererForEnumsHumanizer -->
<a id='snippet-ValueRendererForEnumsHumanizer'></a>
```cs
var builder = new BookBuilder();

List<Car> data =
[
    new()
    {
        Manufacturer = Manufacturer.BuildYourDream,
        Color = Color.AntiqueWhite,
        NullableColor = Color.AntiqueWhite,
    },
    new()
    {
        Manufacturer = Manufacturer.BuildYourDream,
        Color = Color.AntiqueWhite,
    }
];
builder.AddSheet(data);
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForEnumsHumanizer.cs#L22-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForEnumsHumanizer' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/ValueRendererForEnumsHumanizer.Test_Sheet1.png">


### ValueRenderer.For&lt;T&gt;

`ValueRenderer.For<T>` can be used to control the rendering for all instances of a specific type. See [ValueRendererForSpecificType](/src/StaticSettingsTests/ValueRendererForSpecificType.cs) for an example with custom enums.

> [!NOTE]
> `ValueRenderer.For<bool>` and `ValueRenderer.NullDisplayFor<bool>` throw — replacing the cell value with a string would lose Excel's native boolean type, so formulas like `=IF(A2, ...)` and `COUNTIF(A:A, TRUE)` would stop working. Use [`ValueRenderer.BoolDisplay`](#valuerendererbooldisplay) instead, which keeps cells as native booleans and applies the display via a number format.


#### Type specificity

When multiple `For<T>` registrations match a property type, the most specific type wins. For example, `For<Color>(...)` takes precedence over `For<Enum>(...)` for `Color` properties, while other enum types still use the `Enum` fallback.


### ValueRenderer.BoolDisplay

`ValueRenderer.BoolDisplay` controls how `bool` and `bool?` columns render in Excel. Cells stay native booleans (`t="b"`) so Excel formulas continue to recognize them as boolean values; the display strings are applied via the number format `[=1]"trueDisplay";[=0]"falseDisplay"`. The optional third argument supplies a display for `null` cells in `bool?` columns.


#### Config in a ModuleInitializer

<!-- snippet: ValueRendererForBoolInit -->
<a id='snippet-ValueRendererForBoolInit'></a>
```cs
static void ConfigureBoolDisplay() =>
    ValueRenderer.BoolDisplay("Yes", "No", "Unknown");
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForBool.cs#L12-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForBoolInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example use

<!-- snippet: ValueRendererForBool -->
<a id='snippet-ValueRendererForBool'></a>
```cs
var builder = new BookBuilder();

List<Target> data =
[
    new()
    {
        Name = "Alice",
        IsActive = true,
        IsAdmin = true,
    },
    new()
    {
        Name = "Bob",
        IsActive = false,
        IsAdmin = false,
    },
    new()
    {
        Name = "Carol",
        IsActive = true,
        IsAdmin = null,
    }
];
builder.AddSheet(data);
```
<sup><a href='/src/StaticSettingsTests/ValueRendererForBool.cs#L22-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererForBool' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/ValueRendererForBool.Test_Sheet1.png">


### ValueRenderer.NullDisplayFor&lt;T&gt;

`ValueRenderer.NullDisplayFor<T>` can be used to control the display text when a nullable property is null. This combines well with `ValueRenderer.For<T>` — the rendered value is used when the property has a value, and the null display when it doesn't. Type specificity applies the same way as `For<T>`: a more specific registration wins over a less specific one for matching properties.

> [!NOTE]
> `NullDisplayFor<bool>` throws — use [`ValueRenderer.BoolDisplay`](#valuerendererbooldisplay) and pass the third (`nullDisplay`) argument.


#### Config in a ModuleInitializer

<!-- snippet: ValueRendererNullDisplayForTypeInit -->
<a id='snippet-ValueRendererNullDisplayForTypeInit'></a>
```cs
static void CustomNullDisplayForType()
{
    ValueRenderer.For<Address>(_ => $"{_.Street}, {_.City}");
    ValueRenderer.NullDisplayFor<Address>("No address on file");
}
```
<sup><a href='/src/StaticSettingsTests/ValueRendererNullDisplayForType.cs#L15-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererNullDisplayForTypeInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example use

<!-- snippet: ValueRendererNullDisplayForType -->
<a id='snippet-ValueRendererNullDisplayForType'></a>
```cs
var builder = new BookBuilder();

List<Target> data =
[
    new()
    {
        Name = "Alice",
        Address = new()
        {
            Street = "1 Park Ave",
            City = "Springfield"
        }
    },
    new()
    {
        Name = "Bob",
        Address = null
    }
];
builder.AddSheet(data);
```
<sup><a href='/src/StaticSettingsTests/ValueRendererNullDisplayForType.cs#L28-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererNullDisplayForType' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/ValueRendererNullDisplayForType.Test_Sheet1.png">


### ValueRenderer.NullDisplayFor&lt;Enum&gt;

`ValueRenderer.NullDisplayFor<Enum>` can be used to set a default display text for all null enum properties:


#### Config in a ModuleInitializer

<!-- snippet: ValueRendererNullDisplayForEnumInit -->
<a id='snippet-ValueRendererNullDisplayForEnumInit'></a>
```cs
static void CustomNullEnumDisplay() =>
    ValueRenderer.NullDisplayFor<Enum>("Unknown");
```
<sup><a href='/src/StaticSettingsTests/ValueRendererNullDisplayForEnum.cs#L12-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererNullDisplayForEnumInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example use

<!-- snippet: ValueRendererNullDisplayForEnum -->
<a id='snippet-ValueRendererNullDisplayForEnum'></a>
```cs
var builder = new BookBuilder();

List<Target> data =
[
    new()
    {
        Name = "Alice",
        Color = Color.AntiqueWhite,
    },
    new()
    {
        Name = "Bob",
    }
];
builder.AddSheet(data);
```
<sup><a href='/src/StaticSettingsTests/ValueRendererNullDisplayForEnum.cs#L22-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-ValueRendererNullDisplayForEnum' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/ValueRendererNullDisplayForEnum.Test_Sheet1.png">

The same type specificity applies to `NullDisplayFor<T>`: `NullDisplayFor<Color>("Color unknown")` takes precedence over `NullDisplayFor<Enum>("Enum unknown")` for `Color?` properties.


### Date formats

`DateTime`, `DateOnly`, and `TimeOnly` are passed directly in to the respective library.

Excel is directed (using a format string) to render the value using the following:

 * `yyyy-MM-dd HH:mm:ss` for `DateTime`s
 * `yyyy-MM-dd` for `DateOnly`s
 * `HH:mm:ss` for `TimeOnly`s

Excel has no direct support for `DateTimeOffset` — a cell is either a number (formatted as a date) or a string, and the offset cannot be represented natively. So `DateTimeOffset`s are stored as strings using the `yyyy-MM-dd HH:mm:ss z` format and `CultureInfo.InvariantCulture`. This preserves the offset on round-trip, but the cell is plain text — Excel will not treat it as a date for sorting, filtering, or arithmetic, and the format string is applied at write time rather than via Excel's cell number format.


### Custom Date formats

Date formats can be customized:

<!-- snippet: DateFormatsInit -->
<a id='snippet-DateFormatsInit'></a>
```cs
static void CustomDateFormats()
{
    ValueRenderer.DefaultDateFormat = "yyyy/MM/dd" ;
    ValueRenderer.DefaultDateTimeFormat = "yyyy/MM/dd HH:mm:ss" ;
    ValueRenderer.DefaultDateTimeOffsetFormat = "yyyy/MM/dd HH:mm:ss z" ;
    ValueRenderer.DefaultTimeFormat = "HH:mm:ss" ;
}
```
<sup><a href='/src/StaticSettingsTests/DateFormats.cs#L18-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-DateFormatsInit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/StaticSettingsTests/DateFormats.Test_Sheet1.png">


### Filters

By default, auto-filter is enabled on all columns.


#### Disable all filters

<!-- snippet: FilterAllOff -->
<a id='snippet-FilterAllOff'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(Data());
sheet.DisableFilter();
```
<sup><a href='/src/Excelsior.Tests/FilterTests.cs#L32-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-FilterAllOff' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/FilterTests.AllOff_Sheet1.png">


#### Enable filter on specific columns

Filters can be disabled at the sheet level, then selectively enabled on specific columns:

<!-- snippet: FilterDefaultOffWithOneOn -->
<a id='snippet-FilterDefaultOffWithOneOn'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(Data());
sheet.DisableFilter();
sheet.Filter(_ => _.Name);
```
<sup><a href='/src/Excelsior.Tests/FilterTests.cs#L47-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-FilterDefaultOffWithOneOn' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/FilterTests.DefaultOffWithOneOn_Sheet1.png">


#### Disable filter on specific columns

Individual columns can opt out of filtering while the rest remain enabled:

<!-- snippet: FilterDefaultOnWithOneOff -->
<a id='snippet-FilterDefaultOnWithOneOff'></a>
```cs
var builder = new BookBuilder();
var sheet = builder.AddSheet(Data());
sheet.Column(
    _ => _.Age,
    _ => _.Filter = false);
```
<sup><a href='/src/Excelsior.Tests/FilterTests.cs#L63-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-FilterDefaultOnWithOneOff' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/FilterTests.DefaultOnWithOneOff_Sheet1.png">


#### ColumnAttribute

```
public class Employee
{
    [Column(Filter = true)]
    public required string Name { get; init; }
```


### Include/Exclude Columns

Columns can be included or excluded from the output at runtime. This is useful when generating multiple spreadsheets from the same model with different columns based on some state.


#### Toggle based on state

<!-- snippet: IncludeToggleBasedOnState -->
<a id='snippet-IncludeToggleBasedOnState'></a>
```cs
var data = Data();
var isInternalReport = true;

var builder = new BookBuilder();
var sheet = builder.AddSheet(data);
sheet.Include(_ => _.Email, !isInternalReport);
```
<sup><a href='/src/Excelsior.Tests/IncludeTests.cs#L101-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeToggleBasedOnState' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/IncludeTests.ToggleBasedOnState_Sheet1.png">


#### Multiple spreadsheets from the same model

The same data can produce different reports by toggling column inclusion per spreadsheet:

<!-- snippet: IncludeMultipleSpreadsheets_Public -->
<a id='snippet-IncludeMultipleSpreadsheets_Public'></a>
```cs
var data = Data();

// Public report: exclude age and email
var builder = new BookBuilder();
var sheet = builder.AddSheet(data);
sheet.Exclude(_ => _.Age);
sheet.Exclude(_ => _.Email);
```
<sup><a href='/src/Excelsior.Tests/IncludeTests.cs#L119-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeMultipleSpreadsheets_Public' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/IncludeTests.MultipleSpreadsheetsSameModel_Public_Sheet1.png">

<!-- snippet: IncludeMultipleSpreadsheets_Internal -->
<a id='snippet-IncludeMultipleSpreadsheets_Internal'></a>
```cs
List<Target> data = [
    new("Alice", 30, "alice@test.com"),
    new("Bob", 25, "bob@test.com")
];

// Internal report: include all columns
var builder = new BookBuilder();
builder.AddSheet(data);
```
<sup><a href='/src/Excelsior.Tests/IncludeTests.cs#L138-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeMultipleSpreadsheets_Internal' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/IncludeTests.MultipleSpreadsheetsSameModel_Internal_Sheet1.png">


#### Exclude a column

<!-- snippet: IncludeExcludeOne -->
<a id='snippet-IncludeExcludeOne'></a>
```cs
List<Target> data = [
    new("Alice", 30, "alice@test.com"),
    new("Bob", 25, "bob@test.com")
];
var builder = new BookBuilder();
var sheet = builder.AddSheet(data);
sheet.Exclude(_ => _.Age);
```
<sup><a href='/src/Excelsior.Tests/IncludeTests.cs#L34-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeExcludeOne' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/IncludeTests.ExcludeOne_Sheet1.png">


#### Exclude via Column configuration

<!-- snippet: IncludeExcludeOneViaColumn -->
<a id='snippet-IncludeExcludeOneViaColumn'></a>
```cs
List<Target> data = [
    new("Alice", 30, "alice@test.com"),
    new("Bob", 25, "bob@test.com")
];
var builder = new BookBuilder();
var sheet = builder.AddSheet(data);
sheet.Column(
    _ => _.Age,
    _ => _.Include = false);
```
<sup><a href='/src/Excelsior.Tests/IncludeTests.cs#L53-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeExcludeOneViaColumn' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/IncludeTests.ExcludeOneViaColumn_Sheet1.png">


#### ColumnAttribute

```
public class Employee
{
    [Column(Include = false)]
    public required string Name { get; init; }
```


### Splitting

`SplitAttribute` can be used push properties up.

<!-- snippet: ComplexTypeWithSplitter -->
<a id='snippet-ComplexTypeWithSplitter'></a>
```cs
public record Person(
    string Name,
    [Split] Address Address);

public record Address(int StreetNumber, string Street, string City, State State, ushort PostCode);
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithSplitter.cs#L10-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeWithSplitter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/ComplexTypeWithSplitter.Test_Sheet1.png">


#### UseHierachyForName

`SplitAttribute.UseHierachyForName` can be used to prefix members with the parent property name.

<!-- snippet: ComplexTypeWithSplitterUseHierachyForName -->
<a id='snippet-ComplexTypeWithSplitterUseHierachyForName'></a>
```cs
public record Person(
    string Name,
    [Split(UseHierachyForName = true)]
    Address Address);

public record Address(int Number, string Street, string City, State State, ushort PostCode);
```
<sup><a href='/src/Excelsior.Tests/ComplexTypeWithSplitterUseHierachyForName.cs#L10-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-ComplexTypeWithSplitterUseHierachyForName' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Result

<img src="/src/Excelsior.Tests/ComplexTypeWithSplitterUseHierachyForName.Test_Sheet1.png">


### Source Generated Extensions

A source generator is included that generates typed extension methods for `ISheetBuilder`, providing a more concise API.


#### Model

Add `[SheetModel]` to the model class:

<!-- snippet: SourceGeneratedModel -->
<a id='snippet-SourceGeneratedModel'></a>
```cs
[SheetModel]
public class GeneratedTestModel
{
    public required string Name;
    public required int Age;
}
```
<sup><a href='/src/Excelsior.Tests/SourceGeneratorIntegrationTests.cs#L107-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-SourceGeneratedModel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This generates typed extension methods for each property, such as `NameColumn`, `NameOrder`, `AgeWidth`, etc.


#### Usage

Instead of:

```cs
sheet.Column(_ => _.Name, _ => _.Heading = "Full Name");
sheet.Order(_ => _.Age, 1);
```

Use the generated methods:

<!-- snippet: SourceGeneratedUsage -->
<a id='snippet-SourceGeneratedUsage'></a>
```cs
var builder = new BookBuilder();

List<GeneratedTestModel> data =
[
    new()
    {
        Name = "Alice",
        Age = 30
    },
    new()
    {
        Name = "Bob",
        Age = 25
    },
];

var sheet = builder.AddSheet(data);
sheet.NameColumn(_ => _.Heading = "Full Name");
sheet.AgeOrder(1);
sheet.NameOrder(2);
sheet.AgeWidth(15);
```
<sup><a href='/src/Excelsior.Tests/SourceGeneratorIntegrationTests.cs#L7-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-SourceGeneratedUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Generated methods per property

For each public property, the following extension methods are generated:

 * `{Property}Column` — configure the column (heading, order, width, style, etc.)
 * `{Property}HeadingText` — set the heading text
 * `{Property}Order` — set the column order
 * `{Property}Width` — set the column width
 * `{Property}HeadingStyle` — set the heading style
 * `{Property}CellStyle` — set the cell style
 * `{Property}Format` — set the format string
 * `{Property}NullDisplay` — set the null display text
 * `{Property}IsHtml` — mark the column as HTML
 * `{Property}Render` — set a custom render function
 * `{Property}Filter` — enable auto-filter for the column
 * `{Property}Include` — include or exclude the column from the output
 * `{Property}Exclude` — exclude the column from the output

Properties with `[Ignore]` are skipped. Properties with `[Split]` (or types with `[Split]`) are recursed into, generating methods for the nested properties.


## Word Tables

`WordTableBuilder<TModel>` renders model data into a Word `<w:tbl>` element that can be appended to an existing Word document. It reuses the same property discovery, column ordering, and per-column configuration as `BookBuilder`.

<!-- snippet: WordTableUsage -->
<a id='snippet-WordTableUsage'></a>
```cs
var builder = new WordTableBuilder<Employee>(employees);

using var stream = new MemoryStream();
using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
{
    var mainPart = doc.AddMainDocumentPart();
    mainPart.Document = new(new Body());

    var table = builder.Build(mainPart);
    var body = mainPart.Document.Body!;
    body.Append(table);
```
<sup><a href='/src/Excelsior.Tests/Word/WordTableBuilderTests.cs#L13-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-WordTableUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Column configuration (headings, ordering, render, etc.) works the same as with `BookBuilder`:

```cs
var builder = new WordTableBuilder<Employee>(employees)
    .Column(
        _ => _.Name,
        _ => _.Heading = "Person");
```

When a `MainDocumentPart` is passed to `Build()`, `Link`-typed properties produce real `<w:hyperlink>` elements. When omitted, links fall back to their display text.


### Heading styling

The `WordTableBuilder<TModel>` constructor accepts an optional table-level `headingStyle` callback that styles every header cell. It mirrors `BookBuilder.HeadingStyle` and is translated at build time:

- `CellStyle.BackgroundColor` → cell shading (`<w:shd>`).
- `CellFont.Bold` / `Underline` / `Color` / `Size` / `Name` → run properties.
- `CellAlignment.Horizontal` → paragraph justification (defaults to left).
- `CellAlignment.Vertical` → cell vertical alignment.

The `CellStyle` is preseeded with `Font.Bold = true` and horizontal alignment `Left`, matching the default header look. Callers layer on additions, or opt out by setting `Font.Bold = false`:

<!-- snippet: WordTableHeadingStyle -->
<a id='snippet-WordTableHeadingStyle'></a>
```cs
var builder = new WordTableBuilder<Employee>(
    SampleData.Employees(),
    _ =>
    {
        _.BackgroundColor = "4472C4";
        _.Font.Color = "FFFFFF";
        _.Font.Name = "Arial";
        _.Font.Size = 12;
        _.Font.Underline = true;
    });
```
<sup><a href='/src/Excelsior.Tests/Word/WordTableBuilderTests.cs#L210-L223' title='Snippet source file'>snippet source</a> | <a href='#snippet-WordTableHeadingStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A per-column `HeadingStyle` on `ColumnConfig` composes on top of the table-level style, so individual headers can override or extend the shared look:

<!-- snippet: WordTableColumnHeadingStyle -->
<a id='snippet-WordTableColumnHeadingStyle'></a>
```cs
var builder = new WordTableBuilder<Employee>(
        SampleData.Employees(),
        _ => _.BackgroundColor = "000000")
    .Column(
        _ => _.Name,
        _ => _.HeadingStyle = cell => cell.BackgroundColor = "FF0000");
```
<sup><a href='/src/Excelsior.Tests/Word/WordTableBuilderTests.cs#L231-L240' title='Snippet source file'>snippet source</a> | <a href='#snippet-WordTableColumnHeadingStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Colors accept a leading `#` (e.g. `"#4472C4"`) and it will be stripped before being written to OpenXml.


### Body cell styling

The constructor also accepts an optional `bodyStyle` callback that styles every data cell, mirroring `headingStyle`:

<!-- snippet: WordTableBodyStyle -->
<a id='snippet-WordTableBodyStyle'></a>
```cs
var builder = new WordTableBuilder<Employee>(
    SampleData.Employees(),
    bodyStyle: _ =>
    {
        _.Font.Size = 9;
        _.Font.Name = "Arial";
    });
```
<sup><a href='/src/Excelsior.Tests/Word/WordTableBuilderTests.cs#L597-L607' title='Snippet source file'>snippet source</a> | <a href='#snippet-WordTableBodyStyle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A per-column `CellStyle` on `ColumnConfig` composes on top, and — because it receives the model and value — can style cells conditionally (e.g. bold a salary over a threshold, shade a status cell). Font and alignment are written to the cell's runs/paragraph; background and vertical alignment to the cell properties.

Body styling is applied to plain-text cells. `IsHtml` and `Link` cells own their own run formatting, so they only pick up cell-level properties (background, vertical alignment), not the body font.

When neither `bodyStyle` nor a column `CellStyle` is configured, data cells stay lean — a bare run with no run or paragraph properties — so cell text follows the host document's styles (matching a hand-inserted Word table).


### Named paragraph styles

To drive cell appearance from named paragraph styles already defined in the host document (e.g. a branded template), use `HeadingParagraphStyle` and `BodyParagraphStyle`:

<!-- snippet: WordTableParagraphStyles -->
<a id='snippet-WordTableParagraphStyles'></a>
```cs
var builder = new WordTableBuilder<Employee>(SampleData.Employees())
    .HeadingParagraphStyle("TBLHeading")
    .BodyParagraphStyle("TBLText");
```
<sup><a href='/src/Excelsior.Tests/Word/WordTableBuilderTests.cs#L698-L704' title='Snippet source file'>snippet source</a> | <a href='#snippet-WordTableParagraphStyles' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Unlike the run-level `headingStyle`/`bodyStyle` callbacks, a paragraph style is applied to **every** cell paragraph — including `IsHtml` and `Link` cells — so the style's font, size, and spacing reach all content. The referenced style ids must exist in the host document's styles part. This is the cleanest way to brand an Excelsior table from a template: define the look once as `TBLHeading`/`TBLText` (or any names) and point the table at them.


### Style inheritance

Tables render at 100% page width (`<w:tblW w:type="pct" w:w="5000"/>`) and reference Word's built-in `TableGrid` style. When `Build(mainPart)` is called, the renderer adds the two style definitions Word itself ships when a table is inserted via the ribbon — `TableNormal` (the default table style that supplies 108dxa left/right cell padding) and `TableGrid` (single-line 4pt borders, declared `basedOn="TableNormal"` so the padding is inherited) — into the host's `StyleDefinitionsPart` if they aren't already there. Word lazy-writes both into a doc's `styles.xml` only once a table that uses them exists, so programmatically-built hosts and templates authored without tables won't have them on disk. The insertion is idempotent: building multiple tables against the same host adds each style once, and a host that already declares either style (typical of templates authored with tables present) is left untouched so customizations survive.

The supported way to rebrand Excelsior tables is to **customize `TableGrid` in the host template**. Any borders, cell margins, or `<w:tblStylePr w:type="firstRow">` conditional formatting declared on `TableGrid` flow straight through the `tblStyle` reference. The `firstRow=1` bit on `<w:tblLook>` is always emitted so a customized `TableGrid` can paint the header row separately. Per-column `HeadingStyle` and the table-level `headingStyle` callback layer on top.

The standalone `Build()` overload (no `MainDocumentPart`) has no styles part to add `TableGrid` to, so it emits inline borders and cell margins directly on the table instead. The 100% width `tblW` is still emitted so the table fills whatever container it's appended to.

To customize `TableGrid` on the host document in Word itself, see Microsoft's guidance:

- [Change the look of a table](https://support.microsoft.com/en-us/office/change-the-look-of-a-table-a18cbaa8-e681-455f-a99f-a2378fe5ff06) — picking, modifying, and setting a table style as default.
- [Customize or create new styles](https://support.microsoft.com/en-us/office/customize-or-create-new-styles-d38d6e47-f6fc-48eb-a607-1eb120dec563) — covers the Modify Style dialog, including the "New documents based on this template" option that's needed to make a table style stick as the default for the document.
- [Apply a table style](https://support.microsoft.com/en-us/office/video-apply-a-table-style-f1b798e7-fa25-496c-a434-0c2a15bed09f) — short walkthrough of the Table Design ribbon.
- [Set or change table properties](https://support.microsoft.com/en-us/office/set-or-change-table-properties-3237de89-b287-4379-8e0c-86d94873b2e0) — borders, cell margins, alignment.


### Limitations

Formula columns are not supported in Word tables. Word has no equivalent of Excel cell formulas, so configuring a `Formula` on a column used with `WordTableBuilder` will throw when `Build()` is called. Use `Render` or a computed property instead.


## Icon

[Grim Fandango](https://github.com/PapirusDevelopmentTeam/papirus-icon-theme/blob/master/Papirus/64x64/apps/grim-fandango-remastered.svg) from [Papirus Icons](https://github.com/PapirusDevelopmentTeam/papirus-icon-theme).

The [Excelsior Line](https://grim-fandango.fandom.com/wiki/Excelsior_Line) is a travel package sold by Manuel Calavera in the Lucas Arts game "Grim Fandango". The package consists of nothing more than a walking stick with a compass in the handle.
