class ColumnReadState
{
    public required string Name { get; init; }
    public required string Heading { get; set; }
    public required Type Type { get; init; }
    public Func<Cell, object?>? Convert { get; set; }
    public string? NullDisplay { get; set; }
}