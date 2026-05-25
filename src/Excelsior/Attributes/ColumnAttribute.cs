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
