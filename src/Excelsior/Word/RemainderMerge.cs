namespace Excelsior;

/// <summary>
/// How a Word table draws the rows that merge their trailing cells - see
/// <see cref="WordTableBuilder{TModel}.MergeRemainder{TProperty}"/>.
/// </summary>
class RemainderMerge<TModel>(
    Func<TModel, bool> when,
    string afterColumn,
    Func<TModel, string> content,
    bool isHtml)
{
    public Func<TModel, bool> When { get; } = when;
    public string AfterColumn { get; } = afterColumn;
    public Func<TModel, string> Content { get; } = content;
    public bool IsHtml { get; } = isHtml;

    /// <summary>
    /// How many leading columns a merged row keeps, resolved against the final column order.
    /// </summary>
    public int ResolveKeep(List<ColumnConfig<TModel>> columns)
    {
        var index = columns.FindIndex(_ => _.Name == AfterColumn);
        if (index < 0)
        {
            throw new($"MergeRemainder: '{AfterColumn}' is not a rendered column.");
        }

        if (index == columns.Count - 1)
        {
            throw new($"MergeRemainder: '{AfterColumn}' is the last column, so there is nothing after it to merge.");
        }

        return index + 1;
    }
}
