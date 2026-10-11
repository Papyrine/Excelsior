namespace Excelsior;

public class ColumnReadConfig<TProperty>
{
    public string? Heading { get; set; }

    /// <summary>
    /// The text the writer puts in this column for a null. A cell holding exactly that reads
    /// back as null. Defaults to the <c>NullDisplay</c> of the property's
    /// <see cref="ColumnAttribute"/>; set it here where the writer was given one fluently.
    /// </summary>
    public string? NullDisplay { get; set; }

    /// <summary>
    /// Custom conversion delegate. Receives the underlying OpenXml <see cref="Cell"/>
    /// and returns the value to assign to the property. When set, this takes
    /// precedence over the built-in cell parsing.
    /// </summary>
    public Func<Cell, TProperty>? Convert { get; set; }
}
