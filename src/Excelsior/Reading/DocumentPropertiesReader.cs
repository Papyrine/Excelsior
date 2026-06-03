using CustomProps = DocumentFormat.OpenXml.CustomProperties;

/// <summary>
/// Reads the user-defined entries from a workbook's custom properties part
/// (<c>docProps/custom.xml</c>) — the inverse of the custom-property output written by
/// <see cref="DocumentPropertiesWriter"/>. Each value is returned as its raw variant text;
/// typed conversion happens on demand in <see cref="BookReader.GetCustomProperty{T}"/>.
/// </summary>
static class DocumentPropertiesReader
{
    public static Dictionary<string, string?> ReadCustom(SpreadsheetDocument document)
    {
        // Custom property names are matched case-insensitively, mirroring how Excel treats
        // them and how the reader resolves sheet names.
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (document.CustomFilePropertiesPart?.Properties is not { } properties)
        {
            return result;
        }

        foreach (var property in properties.Elements<CustomProps.CustomDocumentProperty>())
        {
            if (property.Name?.Value is not { } name)
            {
                continue;
            }

            // Every variant the writer emits (VTLPWSTR/VTBool/VTInt32/VTInt64/VTDouble/VTFileTime)
            // stores its value as element text, so InnerText is the inverse of ToVariant. Last
            // write wins if a malformed file repeats a name.
            result[name] = property.FirstChild?.InnerText;
        }

        return result;
    }
}
