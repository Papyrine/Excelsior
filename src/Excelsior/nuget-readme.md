# Excelsior

Excelsior is an Excel spreadsheet generation library with a distinctive data-driven approach. Uses [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) for spreadsheet creation and [OpenXmlHtml](https://github.com/Papyrine/OpenXmlHtml) for HTML cell rendering.

## Usage

### Model

Given an input class:

```cs
using Excelsior;

public class Employee
{
    [Column(Heading = "Employee ID", Order = 1)]
    public required int Id { get; init; }

    [Column(Heading = "Full Name", Order = 2)]
    public required string Name { get; init; }

    [Column(Heading = "Email Address", Order = 3)]
    public required string Email { get; init; }

    [Column(Heading = "Hire Date", Order = 4)]
    public Date? HireDate { get; init; }

    [Column(Heading = "Annual Salary", Order = 5)]
    public int Salary { get; init; }

    public bool IsActive { get; init; }

    public EmployeeStatus Status { get; init; }
}
```

`[ColumnAttribute]` is optional. If omitted:

 * Order is based on the order of the properties defined in the class.
 * Heading text is based on the property names that is camel case split.

### Builder

`BookBuilder` is the root entry point. Once instantiated, the data for multiple sheets can be added.

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

## Documentation

See the [readme](https://github.com/Papyrine/Excelsior#readme) for full documentation.
