class Renderer<TModel>(
    string name,
    IAsyncEnumerable<TModel> data,
    List<ColumnConfig<TModel>> columns,
    int? minColumnWidth,
    int? maxColumnWidth,
    int? maxRowHeight,
    BookBuilder bookBuilder,
    int templateRowCount = 0)
{
    const int maxExcelRowHeight = 409;
    const double defaultExcelFontSize = 11;
    const double defaultRowHeight = defaultExcelFontSize + 4;
    const string requiredHighlightColor = "FFFFC7CE";
    // Excel's column upper bound is column XFD (1-indexed = 16,384; 0-indexed = 16,383).
    const int maxExcelColumnIndex = 16383;

    internal bool AutoFilter { get; set; } = true;
    internal bool AutoInputMessages { get; set; } = true;
    internal Banner? Banner { get; set; }

    // Number of rows inserted above the header. A banner occupies one; everything below
    // (header, data, validations, notes, freeze pane) shifts down by this amount.
    int BannerRows => Banner == null ? 0 : 1;

    StyleManager? styleManager;
    Dictionary<Cell, CellStyle> cellStyles = [];
    Dictionary<Cell, int> cellDisplayLengths = [];
    Dictionary<int, double> finalColumnWidths = [];
    Dictionary<int, uint> columnLevelStyles = [];
    Cell? bannerCell;

    internal async Task AddSheet(SpreadsheetDocument book, Cancel cancel)
    {
        ValidateFormulaColumnWidths();
        var sheet = BuildSheet(book);
        CreateBanner(sheet);
        CreateHeadings(sheet);
        FreezeHeader(sheet);
        await PopulateData(sheet, cancel);
        if (bookBuilder.GlobalStyle != null)
        {
            ApplyGlobalStyling(bookBuilder.GlobalStyle);
        }

        var first = -1;
        var last = -1;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            if (column.IsEnumerable || !(column.Filter ?? AutoFilter))
            {
                continue;
            }

            if (first == -1)
            {
                first = i;
            }

            last = i;
        }

        if (first != -1)
        {
            ApplyFilter(sheet, first, last);
        }

        AutoSizeColumns(sheet);
        BuildColumnLevelStyles();
        ResizeRows(sheet);
        ApplyMaxRowHeight(sheet);
        ResizeBannerRow(sheet);
        ApplySheetProtection(sheet);
        MergeBanner(sheet);
        EmitConditionalFormatting(sheet);
        EmitDataValidations(sheet);
        EmitComments(sheet);
        RegisterMetadata();
    }

    void RegisterMetadata()
    {
        var metadata = new List<(int Index, string PropertyName)>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
        {
            metadata.Add((i + 1, columns[i].Name));
        }

        bookBuilder.RegisterSheetMetadata(name, metadata, Banner != null);
    }

    void ApplySheetProtection(SheetContext sheet)
    {
        var options = bookBuilder.Protection;
        if (options == null)
        {
            return;
        }

        var sheetProtection = new SheetProtection
        {
            Sheet = true,
            Password = ProtectionPasswordHasher.Hash(options.Password),
            Objects = options.Objects,
            Scenarios = options.Scenarios,
            FormatCells = options.FormatCells,
            FormatColumns = options.FormatColumns,
            FormatRows = options.FormatRows,
            InsertColumns = options.InsertColumns,
            InsertRows = options.InsertRows,
            InsertHyperlinks = options.InsertHyperlinks,
            DeleteColumns = options.DeleteColumns,
            DeleteRows = options.DeleteRows,
            SelectLockedCells = options.SelectLockedCells,
            SelectUnlockedCells = options.SelectUnlockedCells,
            Sort = options.Sort,
            AutoFilter = options.AutoFilter,
            PivotTables = options.PivotTables
        };
        sheet.Worksheet.InsertAfter(sheetProtection, sheet.SheetData);
    }

    // Renders the merged instruction row above the header. Runs before CreateHeadings so the
    // banner occupies row 1 and the header/data rows stay in ascending order in SheetData.
    void CreateBanner(SheetContext sheet)
    {
        if (Banner == null)
        {
            return;
        }

        var cell = sheet.GetCell(0, 0);
        bannerCell = cell;
        var style = GetStyle(cell);
        style.Alignment.Horizontal = HorizontalAlignmentValues.Left;
        style.Alignment.Vertical = VerticalAlignmentValues.Top;
        style.Alignment.WrapText = true;
        if (bookBuilder.IsProtected)
        {
            // A protected sheet leaves the banner read-only — instructions are not meant to be
            // edited. Data columns opt back in to editable via their (un)Locked handling.
            style.Locked = true;
        }

        if (Banner.Render == null)
        {
            SetCellValue(cell, Banner.Text!);
        }
        else
        {
            Banner.Render(cell);
        }

        Banner.Style?.Invoke(style);
        CommitStyle(cell, style);
    }

    // Spans the banner cell across the entire spreadsheet row (A1:XFD1) rather than only the
    // declared data columns. Excel needs the anchor cell (A1) plus a merge region; the cells
    // it merges over can stay absent. Merging the whole row keeps the banner reading as a
    // single horizontal stripe even when the user adds columns past the declared ones, and it
    // makes the single-column case work too — a 1x1 merge would be invalid, but 1xN where N is
    // the full row width is fine.
    void MergeBanner(SheetContext sheet)
    {
        if (Banner == null)
        {
            return;
        }

        var lastColumn = SheetContext.GetColumnLetter(maxExcelColumnIndex);
        var mergeCells = new MergeCells
        {
            Count = 1
        };
        mergeCells.Append(
            new MergeCell
            {
                Reference = $"A1:{lastColumn}1"
            });

        // mergeCells must sit after sheetData/sheetProtection/autoFilter but before the
        // conditionalFormatting and dataValidations that are appended later.
        var worksheet = sheet.Worksheet;
        OpenXmlElement? anchor = worksheet.GetFirstChild<AutoFilter>();
        anchor ??= worksheet.GetFirstChild<SheetProtection>();
        anchor ??= sheet.SheetData;
        worksheet.InsertAfter(mergeCells, anchor);
    }

    // Merged cells do not auto-size their row in Excel, so a multi-line banner would be clipped
    // at the default single-line height. The banner is merged across the entire row (A1:XFD1),
    // so text never wraps on the cell boundary — only on its explicit newlines. The line count
    // is therefore just `text.Split('\n').Length`; no chars-per-line / word-wrap estimation is
    // required, which is why the previous EstimateBannerLines / CountWrappedLines machinery
    // could be deleted.
    void ResizeBannerRow(SheetContext sheet)
    {
        if (Banner == null ||
            bannerCell == null)
        {
            return;
        }

        var fontSize = BannerFontSize();
        var text = string.Concat(EnumerateCellTexts(bannerCell));
        var lines = text.AsSpan().Count('\n') + 1;
        var height = lines * (fontSize + 4);

        if (Banner.MaxHeight is { } max &&
            height > max)
        {
            height = max;
        }

        if (height > maxExcelRowHeight)
        {
            height = maxExcelRowHeight;
        }

        if (Math.Abs(height - defaultRowHeight) < 0.01)
        {
            // A single default-font line needs no explicit height — Excel's default row height
            // already fits it, so leave the row attribute off.
            return;
        }

        var row = sheet.SheetData
            .Elements<Row>()
            .FirstOrDefault(_ => _.RowIndex?.Value == 1);
        if (row == null)
        {
            return;
        }

        row.Height = height;
        row.CustomHeight = true;
    }

    double BannerFontSize()
    {
        var size = defaultExcelFontSize;
        ProbeFontSize(bookBuilder.GlobalStyle, ref size);
        ProbeFontSize(Banner!.Style, ref size);
        return size;
    }

    void CreateHeadings(SheetContext sheet)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];

            var cell = sheet.GetCell(BannerRows, i);

            SetCellValue(cell, column.Heading);
            var style = GetStyle(cell);
            ApplyHeadingStyling(column, style);
            if (column.Heading.AsSpan().IndexOfAny('\n', '\r') >= 0)
            {
                style.Alignment.WrapText = true;
            }
            CommitStyle(cell, style);
        }
    }

    void ApplyHeadingStyling(ColumnConfig<TModel> column, CellStyle style)
    {
        style.Font.Bold = true;
        bookBuilder.HeadingStyle?.Invoke(style);
        column.HeadingStyle?.Invoke(style);
    }

    void ResizeColumn(SheetContext sheet, int index, ColumnConfig<TModel> columnConfig)
    {
        var resultMinColumnWidth = minColumnWidth ?? bookBuilder.DefaultMinColumnWidth;
        var resultMaxColumnWidth = maxColumnWidth ?? bookBuilder.DefaultMaxColumnWidth;
        int width;
        if (columnConfig.Width == null)
        {
            var doubleWidth = AdjustColumnWidth(sheet, index);
            width = (int)Math.Round(doubleWidth);
            width += 1;

            if (columnConfig.IsEnumerable)
            {
                width += 5;
            }

            var effectiveMin = columnConfig.MinWidth ?? resultMinColumnWidth;
            if (effectiveMin is { } min && width < min)
            {
                width = min;
            }

            if (columnConfig.MaxWidth is { } max && width > max)
            {
                width = max;
            }

            if (width > resultMaxColumnWidth)
            {
                width = resultMaxColumnWidth;
            }
        }
        else
        {
            width = columnConfig.Width.Value;
        }

        finalColumnWidths[index] = width;
    }

    void AutoSizeColumns(SheetContext sheet)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            ResizeColumn(sheet, index, column);
        }
    }

    async Task PopulateData(SheetContext sheet, Cancel cancel)
    {
        var columnIndexesByName = new Dictionary<string, int>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
        {
            columnIndexesByName[columns[i].Name] = i;
        }

        var itemIndex = 0;
        await foreach (var item in data.WithCancellation(cancel))
        {
            var rowIndex = itemIndex + 1 + BannerRows; // +1 to skip heading, plus any banner

            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var column = columns[columnIndex];
                var cell = sheet.GetCell(rowIndex, columnIndex);
                var style = GetStyle(cell);
                style.Alignment.Horizontal = HorizontalAlignmentValues.Left;
                style.Alignment.Vertical = VerticalAlignmentValues.Top;
                style.Alignment.WrapText = true;
                if (bookBuilder.IsProtected)
                {
                    style.Locked = column.Locked ?? false;
                }

                // Avoid the box from GetValue when nobody downstream needs the boxed value.
                var canSkipBox = column.Formula == null &&
                                 column is
                                 {
                                     TypedEnumWriter: not null,
                                     Render: null,
                                     CellStyle: null
                                 };

                var value = canSkipBox ? null : column.GetValue(item);

                if (column.Formula != null)
                {
                    var context = new FormulaContext<TModel>(columnIndexesByName, rowIndex + 1);
                    var formula = column.Formula(item, context);
                    SetCellFormula(cell, formula);
                    if (column.Format != null)
                    {
                        style.NumberFormat = column.Format;
                    }
                }
                else if (column is
                         {
                             TypedEnumWriter: not null,
                             Render: null
                         })
                {
                    column.TypedEnumWriter(item, cell, column);
                }
                else
                {
                    SetCellValue(cell, sheet, style, value, column, item);
                }

                if (bookBuilder.UseAlternatingRowColors &&
                    rowIndex % 2 == 1)
                {
                    style.BackgroundColor = bookBuilder.AlternateRowColor!;
                }

                column.CellStyle?.Invoke(style, item, value);
                CommitStyle(cell, style);
            }

            itemIndex++;
        }
    }

    CellStyle GetStyle(Cell cell)
    {
        var style = new CellStyle();
        cellStyles[cell] = style;
        return style;
    }

    void CommitStyle(Cell cell, CellStyle style) =>
        cell.StyleIndex = styleManager!.GetOrCreateStyleIndex(style);

    SheetContext BuildSheet(SpreadsheetDocument book)
    {
        styleManager = bookBuilder.StyleManager;

        var workbookPart = book.WorkbookPart!;
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new(new SheetData());

        var sheets = workbookPart.Workbook!.GetFirstChild<Sheets>()!;
        var sheetId = (uint)(sheets.Count() + 1);
        sheets.Append(
            new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = name
            });

        return new(worksheetPart);
    }

    void FreezeHeader(SheetContext sheet)
    {
        // Frozen rows are always contiguous from the top, so freezing the header necessarily
        // freezes any banner above it. A banner with Freeze=false therefore frees the whole top
        // region (the header cannot stay pinned while the banner above it scrolls).
        var freezeRows = Banner is { Freeze: false } ? 0 : 1 + BannerRows;
        if (freezeRows == 0)
        {
            return;
        }

        var sheetViews = new SheetViews(
            new SheetView(
                new Pane
                {
                    VerticalSplit = freezeRows,
                    TopLeftCell = $"A{freezeRows + 1}",
                    ActivePane = PaneValues.BottomLeft,
                    State = PaneStateValues.Frozen
                })
            {
                TabSelected = true,
                WorkbookViewId = 0
            });
        sheet.Worksheet.InsertBefore(sheetViews, sheet.SheetData);
    }

    static void SetCellValue(Cell cell, object value)
    {
        switch (value)
        {
            case bool b:
                cell.DataType = CellValues.Boolean;
                // Excel's spec for t="b" cells requires "1"/"0"; OpenXml's CellValue(bool)
                // ctor writes "true"/"false" (XmlConvert.ToString), which Excel itself
                // tolerates but downstream readers (and formulas like COUNTIF) may not.
                cell.CellValue = new(b ? "1" : "0");
                break;
            case DateTime dt:
                cell.CellValue = new(dt.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case double d:
                cell.CellValue = new(d.ToString(CultureInfo.InvariantCulture));
                break;
            default:
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new(BuildText(value.ToString() ?? ""));
                break;
        }
    }

    static void SetCellFormula(Cell cell, string formula)
    {
        var text = formula.StartsWith('=') ? formula[1..] : formula;
        cell.CellFormula = new(text);
    }

    static void SetCellValue(Cell cell, string value) =>
        CellWrite.String(cell, value);

    static Text BuildText(string value) =>
        CellWrite.BuildText(value);

    static void SetCellHtml(Cell cell, string value) =>
        CellWrite.Html(cell, value);

    static void SetCellList(Cell cell, IReadOnlyList<string> items)
    {
        cell.DataType = CellValues.InlineString;
        var inlineString = new InlineString();
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                inlineString.Append(new Run(BuildText("\n")));
            }

            inlineString.Append(
                new Run(
                    new RunProperties(new Bold()),
                    BuildText("● ")));
            inlineString.Append(new Run(BuildText(items[i])));
        }

        cell.InlineString = inlineString;
    }

    static void SetCellLink(Cell cell, SheetContext sheet, CellStyle style, Link link)
    {
        var display = link.Text ?? link.Url;
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new(BuildText(display));

        var rel = sheet.WorksheetPart.AddHyperlinkRelationship(new(link.Url), true);
        var hyperlinks = sheet.Worksheet.GetFirstChild<Hyperlinks>();
        if (hyperlinks == null)
        {
            hyperlinks = new();
            sheet.Worksheet.InsertAfter(hyperlinks, sheet.SheetData);
        }

        hyperlinks.Append(
            new Hyperlink
            {
                Reference = cell.CellReference,
                Id = rel.Id
            });

        style.Font.Color = "0563C1";
        style.Font.Underline = true;
    }

    static void SetCellLinkList(Cell cell, SheetContext sheet, List<string> items, string? hyperlinkUrl)
    {
        cell.DataType = CellValues.InlineString;
        var inlineString = new InlineString();
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                inlineString.Append(new Run(BuildText("\n")));
            }

            inlineString.Append(
                new Run(
                    new RunProperties(new Bold()),
                    BuildText("● ")));
            inlineString.Append(
                new Run(
                    new RunProperties(
                        new Underline(),
                        new Color
                        {
                            Rgb = "0563C1"
                        }),
                    BuildText(items[i])));
        }

        cell.InlineString = inlineString;

        if (hyperlinkUrl == null)
        {
            return;
        }

        var rel = sheet.WorksheetPart.AddHyperlinkRelationship(new(hyperlinkUrl), true);
        var hyperlinks = sheet.Worksheet.GetFirstChild<Hyperlinks>();
        if (hyperlinks == null)
        {
            hyperlinks = new();
            sheet.Worksheet.InsertAfter(hyperlinks, sheet.SheetData);
        }

        hyperlinks.Append(
            new Hyperlink
            {
                Reference = cell.CellReference,
                Id = rel.Id
            });
    }

    void ApplyGlobalStyling(Action<CellStyle> globalStyle)
    {
        foreach (var (cell, style) in cellStyles)
        {
            globalStyle(style);
            cell.StyleIndex = styleManager!.GetOrCreateStyleIndex(style);
        }
    }

    void ApplyFilter(SheetContext sheet, int firstColumn, int lastColumn)
    {
        if (sheet.RowCount == 0)
        {
            return;
        }

        var firstCol = SheetContext.GetColumnLetter(firstColumn);
        var lastCol = SheetContext.GetColumnLetter(lastColumn);
        var reference = $"{firstCol}{BannerRows + 1}:{lastCol}{sheet.RowCount}";
        sheet.Worksheet
            .InsertAfter(
                new AutoFilter
                {
                    Reference = reference
                },
                sheet.SheetData);
    }

    double AdjustColumnWidth(SheetContext sheet, int columnIndex)
    {
        double maxWidth = 8;
        var colLetter = SheetContext.GetColumnLetter(columnIndex);

        foreach (var row in sheet.SheetData.Elements<Row>())
        {
            if (Banner != null &&
                row.RowIndex?.Value == 1)
            {
                // The banner spans every column (merged), so its text must not drive any single
                // column's width.
                continue;
            }

            var cellRef = colLetter + row.RowIndex;
            var cell = row.Elements<Cell>()
                .FirstOrDefault(_ => _.CellReference?.Value == cellRef);
            if (cell == null)
            {
                continue;
            }

            var length = GetCellContentLength(cell);
            var estimated = length * CharWidthFactor(cell) + 2;
            if (estimated > maxWidth)
            {
                maxWidth = estimated;
            }
        }

        return maxWidth;
    }

    double CharWidthFactor(Cell cell)
    {
        // Per-char width factor for default Calibri 11 regular. Scaled up for larger fonts
        // (linear in point size) and bold text (~5% wider). Without this, columns sized for
        // bold headings or non-default font sizes clip to "########".
        const double baseFactor = 1.1;
        if (!cellStyles.TryGetValue(cell, out var style))
        {
            return baseFactor;
        }

        var size = style.Font.Size ?? defaultExcelFontSize;
        var factor = baseFactor * (size / defaultExcelFontSize);
        if (style.Font.Bold)
        {
            factor *= 1.05;
        }

        return factor;
    }

    void ValidateFormulaColumnWidths()
    {
        foreach (var column in columns)
        {
            if (column.Formula == null)
            {
                continue;
            }

            if (column.MinWidth != null ||
                column.MaxWidth != null)
            {
                throw new($"Column '{column.Name}': formula columns cannot use MinWidth/MaxWidth — Excel calculates the value at open time, so auto-sizing has no rendered text to measure. Set Width explicitly.");
            }

            if (column.Width == null)
            {
                throw new($"Column '{column.Name}': formula columns must set Width explicitly — Excel calculates the value at open time, so auto-sizing has no rendered text to measure.");
            }
        }
    }

    void RecordDateDisplayLength(Cell cell, DateTime value, string format) =>
        cellDisplayLengths[cell] = value.ToString(format, ValueRenderer.Culture).Length;

    int GetCellContentLength(Cell cell)
    {
        if (cellDisplayLengths.TryGetValue(cell, out var displayLength))
        {
            return displayLength;
        }

        if (cell.InlineString != null)
        {
            var length = 0;
            var hasRuns = false;
            foreach (var run in cell.InlineString.Elements<Run>())
            {
                hasRuns = true;
                length += run.Text?.Text.Length ?? 0;
            }

            if (hasRuns)
            {
                return length;
            }

            if (cell.InlineString.Text != null)
            {
                return cell.InlineString.Text.Text.Length;
            }
        }

        return cell.CellValue?.Text.Length ?? 0;
    }

    void ResizeRows(SheetContext sheet)
    {
        if (finalColumnWidths.Count <= 0 && columnLevelStyles.Count <= 0)
        {
            return;
        }

        var indices = finalColumnWidths.Keys
            .Concat(columnLevelStyles.Keys)
            .Distinct()
            .OrderBy(_ => _);

        var cols = new Columns();
        foreach (var index in indices)
        {
            var col = new Column
            {
                Min = (uint)(index + 1),
                Max = (uint)(index + 1),
            };

            if (finalColumnWidths.TryGetValue(index, out var width))
            {
                col.Width = width;
                col.CustomWidth = true;
            }

            if (columnLevelStyles.TryGetValue(index, out var styleIndex))
            {
                col.Style = styleIndex;
            }

            cols.Append(col);
        }

        sheet.Worksheet.InsertBefore(cols, sheet.SheetData);
    }

    void BuildColumnLevelStyles()
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var hasFormat = column.Format != null;
            var hasLockOverride = bookBuilder.IsProtected;

            if (!hasFormat && !hasLockOverride)
            {
                continue;
            }

            var style = new CellStyle();
            if (hasFormat)
            {
                style.NumberFormat = column.Format;
            }

            if (hasLockOverride)
            {
                style.Locked = column.Locked ?? false;
            }

            columnLevelStyles[i] = styleManager!.GetOrCreateStyleIndex(style);
        }
    }

    void EmitConditionalFormatting(SheetContext sheet)
    {
        var validationFirstRow = 2 + BannerRows;
        var validationLastRow = ComputeValidationLastRow(sheet);
        if (validationLastRow < validationFirstRow)
        {
            return;
        }

        uint? dxfId = null;
        var priority = 1;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            if (!column.Required)
            {
                continue;
            }

            dxfId ??= styleManager!.GetOrCreateDxfFillIndex(requiredHighlightColor);
            var letter = SheetContext.GetColumnLetter(i);
            var sqref = $"{letter}{validationFirstRow}:{letter}{validationLastRow}";
            var cf = new ConditionalFormatting
            {
                SequenceOfReferences = new()
                {
                    InnerText = sqref
                }
            };
            var rule = new ConditionalFormattingRule
            {
                Type = ConditionalFormatValues.ContainsBlanks,
                Priority = priority++,
                FormatId = dxfId
            };
            rule.Append(new Formula($"LEN(TRIM({letter}{validationFirstRow}))=0"));
            cf.Append(rule);
            sheet.Worksheet.Append(cf);
        }
    }

    void EmitDataValidations(SheetContext sheet)
    {
        var validationFirstRow = 2 + BannerRows;
        var validationLastRow = ComputeValidationLastRow(sheet);
        if (validationLastRow < validationFirstRow)
        {
            return;
        }

        DataValidations? validations = null;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            if (column is
                {
                    HasValidation: false,
                    HasInputMessage: false
                })
            {
                continue;
            }

            var letter = SheetContext.GetColumnLetter(i);
            var firstCell = $"{letter}{validationFirstRow}";
            var sqref = $"{firstCell}:{letter}{validationLastRow}";
            var validation = BuildDataValidation(column, sqref, firstCell, AutoInputMessages);
            if (validation == null)
            {
                continue;
            }

            validations ??= new();
            validations.Append(validation);
        }

        if (validations == null)
        {
            return;
        }

        var count = 0u;
        foreach (var _ in validations.Elements<DataValidation>())
        {
            count++;
        }

        validations.Count = count;
        sheet.Worksheet.Append(validations);
    }

    // Notes are anchored to the heading cell of each column that declares one. They survive
    // sheet protection (unlike an input-message tooltip, which only shows on an editable cell)
    // and are the natural place to explain a constraint or why a column is locked. A legacy
    // note needs three things: a comments part (the text), a VML drawing part (the box shape),
    // and a <legacyDrawing> on the worksheet pointing at the VML.
    void EmitComments(SheetContext sheet)
    {
        List<(int Index, string Reference, string Text)>? notes = null;
        for (var i = 0; i < columns.Count; i++)
        {
            var text = columns[i].Note;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            notes ??= [];
            notes.Add((i, $"{SheetContext.GetColumnLetter(i)}{BannerRows + 1}", text));
        }

        if (notes == null)
        {
            return;
        }

        var worksheetPart = sheet.WorksheetPart;

        var commentList = new CommentList();
        foreach (var note in notes)
        {
            var comment = new Comment
            {
                Reference = note.Reference,
                AuthorId = 0U
            };
            comment.Append(
                new CommentText(
                    new Run(
                        new Text(note.Text)
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        })));
            commentList.Append(comment);
        }

        var commentsPart = worksheetPart.AddNewPart<WorksheetCommentsPart>();
        // A single empty author keeps the note body clean — Excel shows just the text, with no
        // "Author:" prefix — while still satisfying the schema's required authors list.
        commentsPart.Comments = new(new Authors(new Author("")), commentList);

        var vmlPart = worksheetPart.AddNewPart<VmlDrawingPart>();
        WriteCommentVml(vmlPart, notes, BannerRows);

        sheet.Worksheet.Append(
            new LegacyDrawing
            {
                Id = worksheetPart.GetIdOfPart(vmlPart)
            });
    }

    static void WriteCommentVml(VmlDrawingPart part, List<(int Index, string Reference, string Text)> notes, int rowOffset)
    {
        var builder = new StringBuilder();
        builder.Append(
            """
            <xml xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel"><o:shapelayout v:ext="edit"><o:idmap v:ext="edit" data="1"/></o:shapelayout><v:shapetype id="_x0000_t202" coordsize="21600,21600" o:spt="202" path="m,l,21600r21600,l21600,xe"><v:stroke joinstyle="miter"/><v:path gradientshapeok="t" o:connecttype="rect"/></v:shapetype>
            """);

        var shapeId = 1025;
        foreach (var note in notes)
        {
            var column = note.Index;
            var zIndex = shapeId - 1024;
            // The box is anchored a couple of columns to the right of the heading and stays
            // hidden until the cell is hovered, matching how Excel itself emits notes.
            builder.Append(
                $"""
                 <v:shape id="_x0000_s{shapeId}" type="#_x0000_t202" style="position:absolute;width:108pt;height:60pt;z-index:{zIndex};visibility:hidden" fillcolor="#ffffe1" o:insetmode="auto"><v:fill color2="#ffffe1"/><v:shadow on="t" color="black" obscured="t"/><v:path o:connecttype="none"/><v:textbox style="mso-direction-alt:auto"><div style="text-align:left"></div></v:textbox><x:ClientData ObjectType="Note"><x:MoveWithCells/><x:SizeWithCells/><x:Anchor>{column + 1}, 15, {rowOffset}, 2, {column + 3}, 15, {rowOffset + 4}, 16</x:Anchor><x:AutoFill>False</x:AutoFill><x:Row>{rowOffset}</x:Row><x:Column>{column}</x:Column></x:ClientData></v:shape>
                 """);
            shapeId++;
        }

        builder.Append("</xml>");

        using var stream = part.GetStream(FileMode.Create);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(builder.ToString());
    }

    int ComputeValidationLastRow(SheetContext sheet)
    {
        var headerRow = 1 + BannerRows;
        var dataRowCount = Math.Max(0, sheet.RowCount - headerRow);
        return headerRow + dataRowCount + templateRowCount;
    }

    static DataValidation? BuildDataValidation(ColumnConfig<TModel> column, string sqref, string firstCell, bool autoInputMessages)
    {
        var validation = new DataValidation
        {
            SequenceOfReferences = new()
            {
                InnerText = sqref
            }
        };

        var hasValidation = false;

        if (column.AllowedValues is { Count: > 0 } values)
        {
            hasValidation = true;
            validation.Type = DataValidationValues.List;
            var list = string.Join(",", values);
            validation.Formula1 = new($"\"{list}\"");
        }
        else if (column.NumericMin.HasValue ||
                 column.NumericMax.HasValue)
        {
            hasValidation = true;
            validation.Type = DataValidationValues.Decimal;
            ApplyRangeOperator(validation, column.NumericMin, column.NumericMax);
        }
        else if (column.DateMin.HasValue ||
                 column.DateMax.HasValue)
        {
            hasValidation = true;
            validation.Type = DataValidationValues.Date;
            ApplyRangeOperator(
                validation,
                column.DateMin?.ToOADate() is { } min ? (decimal)min : null,
                column.DateMax?.ToOADate() is { } max ? (decimal)max : null);
        }
        else if (column.HasNumericValidation)
        {
            hasValidation = true;
            validation.Type = DataValidationValues.Custom;
            validation.Formula1 = new($"ISNUMBER({firstCell})");
        }
        else if (column.Required)
        {
            // No type-specific constraint, but the column is required — block blank entries
            // (empty or whitespace-only) via a custom validation.
            hasValidation = true;
            validation.Type = DataValidationValues.Custom;
            validation.Formula1 = new($"LEN(TRIM({firstCell}))>0");
        }

        if (!hasValidation && !column.HasInputMessage)
        {
            return null;
        }

        if (hasValidation)
        {
            validation.AllowBlank = !column.Required;
            // Without ShowErrorMessage, Excel renders the dropdown but silently
            // accepts manually-typed invalid values. Force it on so the configured
            // (or default Stop) error popup actually fires.
            validation.ShowErrorMessage = true;
            validation.ErrorTitle = column.ErrorTitle ?? "Invalid value";
            validation.Error = column.ErrorMessage ?? BuildDefaultErrorMessage(column);
            if (column.ErrorStyle is { } errorStyle)
            {
                validation.ErrorStyle = ToOpenXmlErrorStyle(errorStyle);
            }
        }

        if (column.HasInputMessage)
        {
            validation.ShowInputMessage = true;
            if (column.InputTitle != null)
            {
                validation.PromptTitle = column.InputTitle;
            }

            if (column.InputMessage != null)
            {
                validation.Prompt = column.InputMessage;
            }
        }
        else if (autoInputMessages &&
                 hasValidation &&
                 !column.DisableInputMessage &&
                 BuildDefaultInputMessage(column) is { } autoPrompt)
        {
            // The column carries a constraint but the author hasn't written an input hint.
            // Surface the rule proactively so the editor sees it on cell select, not just
            // after tripping the error popup.
            validation.ShowInputMessage = true;
            validation.Prompt = autoPrompt;
        }

        return validation;
    }

    static DataValidationErrorStyleValues ToOpenXmlErrorStyle(ValidationErrorStyle style) =>
        style switch
        {
            ValidationErrorStyle.Warning => DataValidationErrorStyleValues.Warning,
            ValidationErrorStyle.Information => DataValidationErrorStyleValues.Information,
            _ => DataValidationErrorStyleValues.Stop
        };

    static string BuildDefaultErrorMessage(ColumnConfig<TModel> column)
    {
        if (column.AllowedValues is { Count: > 0 } values)
        {
            const int previewCount = 5;
            string preview;
            if (values.Count <= previewCount)
            {
                preview = string.Join(", ", values);
            }
            else
            {
                preview = string.Join(", ", values.Take(previewCount)) + ", …";
            }

            return $"Must be one of: {preview}.";
        }

        if (column is { NumericMin: not null, NumericMax: not null })
        {
            return $"Must be a number between {Num(column.NumericMin.Value)} and {Num(column.NumericMax.Value)}.";
        }

        if (column.NumericMin.HasValue)
        {
            return $"Must be a number greater than or equal to {Num(column.NumericMin.Value)}.";
        }

        if (column.NumericMax.HasValue)
        {
            return $"Must be a number less than or equal to {Num(column.NumericMax.Value)}.";
        }

        if (column is { DateMin: not null, DateMax: not null })
        {
            return $"Must be a date between {Day(column.DateMin.Value)} and {Day(column.DateMax.Value)}.";
        }

        if (column.DateMin.HasValue)
        {
            return $"Must be a date on or after {Day(column.DateMin.Value)}.";
        }

        if (column.DateMax.HasValue)
        {
            return $"Must be a date on or before {Day(column.DateMax.Value)}.";
        }

        if (column.HasNumericValidation)
        {
            return "Must be a number.";
        }

        if (column.Required)
        {
            return "This field is required.";
        }

        return "Invalid value.";

        static string Num(decimal value) =>
            value.ToString(CultureInfo.InvariantCulture);

        static string Day(DateTime value) =>
            value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Guidance text shown proactively as the cell's input hint, derived from the column's
    /// constraint. Mirrors <see cref="BuildDefaultErrorMessage"/> but phrased as instruction,
    /// and returns <c>null</c> for the bare <c>ISNUMBER</c> type check — that a numeric column
    /// holds numbers is self-evident, so nagging about it on every cell select is noise.
    /// </summary>
    static string? BuildDefaultInputMessage(ColumnConfig<TModel> column)
    {
        if (column.AllowedValues is { Count: > 0 } values)
        {
            const int previewCount = 5;
            var preview = values.Count <= previewCount
                ? string.Join(", ", values)
                : string.Join(", ", values.Take(previewCount)) + ", …";
            return $"Select one of: {preview}";
        }

        if (column is { NumericMin: not null, NumericMax: not null })
        {
            return $"Number between {Num(column.NumericMin.Value)} and {Num(column.NumericMax.Value)}";
        }

        if (column.NumericMin.HasValue)
        {
            return $"Number greater than or equal to {Num(column.NumericMin.Value)}";
        }

        if (column.NumericMax.HasValue)
        {
            return $"Number less than or equal to {Num(column.NumericMax.Value)}";
        }

        if (column is { DateMin: not null, DateMax: not null })
        {
            return $"Date between {Day(column.DateMin.Value)} and {Day(column.DateMax.Value)}";
        }

        if (column.DateMin.HasValue)
        {
            return $"Date on or after {Day(column.DateMin.Value)}";
        }

        if (column.DateMax.HasValue)
        {
            return $"Date on or before {Day(column.DateMax.Value)}";
        }

        if (column.Required)
        {
            return "Required field";
        }

        return null;

        static string Num(decimal value) =>
            value.ToString(CultureInfo.InvariantCulture);

        static string Day(DateTime value) =>
            value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    static void ApplyRangeOperator(DataValidation validation, decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue)
        {
            validation.Operator = DataValidationOperatorValues.Between;
            validation.Formula1 = new(min.Value.ToString(CultureInfo.InvariantCulture));
            validation.Formula2 = new(max.Value.ToString(CultureInfo.InvariantCulture));
        }
        else if (min.HasValue)
        {
            validation.Operator = DataValidationOperatorValues.GreaterThanOrEqual;
            validation.Formula1 = new(min.Value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            validation.Operator = DataValidationOperatorValues.LessThanOrEqual;
            validation.Formula1 = new(max!.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    void ApplyMaxRowHeight(SheetContext sheet)
    {
        var max = maxRowHeight ?? bookBuilder.MaxRowHeight;
        if (max == null)
        {
            return;
        }

        var pointsPerLine = ComputePointsPerLine();
        var minRowHeight = (int)Math.Ceiling(pointsPerLine);

        if (max < minRowHeight || max > maxExcelRowHeight)
        {
            throw new($"MaxRowHeight ({max}) must be between {minRowHeight} (one line at the configured font size) and {maxExcelRowHeight}.");
        }

        var maxLinesAllowed = max.Value / pointsPerLine;

        var lastPinnedRow = (uint)(1 + BannerRows);
        foreach (var row in sheet.SheetData.Elements<Row>())
        {
            if (row.RowIndex?.Value <= lastPinnedRow)
            {
                // Banner and header rows always auto-size; MaxRowHeight does not apply.
                continue;
            }

            double maxLines = 1;
            for (var i = 0; i < columns.Count; i++)
            {
                var colLetter = SheetContext.GetColumnLetter(i);
                var cellRef = colLetter + row.RowIndex;
                var cell = row.Elements<Cell>()
                    .FirstOrDefault(_ => _.CellReference?.Value == cellRef);
                if (cell == null)
                {
                    continue;
                }

                var width = finalColumnWidths.GetValueOrDefault(i, 8d);
                var lines = EstimateVisualLines(cell, width);
                if (lines > maxLines)
                {
                    maxLines = lines;
                }
            }

            if (maxLines > maxLinesAllowed)
            {
                row.Height = (double)max;
                row.CustomHeight = true;
            }
        }
    }

    double ComputePointsPerLine()
    {
        var maxFontSize = defaultExcelFontSize;
        ProbeFontSize(bookBuilder.GlobalStyle, ref maxFontSize);
        ProbeFontSize(bookBuilder.HeadingStyle, ref maxFontSize);
        // Excel's row height in points is approximately font size + 4 padding (Calibri 11 → 15).
        return maxFontSize + 4;
    }

    static void ProbeFontSize(Action<CellStyle>? styleAction, ref double maxFontSize)
    {
        if (styleAction == null)
        {
            return;
        }

        var probe = new CellStyle();
        styleAction(probe);
        if (probe.Font.Size is { } size && size > maxFontSize)
        {
            maxFontSize = size;
        }
    }

    static double EstimateVisualLines(Cell cell, double columnWidth)
    {
        var charsPerLine = Math.Max(1d, (columnWidth - 2) / 1.1);
        double lines = 0;
        var hasContent = false;
        foreach (var text in EnumerateCellTexts(cell))
        {
            hasContent = true;
            foreach (var line in text.Split('\n'))
            {
                lines += Math.Max(1d, Math.Ceiling(line.Length / charsPerLine));
            }
        }

        return hasContent ? lines : 1;
    }

    static IEnumerable<string> EnumerateCellTexts(Cell cell)
    {
        if (cell.InlineString != null)
        {
            var hasRuns = false;
            foreach (var run in cell.InlineString.Elements<Run>())
            {
                hasRuns = true;
                if (run.Text?.Text is { } runText)
                {
                    yield return runText;
                }
            }

            if (!hasRuns && cell.InlineString.Text?.Text is { } inlineText)
            {
                yield return inlineText;
            }

            yield break;
        }

        if (cell.CellValue?.Text is { } cellValueText)
        {
            yield return cellValueText;
        }
    }

    void SetCellValue(
        Cell cell,
        SheetContext sheet,
        CellStyle style,
        object? value,
        ColumnConfig<TModel> column,
        TModel item)
    {
        void SetStringOrHtml(string content)
        {
            if (column.IsHtml)
            {
                SetCellHtml(cell, content);
            }
            else
            {
                SetCellValue(cell, content);
            }
        }

        void ThrowIfHtml()
        {
            if (column.IsHtml)
            {
                throw new("TreatAsHtml is not compatible with this type");
            }
        }

        if (value == null)
        {
            if (column.NullDisplay != null)
            {
                SetCellValue(cell, column.NullDisplay);
            }

            return;
        }

        if (column.TryRender(item, value, out var render))
        {
            SetStringOrHtml(render);

            return;
        }

        if (value is Link link)
        {
            SetCellLink(cell, sheet, style, link);
            return;
        }

        if (column.IsEnumerable &&
            value is IEnumerable<Link?> linkEnumerable)
        {
            var links = new List<Link>();
            foreach (var l in linkEnumerable)
            {
                if (l == null)
                {
                    continue;
                }

                links.Add(l);
            }

            if (links.Count > 0)
            {
                var linkItems = new List<string>(links.Count);
                foreach (var l in links)
                {
                    linkItems.Add(l.Text == null ? l.Url : $"{l.Text} ({l.Url})");
                }

                var hyperlinkUrl = links.Count == 1 ? links[0].Url : null;
                SetCellLinkList(cell, sheet, linkItems, hyperlinkUrl);
            }

            return;
        }

        if (column.IsEnumerable &&
            value is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (var obj in enumerable)
            {
                if (obj == null)
                {
                    continue;
                }

                var str = column.ItemRender == null ? obj.ToString() : column.ItemRender(obj);
                if (str != null &&
                    ValueRenderer.TrimWhitespace)
                {
                    str = str.Trim();
                }

                if (str != null)
                {
                    items.Add(str);
                }
            }

            if (items.Count > 0)
            {
                SetCellList(cell, items);
            }

            return;
        }

        if (value is DateTime dateTime)
        {
            ThrowIfHtml();
            var format = column.Format ?? ValueRenderer.DefaultDateTimeFormat;
            style.NumberFormat = format;
            SetCellValue(cell, dateTime);
            RecordDateDisplayLength(cell, dateTime, format);

            return;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            ThrowIfHtml();
            // Excel cells can't represent a timezone offset — written as a string
            // so the offset round-trips. Cell is text, not a date, so no NumberFormat.
            var format = column.Format ?? ValueRenderer.DefaultDateTimeOffsetFormat;
            SetCellValue(cell, dateTimeOffset.ToString(format, ValueRenderer.Culture));

            return;
        }

        if (value is Date date)
        {
            ThrowIfHtml();
            var format = column.Format ?? ValueRenderer.DefaultDateFormat;
            style.NumberFormat = format;
            var asDateTime = date.ToDateTime(new(0, 0));
            SetCellValue(cell, asDateTime);
            RecordDateDisplayLength(cell, asDateTime, format);

            return;
        }

        if (value is Time time)
        {
            ThrowIfHtml();
            var format = column.Format ?? ValueRenderer.DefaultTimeFormat;
            style.NumberFormat = format;
            var asDateTime = DateTime.FromOADate(0).Add(time.ToTimeSpan());
            SetCellValue(cell, asDateTime);
            RecordDateDisplayLength(cell, asDateTime, format);

            return;
        }

        if (value is bool boolean)
        {
            ThrowIfHtml();
            var format = column.Format ?? ValueRenderer.BoolFormat;
            if (format != null)
            {
                style.NumberFormat = format;
            }

            SetCellValue(cell, boolean);
            var (trueDisplay, falseDisplay) = ValueRenderer.GetBoolDisplayValues();
            cellDisplayLengths[cell] = boolean ? trueDisplay.Length : falseDisplay.Length;
            return;
        }

        if (column.IsNumber)
        {
            ThrowIfHtml();
            var asDouble = Convert.ToDouble(value);
            if (column.Format != null)
            {
                style.NumberFormat = column.Format;
                cellDisplayLengths[cell] = asDouble.ToString(column.Format, ValueRenderer.Culture).Length;
            }

            SetCellValue(cell, asDouble);
            return;
        }

        var valueAsString = value.ToString();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (valueAsString != null &&
            ValueRenderer.TrimWhitespace)
        {
            valueAsString = valueAsString.Trim();
        }

        if (valueAsString != null)
        {
            SetStringOrHtml(valueAsString);
        }
    }
}
