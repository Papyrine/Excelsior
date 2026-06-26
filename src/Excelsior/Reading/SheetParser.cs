static class SheetParser
{
    public record SheetMetadata(Dictionary<int, string> ColumnMap, bool HasBanner);

    public static Dictionary<string, SheetMetadata> ReadMetadata(WorkbookPart workbookPart, out string? userMetadataJson)
    {
        userMetadataJson = null;
        var result = new Dictionary<string, SheetMetadata>(StringComparer.Ordinal);
        foreach (var part in workbookPart.GetPartsOfType<CustomXmlPart>())
        {
            using var stream = part.GetStream();
            var doc = XDocument.Load(stream);


            var root = doc.Root;
            if (root == null)
            {
                continue;
            }

            if (root.Name.NamespaceName == BookBuilder.UserMetadataNamespace)
            {
                userMetadataJson = root.Value;
                continue;
            }

            if (root.Name.NamespaceName != BookBuilder.MetadataNamespace)
            {
                continue;
            }

            XNamespace ns = BookBuilder.MetadataNamespace;
            foreach (var sheet in root.Elements(ns + "sheet"))
            {
                var sheetName = sheet.Attribute("name")?.Value;
                if (sheetName == null)
                {
                    continue;
                }

                var map = new Dictionary<int, string>();
                foreach (var column in sheet.Elements(ns + "column"))
                {
                    var indexCalue = column.Attribute("index")?.Value;
                    if (!int.TryParse(indexCalue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                    {
                        continue;
                    }

                    var name = column.Attribute("property")?.Value;
                    if (name == null)
                    {
                        continue;
                    }

                    map[index] = name;
                }

                // Attribute is omitted by writers that emit no banner; only the literal "true"
                // counts as set, so a malformed value cannot trick the reader into silently
                // swallowing a real data row.
                var hasBanner = string.Equals(sheet.Attribute("banner")?.Value, "true", StringComparison.Ordinal);
                result[sheetName] = new(map, hasBanner);
            }
        }

        return result;
    }

    public static void Parse(
        IReaderSheet sheet,
        string resolvedSheetName,
        WorksheetPart worksheetPart,
        string?[]? sharedStrings,
        Dictionary<int, string>? metadataColumnMap,
        bool hasBanner,
        List<ReadError> errors)
    {
        sheet.Reset();

        var columns = sheet.Columns();
        var byName = new Dictionary<string, ColumnReadInfo>(columns.Count, StringComparer.Ordinal);
        var byHeading = new Dictionary<string, ColumnReadInfo>(columns.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var info in columns)
        {
            byName[info.Name] = info;
            byHeading[info.Heading] = info;
        }

        // Streaming reader: avoids materializing the entire SheetData DOM.
        // Each Row is loaded on demand and becomes Gen0 garbage after processing.
        using var reader = OpenXmlReader.Create(worksheetPart);

        var headerSeen = false;
        var bannerSkipped = !hasBanner;
        Dictionary<int, int>? fileIndexToSlot = null;
        Cell?[]? cellsBySlot = null;
        Action<int, string>? onError = null;
        Row? currentRow = null;

        while (reader.Read())
        {
            if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
            {
                continue;
            }

            var row = (Row)reader.LoadCurrentElement()!;

            // The banner row sits above the header — the writer records its presence in the
            // metadata XML so the reader can skip past it. Without this, the merged banner cell
            // would be misread as the header row and every declared column would surface as
            // "not found".
            if (!bannerSkipped)
            {
                bannerSkipped = true;
                continue;
            }

            if (!headerSeen)
            {
                headerSeen = true;
                var columnByIndex = ResolveColumnByIndex(row, sharedStrings, metadataColumnMap, byName, byHeading, out var unmatchedHeaders, out var duplicateMatches);

                var resolvedNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var info in columnByIndex.Values)
                {
                    resolvedNames.Add(info.Name);
                }

                var beforeCount = errors.Count;
                foreach (var declared in columns)
                {
                    if (resolvedNames.Contains(declared.Name))
                    {
                        continue;
                    }

                    errors.Add(new(
                        resolvedSheetName,
                        RowIndex: 0,
                        declared.Name,
                        CellReference: "",
                        $"Column '{declared.Name}' (heading '{declared.Heading}') was not found in the sheet header row."));
                }

                foreach (var (cellRef, headerText) in unmatchedHeaders)
                {
                    errors.Add(new(
                        resolvedSheetName,
                        RowIndex: (int)(row.RowIndex?.Value ?? 0),
                        ColumnName: "",
                        CellReference: cellRef,
                        $"Unrecognized header '{headerText}' — no declared column matches this heading."));
                }

                foreach (var (column, firstRef, secondRef, source) in duplicateMatches)
                {
                    var detail = source == DuplicateSource.Metadata
                        ? $"metadata maps multiple header cells ({firstRef} and {secondRef}) to property '{column.Name}'"
                        : $"header cells {firstRef} and {secondRef} both resolve to column '{column.Name}' (heading '{column.Heading}')";
                    errors.Add(new(
                        resolvedSheetName,
                        RowIndex: (int)(row.RowIndex?.Value ?? 0),
                        column.Name,
                        CellReference: secondRef,
                        $"Duplicate column match: {detail}."));
                }

                if (errors.Count > beforeCount)
                {
                    return;
                }

                var slotByName = new Dictionary<string, int>(columns.Count, StringComparer.Ordinal);
                for (var i = 0; i < columns.Count; i++)
                {
                    slotByName[columns[i].Name] = i;
                }

                fileIndexToSlot = new(columnByIndex.Count);
                // Inverse map (slot -> file column index). The file's physical column order can
                // differ from the model's declared order, so an error's cell reference must use the
                // column's real position in the file, not its declared ordinal. Every slot is
                // guaranteed a file index here: the loop above aborts if any declared column is
                // unmatched, so the int[] zero-default is never observed.
                var slotToFileIndex = new int[columns.Count];
                foreach (var (fileIndex, column) in columnByIndex)
                {
                    var slot = slotByName[column.Name];
                    fileIndexToSlot[fileIndex] = slot;
                    slotToFileIndex[slot] = fileIndex;
                }

                cellsBySlot = new Cell?[columns.Count];

                // Closure built once; `currentRow` is reassigned per row but the
                // closure reads it lazily so no per-row delegate allocation.
                var slotColumns = columns;
                onError = (slot, message) =>
                {
                    var col = slotColumns[slot];
                    var rowIndex = currentRow?.RowIndex?.Value ?? 0;
                    var cellRef = $"{SheetContext.GetColumnLetter(slotToFileIndex[slot])}{rowIndex}";
                    errors.Add(new(resolvedSheetName, (int)rowIndex, col.Name, cellRef, message));
                };

                continue;
            }

            currentRow = row;
            Array.Clear(cellsBySlot!);

            foreach (var cell in row.Elements<Cell>())
            {
                var index = ParseColumnIndex(cell.CellReference?.Value);
                if (index < 0)
                {
                    continue;
                }

                if (fileIndexToSlot!.TryGetValue(index, out var slot))
                {
                    cellsBySlot![slot] = cell;
                }
            }

            sheet.ReceiveRow(cellsBySlot!, sharedStrings, onError!);
        }
    }

    enum DuplicateSource
    {
        Metadata,
        Heading
    }

    static Dictionary<int, ColumnReadInfo> ResolveColumnByIndex(
        Row headerRow,
        string?[]? sharedStrings,
        Dictionary<int, string>? metadataColumnMap,
        Dictionary<string, ColumnReadInfo> byName,
        Dictionary<string, ColumnReadInfo> byHeading,
        out List<(string CellReference, string HeaderText)> unmatchedHeaders,
        out List<(ColumnReadInfo Column, string FirstCellRef, string SecondCellRef, DuplicateSource Source)> duplicateMatches)
    {
        var result = new Dictionary<int, ColumnReadInfo>();
        unmatchedHeaders = [];
        duplicateMatches = [];
        var claimedBy = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var cell in headerRow.Elements<Cell>())
        {
            var cellRef = cell.CellReference?.Value ?? "";
            var index = ParseColumnIndex(cellRef);
            if (index < 0)
            {
                continue;
            }

            // Prefer the metadata-XML mapping (round-trip case). Indices in the
            // metadata are 1-based to match the column letter; cell-ref indices
            // here are 0-based, so add one when probing.
            if (metadataColumnMap != null &&
                metadataColumnMap.TryGetValue(index + 1, out var propertyName) &&
                byName.TryGetValue(propertyName, out var byMeta))
            {
                if (claimedBy.TryGetValue(byMeta.Name, out var firstRefMeta))
                {
                    duplicateMatches.Add((byMeta, firstRefMeta, cellRef, DuplicateSource.Metadata));
                    continue;
                }

                claimedBy[byMeta.Name] = cellRef;
                result[index] = byMeta;
                continue;
            }

            var headerText = CellConverter.ReadRaw(cell, sharedStrings);
            if (string.IsNullOrWhiteSpace(headerText))
            {
                continue;
            }

            if (byHeading.TryGetValue(headerText, out var byHead))
            {
                if (claimedBy.TryGetValue(byHead.Name, out var firstRefHead))
                {
                    duplicateMatches.Add((byHead, firstRefHead, cellRef, DuplicateSource.Heading));
                    continue;
                }

                claimedBy[byHead.Name] = cellRef;
                result[index] = byHead;
                continue;
            }

            // Final fallback: the column name itself, in case the heading was
            // overridden but the writer used DisplayName everywhere.
            if (byName.TryGetValue(headerText, out var byPlainName))
            {
                if (claimedBy.TryGetValue(byPlainName.Name, out var firstRefPlain))
                {
                    duplicateMatches.Add((byPlainName, firstRefPlain, cellRef, DuplicateSource.Heading));
                    continue;
                }

                claimedBy[byPlainName.Name] = cellRef;
                result[index] = byPlainName;
                continue;
            }

            unmatchedHeaders.Add((cellRef, headerText));
        }

        return result;
    }

    static int ParseColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
        {
            return -1;
        }

        var index = 0;
        var length = 0;
        foreach (var c in cellReference)
        {
            if (c is >= 'A' and <= 'Z')
            {
                index = index * 26 + (c - 'A') + 1;
                length++;
            }
            else if (c is >= 'a' and <= 'z')
            {
                index = index * 26 + (c - 'a') + 1;
                length++;
            }
            else
            {
                break;
            }
        }

        return length == 0 ? -1 : index - 1;
    }
}
