sealed record ColumnReadInfo(
    string Name,
    string Heading,
    Type Type,
    Func<Cell, object?>? Convert,
    string? NullDisplay = null);