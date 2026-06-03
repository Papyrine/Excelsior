namespace Excelsior;

/// <summary>
/// Standard and user-defined workbook document properties — the values Excel surfaces in
/// the File &gt; Info pane and the Advanced Properties dialog. Apply via
/// <see cref="BookBuilder.SetProperties"/>. Every member is optional; only the values that
/// are set get written, so a property left at its default leaves that part of the workbook
/// untouched.
/// </summary>
public class DocumentProperties
{
    /// <summary>Maps to the core property <c>Title</c>.</summary>
    public string? Title { get; init; }

    /// <summary>The document author. Maps to the core property <c>Creator</c> (<c>dc:creator</c>).</summary>
    public string? Author { get; init; }

    /// <summary>Maps to the core property <c>Subject</c>.</summary>
    public string? Subject { get; init; }

    /// <summary>Comma- or space-separated tags. Maps to the core property <c>Keywords</c>.</summary>
    public string? Keywords { get; init; }

    /// <summary>Free-text comments. Maps to the core property <c>Description</c> (<c>dc:description</c>).</summary>
    public string? Comments { get; init; }

    /// <summary>Maps to the core property <c>Category</c>.</summary>
    public string? Category { get; init; }

    /// <summary>Content/approval status (e.g. "Draft", "Final"). Maps to the core property <c>ContentStatus</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Maps to the core property <c>LastModifiedBy</c>.</summary>
    public string? LastModifiedBy { get; init; }

    /// <summary>Company name. Written to the extended (app) properties part.</summary>
    public string? Company { get; init; }

    /// <summary>Manager name. Written to the extended (app) properties part.</summary>
    public string? Manager { get; init; }

    /// <summary>
    /// User-defined properties shown on the "Custom" tab of Excel's Advanced Properties dialog.
    /// Supported value types are <see cref="string"/>, <see cref="bool"/>, integral and
    /// floating-point numbers, <see cref="DateTime"/> and <see cref="DateOnly"/>; any other
    /// value type throws an <see cref="ArgumentException"/> when the workbook is built, so
    /// convert it to a supported type first. A <c>null</c> value is written as empty text.
    /// </summary>
    public Dictionary<string, object?> Custom { get; init; } = new();
}
