namespace Excelsior;

/// <summary>
/// A single merged row rendered above the header, used to surface arbitrary instructions to
/// whoever edits the sheet. Either <see cref="Text"/> (with an optional <see cref="Style"/>)
/// or <see cref="Render"/> (raw cell access for rich text) is set, never both.
/// </summary>
class Banner
{
    public string? Text { get; init; }
    public Action<CellStyle>? Style { get; init; }
    public Action<Cell>? Render { get; init; }
    public bool Freeze { get; init; } = true;

    /// <summary>
    /// Caps the auto-computed banner row height (in points). The banner row grows to fit its
    /// wrapped text — merged cells do not auto-size in Excel — up to this ceiling, beyond which
    /// the text is clipped. <c>null</c> leaves only Excel's hard maximum (409) in effect.
    /// </summary>
    public int? MaxHeight { get; init; }
}
