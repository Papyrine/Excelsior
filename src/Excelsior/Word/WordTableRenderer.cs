using W = DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Internal worker that turns a sequence of model rows + column configs into a Word
/// <c>&lt;w:tbl&gt;</c> element.
/// </summary>
static class WordTableRenderer<TModel>
{
    public static W.Table Build(
        IEnumerable<TModel> data,
        List<ColumnConfig<TModel>> columns,
        Action<CellStyle>? tableHeadingStyle,
        Action<CellStyle>? tableBodyStyle,
        string? headingParagraphStyle,
        string? bodyParagraphStyle,
        MainDocumentPart? mainPart)
    {
        var table = new W.Table();
        table.Append(BuildTableProperties(mainPart));
        table.Append(BuildGrid(columns.Count));
        table.Append(BuildHeaderRow(columns, tableHeadingStyle, headingParagraphStyle));

        foreach (var item in data)
        {
            table.Append(BuildDataRow(columns, item, tableBodyStyle, bodyParagraphStyle, mainPart));
        }

        return table;
    }

    /// <summary>
    /// Applies a named paragraph style (by style id) to a cell paragraph. Set as the first child of
    /// the paragraph's properties so it sits at the correct schema position; existing direct
    /// formatting on the paragraph/runs still wins over the style per Word's precedence rules.
    /// </summary>
    static void ApplyParagraphStyle(W.Paragraph paragraph, string styleId)
    {
        var properties = paragraph.ParagraphProperties ??= new();
        properties.ParagraphStyleId = new()
        {
            Val = styleId
        };
    }

    /// <summary>
    /// Builds <c>&lt;w:tblPr&gt;</c>. Tables are rendered at 100% page width via
    /// <c>&lt;w:tblW w:type="pct" w:w="5000"/&gt;</c>. When a <c>MainDocumentPart</c> is supplied,
    /// the table references Word's built-in <c>TableGrid</c> style (single-line borders) — the
    /// definition is added to the host's styles part if absent, mirroring what Word itself does
    /// when a table is inserted via the ribbon. Customizing <c>TableGrid</c> in the template is
    /// the supported way to rebrand Excelsior tables. The standalone path (no host) emits inline
    /// borders instead, since there's no styles part to add to.
    /// </summary>
    static W.TableProperties BuildTableProperties(MainDocumentPart? mainPart)
    {
        var tblW = new W.TableWidth
        {
            Width = "5000",
            Type = W.TableWidthUnitValues.Pct
        };

        var tblLook = new W.TableLook
        {
            Val = "04A0",
            FirstRow = true,
            LastRow = false,
            FirstColumn = false,
            LastColumn = false,
            NoHorizontalBand = false,
            NoVerticalBand = true,
        };

        if (mainPart != null)
        {
            TableGridStyle.EnsurePresent(mainPart);
            return new(
                new W.TableStyle
                {
                    Val = TableGridStyle.StyleId
                },
                tblW,
                tblLook);
        }

        return new(
            tblW,
            new W.TableBorders(
                new W.TopBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                },
                new W.BottomBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                },
                new W.LeftBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                },
                new W.RightBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                },
                new W.InsideHorizontalBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                },
                new W.InsideVerticalBorder
                {
                    Val = W.BorderValues.Single,
                    Size = 4
                }),
            new W.TableCellMarginDefault(
                new W.TopMargin
                {
                    Width = "0",
                    Type = W.TableWidthUnitValues.Dxa
                },
                new W.StartMargin
                {
                    Width = "108",
                    Type = W.TableWidthUnitValues.Dxa
                },
                new W.BottomMargin
                {
                    Width = "0",
                    Type = W.TableWidthUnitValues.Dxa
                },
                new W.EndMargin
                {
                    Width = "108",
                    Type = W.TableWidthUnitValues.Dxa
                }),
            tblLook);
    }

    static W.TableGrid BuildGrid(int columnCount)
    {
        var grid = new W.TableGrid();
        for (var i = 0; i < columnCount; i++)
        {
            grid.Append(new W.GridColumn());
        }

        return grid;
    }

    static void AppendTextWithLineBreaks(W.Run run, string value)
    {
        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                run.Append(new W.Break());
            }

            run.Append(
                new W.Text(lines[i])
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
        }
    }

    static W.TableRow BuildHeaderRow(List<ColumnConfig<TModel>> columns, Action<CellStyle>? tableHeadingStyle, string? headingParagraphStyle)
    {
        var row = new W.TableRow();
        foreach (var column in columns)
        {
            row.Append(BuildHeaderCell(column, tableHeadingStyle, headingParagraphStyle));
        }

        return row;
    }

    static W.TableCell BuildHeaderCell(ColumnConfig<TModel> column, Action<CellStyle>? tableHeadingStyle, string? headingParagraphStyle)
    {
        var style = ResolveHeadingStyle(column, tableHeadingStyle);

        var run = new W.Run(BuildRunProperties(style));
        AppendTextWithLineBreaks(run, column.Heading);

        var paragraph = new W.Paragraph(BuildAlignmentParagraphProperties(style), run);
        if (headingParagraphStyle != null)
        {
            ApplyParagraphStyle(paragraph, headingParagraphStyle);
        }

        var cell = new W.TableCell(paragraph);

        var cellProperties = BuildCellProperties(style);
        if (cellProperties != null)
        {
            cell.PrependChild(cellProperties);
        }

        return cell;
    }

    static CellStyle ResolveHeadingStyle(ColumnConfig<TModel> column, Action<CellStyle>? tableHeadingStyle)
    {
        // Preseed with the renderer's header defaults (bold, left-aligned) so callers can layer on
        // additions (background, font color, size) or opt out (set Font.Bold = false) without
        // having to restate what they didn't want to change.
        var style = new CellStyle
        {
            Alignment =
            {
                Horizontal = HorizontalAlignmentValues.Left
            },
            Font =
            {
                Bold = true
            }
        };

        tableHeadingStyle?.Invoke(style);
        column.HeadingStyle?.Invoke(style);
        return style;
    }

    static W.RunProperties BuildRunProperties(CellStyle style)
    {
        var properties = new W.RunProperties();
        if (style.Font.Bold)
        {
            properties.Append(new W.Bold());
        }

        if (style.Font.Underline)
        {
            properties.Append(
                new W.Underline
                {
                    Val = W.UnderlineValues.Single
                });
        }

        if (!string.IsNullOrEmpty(style.Font.Color))
        {
            properties.Append(
                new W.Color
                {
                    Val = style.Font.Color
                });
        }

        if (style.Font.Size is { } size)
        {
            // Word uses half-points for font size.
            var halfPoints = ((int)Math.Round(size * 2)).ToString(CultureInfo.InvariantCulture);
            properties.Append(
                new W.FontSize
                {
                    Val = halfPoints
                });
            properties.Append(
                new W.FontSizeComplexScript
                {
                    Val = halfPoints
                });
        }

        if (!string.IsNullOrEmpty(style.Font.Name))
        {
            properties.Append(
                new W.RunFonts
                {
                    Ascii = style.Font.Name,
                    HighAnsi = style.Font.Name
                });
        }

        return properties;
    }

    static W.ParagraphProperties BuildAlignmentParagraphProperties(CellStyle style)
    {
        var horizontal = style.Alignment.Horizontal;
        W.JustificationValues justification;
        if (horizontal == HorizontalAlignmentValues.Center)
        {
            justification = W.JustificationValues.Center;
        }
        else if (horizontal == HorizontalAlignmentValues.Right)
        {
            justification = W.JustificationValues.Right;
        }
        else if (horizontal == HorizontalAlignmentValues.Justify)
        {
            justification = W.JustificationValues.Both;
        }
        else
        {
            // Left / General / Fill all collapse to left, matching the renderer's default.
            justification = W.JustificationValues.Left;
        }

        return new(
            new W.Justification
            {
                Val = justification
            });
    }

    static W.TableCellProperties? BuildCellProperties(CellStyle style)
    {
        var properties = new W.TableCellProperties();
        var hasAny = false;

        if (!string.IsNullOrEmpty(style.BackgroundColor))
        {
            properties.Append(
                new W.Shading
                {
                    Val = W.ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = NormaliseColor(style.BackgroundColor)
                });
            hasAny = true;
        }

        if (style.Alignment.Vertical != VerticalAlignmentValues.Bottom)
        {
            W.TableVerticalAlignmentValues vertical;
            if (style.Alignment.Vertical == VerticalAlignmentValues.Top)
            {
                vertical = W.TableVerticalAlignmentValues.Top;
            }
            else if (style.Alignment.Vertical == VerticalAlignmentValues.Center)
            {
                vertical = W.TableVerticalAlignmentValues.Center;
            }
            else
            {
                vertical = W.TableVerticalAlignmentValues.Bottom;
            }

            properties.Append(
                new W.TableCellVerticalAlignment
                {
                    Val = vertical
                });
            hasAny = true;
        }

        return hasAny ? properties : null;
    }

    static string NormaliseColor(string color) =>
        color.StartsWith('#') ? color[1..] : color;

    static W.TableRow BuildDataRow(List<ColumnConfig<TModel>> columns, TModel item, Action<CellStyle>? tableBodyStyle, string? bodyParagraphStyle, MainDocumentPart? mainPart)
    {
        var row = new W.TableRow();
        foreach (var column in columns)
        {
            var value = column.GetValue(item);
            var style = ResolveBodyStyle(column, item, value, tableBodyStyle);
            var cell = new W.TableCell();

            // Cell-level properties (background shading, vertical alignment) apply regardless of the
            // content path — plain text, HTML, or hyperlink.
            var cellProperties = style == null ? null : BuildCellProperties(style);
            if (cellProperties != null)
            {
                cell.Append(cellProperties);
            }

            foreach (var paragraph in BuildCellParagraphs(column, item, value, style, mainPart))
            {
                // A named paragraph style applies to every paragraph — including those produced by
                // the HTML and hyperlink paths — so the style's font/size/spacing reaches all cells.
                if (bodyParagraphStyle != null)
                {
                    ApplyParagraphStyle(paragraph, bodyParagraphStyle);
                }

                cell.Append(paragraph);
            }

            row.Append(cell);
        }

        return row;
    }

    /// <summary>
    /// Resolves the effective <see cref="CellStyle"/> for a data cell, layering the per-column
    /// <c>CellStyle</c> on top of the table-wide body style. Returns <c>null</c> when neither is
    /// configured so the cell falls back to the lean default (a bare run with no run/paragraph
    /// properties), matching Excelsior's prior output.
    /// </summary>
    static CellStyle? ResolveBodyStyle(ColumnConfig<TModel> column, TModel item, object? value, Action<CellStyle>? tableBodyStyle)
    {
        if (tableBodyStyle == null &&
            column.CellStyle == null)
        {
            return null;
        }

        var style = new CellStyle
        {
            Alignment =
            {
                Horizontal = HorizontalAlignmentValues.Left
            }
        };

        tableBodyStyle?.Invoke(style);
        column.CellStyle?.Invoke(style, item, value);
        return style;
    }

    static IReadOnlyList<W.Paragraph> BuildCellParagraphs(
        ColumnConfig<TModel> column,
        TModel item,
        object? value,
        CellStyle? style,
        MainDocumentPart? mainPart)
    {
        // Excel formulas (=A2*B2 etc.) have no equivalent in Word tables. Fail loudly so the
        // caller knows the column must be restructured rather than silently dropping the formula.
        if (column.Formula != null)
        {
            throw new($"Column '{column.Heading}' has a Formula, which is not supported in Word tables.");
        }

        // A live MainDocumentPart is required to register the hyperlink relationship; without one,
        // fall through to the plain-text path which renders link.Text ?? link.Url.
        if (value is Link link && mainPart != null)
        {
            return [BuildHyperlinkParagraph(link, mainPart)];
        }

        var text = ToText(column, item, value);

        // IsHtml columns route through WordHtmlConverter so inline tags (<i>, <b>, <a>, etc.) and
        // block elements (<p>, <ul>, ...) become proper runs/paragraphs instead of literal text.
        if (column.IsHtml)
        {
            return WordHtmlConverter.ToParagraphs(text, mainPart);
        }

        var run = new W.Run();
        if (style != null)
        {
            var runProperties = BuildRunProperties(style);
            if (runProperties.HasChildren)
            {
                run.AppendChild(runProperties);
            }
        }

        AppendTextWithLineBreaks(run, text);

        var paragraph = new W.Paragraph();
        if (style != null)
        {
            paragraph.AppendChild(BuildAlignmentParagraphProperties(style));
        }

        paragraph.AppendChild(run);
        return [paragraph];
    }

    static W.Paragraph BuildHyperlinkParagraph(Link link, MainDocumentPart mainPart)
    {
        var display = link.Text ?? link.Url;
        var relId = mainPart.AddHyperlinkRelationship(new(link.Url, UriKind.RelativeOrAbsolute), true).Id;

        // Color and underline mirror Excelsior's spreadsheet hyperlink styling (#0563C1, underline)
        // and avoid depending on a "Hyperlink" character style that may not exist in the host doc.
        var run = new W.Run(
            new W.RunProperties(
                new W.Color
                {
                    Val = "0563C1"
                },
                new W.Underline
                {
                    Val = W.UnderlineValues.Single
                }),
            new W.Text(display)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

        return new(
            new W.Hyperlink(run)
            {
                Id = relId
            });
    }

    static string ToText(ColumnConfig<TModel> column, TModel item, object? value)
    {
        if (value == null)
        {
            return column.NullDisplay ?? string.Empty;
        }

        if (column.TryRender(item, value, out var rendered))
        {
            return rendered;
        }

        // Enum columns now route through the typed (non-boxing) writer in the Excel
        // renderer rather than column.Render, so handle them explicitly here.
        if (value is Enum enumValue)
        {
            return ValueRenderer.RenderEnum(enumValue);
        }

        // Hyperlink fallback when the renderer wasn't given a MainDocumentPart: emit the display
        // text only. The relationship cannot be registered without a host document part.
        if (value is Link link)
        {
            return link.Text ?? link.Url;
        }

        if (column.IsEnumerable && value is IEnumerable enumerable and not string)
        {
            var items = new List<string>();
            foreach (var entry in enumerable)
            {
                if (entry == null)
                {
                    continue;
                }

                var entryText = column.ItemRender == null ? entry.ToString() : column.ItemRender(entry);
                if (entryText != null && ValueRenderer.TrimWhitespace)
                {
                    entryText = entryText.Trim();
                }

                if (entryText != null)
                {
                    items.Add(entryText);
                }
            }

            return string.Join(", ", items);
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString(column.Format ?? ValueRenderer.DefaultDateTimeFormat, ValueRenderer.Culture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString(column.Format ?? ValueRenderer.DefaultDateTimeOffsetFormat, ValueRenderer.Culture);
        }

        if (value is Date date)
        {
            return date.ToDateTime(new(0, 0)).ToString(column.Format ?? ValueRenderer.DefaultDateFormat, ValueRenderer.Culture);
        }

        if (value is Time time)
        {
            return time.ToString(column.Format ?? ValueRenderer.DefaultTimeFormat, ValueRenderer.Culture);
        }

        if (column.Format != null && value is IFormattable formattable)
        {
            return formattable.ToString(column.Format, ValueRenderer.Culture);
        }

        var asString = value.ToString() ?? string.Empty;
        if (ValueRenderer.TrimWhitespace)
        {
            asString = asString.Trim();
        }

        return asString;
    }
}
