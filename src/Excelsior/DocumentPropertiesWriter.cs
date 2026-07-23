using DocumentFormat.OpenXml.VariantTypes;
using CustomProps = DocumentFormat.OpenXml.CustomProperties;
using ExtendedProps = DocumentFormat.OpenXml.ExtendedProperties;

/// <summary>
/// Applies a <see cref="BookProperties"/> to a <see cref="SpreadsheetDocument"/>:
/// core properties go to <c>docProps/core.xml</c> via <see cref="OpenXmlPackage.PackageProperties"/>,
/// company/manager to the extended part (<c>docProps/app.xml</c>), and the user-defined entries
/// to the custom part (<c>docProps/custom.xml</c>). The namespace aliasing is isolated here because
/// the <c>CustomProperties</c>, <c>ExtendedProperties</c>, and Excelsior all expose a type named
/// <c>Properties</c>.
/// </summary>
static class DocumentPropertiesWriter
{
    // The fixed FMTID required on every custom document property by the OOXML spec.
    const string customFormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

    public static void Apply(SpreadsheetDocument document, BookProperties properties)
    {
        ApplyCore(document, properties);
        ApplyExtended(document, properties);
        ApplyCustom(document, properties);
    }

    static XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    static XNamespace dc = "http://purl.org/dc/elements/1.1/";

    // Core properties are written as an explicit CoreFilePropertiesPart rather than through
    // OpenXmlPackage.PackageProperties: the latter is backed by the OPC package's intrinsic
    // core-property store, which SpreadsheetDocument.Clone (used by every ToStream/ToFile/ToBytes
    // path) does not copy. A real part is enumerated and cloned like any other.
    static void ApplyCore(SpreadsheetDocument document, BookProperties properties)
    {
        if (properties is
            {
                Title: null,
                Author: null,
                Subject: null,
                Keywords: null,
                Comments: null,
                Category: null,
                Status: null,
                LastModifiedBy: null
            })
        {
            return;
        }

        var root = new XElement(
            cp + "coreProperties",
            new XAttribute(XNamespace.Xmlns + "cp", cp.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName));

        void Add(XName name, string? value)
        {
            if (value != null)
            {
                root.Add(new XElement(name, value));
            }
        }

        Add(dc + "title", properties.Title);
        Add(dc + "subject", properties.Subject);
        Add(dc + "creator", properties.Author);
        Add(cp + "keywords", properties.Keywords);
        Add(dc + "description", properties.Comments);
        Add(cp + "lastModifiedBy", properties.LastModifiedBy);
        Add(cp + "category", properties.Category);
        Add(cp + "contentStatus", properties.Status);

        var part = document.AddCoreFilePropertiesPart();
        using var stream = part.GetStream(FileMode.Create);
        new XDocument(root).Save(stream);
    }

    static void ApplyExtended(SpreadsheetDocument document, BookProperties properties)
    {
        if (properties.Company == null &&
            properties.Manager == null)
        {
            return;
        }

        var part = document.AddExtendedFilePropertiesPart();
        var extended = new ExtendedProps.Properties();
        if (properties.Company != null)
        {
            extended.Company = new(properties.Company);
        }

        if (properties.Manager != null)
        {
            extended.Manager = new(properties.Manager);
        }

        part.Properties = extended;
    }

    static void ApplyCustom(SpreadsheetDocument document, BookProperties properties)
    {
        if (properties.Custom.Count == 0)
        {
            return;
        }

        var part = document.AddCustomFilePropertiesPart();
        var custom = new CustomProps.Properties();
        // PropertyId is 1-based and 1 is reserved, so user-defined properties start at 2
        // and increment by one each.
        var id = 2;
        foreach (var (name, value) in properties.Custom)
        {
            var property = new CustomProps.CustomDocumentProperty
            {
                FormatId = customFormatId,
                PropertyId = id,
                Name = name
            };
            property.AppendChild(ToVariant(name, value));
            custom.AppendChild(property);
            id++;
        }

        part.Properties = custom;
    }

    static OpenXmlElement ToVariant(string name, object? value) =>
        value switch
        {
            null => new VTLPWSTR(string.Empty),
            string s => new VTLPWSTR(s),
            bool b => new VTBool(b ? "true" : "false"),
            int i => new VTInt32(i.ToString(CultureInfo.InvariantCulture)),
            long l => new VTInt64(l.ToString(CultureInfo.InvariantCulture)),
            short s => new VTInt32(((int)s).ToString(CultureInfo.InvariantCulture)),
            byte b => new VTInt32(((int)b).ToString(CultureInfo.InvariantCulture)),
            double d => new VTDouble(d.ToString(CultureInfo.InvariantCulture)),
            float f => new VTDouble(((double)f).ToString(CultureInfo.InvariantCulture)),
            decimal m => new VTDouble(m.ToString(CultureInfo.InvariantCulture)),
            DateTime time => new VTFileTime(time.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)),
            Date date => new VTFileTime($"{date:yyyy-MM-dd}T00:00:00Z"),
            // Written as text rather than the clsid variant: Excel's Advanced Properties dialog
            // only surfaces Text/Date/Number/Yes-No, so a string shows as an editable property
            // whereas clsid would not appear at all.
            Guid guid => new VTLPWSTR(guid.ToString()),
            // Deliberately strict rather than falling back to value.ToString(): coercing an
            // unsupported type would write something like "System.Int32[]" into the property and
            // hide the caller's mistake. Make them convert to a supported type explicitly.
            _ => throw new ArgumentException(
                $"Custom document property '{name}' has unsupported value type '{value.GetType()}'. Supported types are string, bool, integral and floating-point numbers, DateTime, DateOnly and Guid. Convert the value to one of these before adding it.")
        };
}
