namespace Excelsior;

/// <summary>
/// How a sheet prints: the text in the page header and footer, and the paper it prints on. Every
/// member is optional, and one left <c>null</c> leaves Excel's own default in place. A sheet given a
/// print setup also has its print area limited to its own columns — see <see cref="BookBuilder.Print"/>.
/// </summary>
public class PrintSetup
{
    /// <summary>
    /// Text centred in the header of every printed page. Written as it is: an <c>&amp;</c> is
    /// escaped rather than read as one of Excel's header codes.
    /// </summary>
    public string? Header { get; init; }

    /// <summary>
    /// Text centred in the footer of every printed page, escaped as <see cref="Header"/> is.
    /// </summary>
    public string? Footer { get; init; }

    public PrintOrientation? Orientation { get; init; }

    public PrintPaperSize? PaperSize { get; init; }
}

public enum PrintOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// A paper size, valued as the code a worksheet's page setup stores for it.
/// </summary>
public enum PrintPaperSize
{
    Letter = 1,
    Legal = 5,
    A3 = 8,
    A4 = 9
}
