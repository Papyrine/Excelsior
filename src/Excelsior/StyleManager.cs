class StyleManager
{
    record FontKey(bool Bold, bool Underline, string? Color, double? Size, string? Name);

    record CellFormatKey(
        uint FontId,
        uint FillId,
        uint? NumberFormatId,
        HorizontalAlignmentValues HAlign,
        VerticalAlignmentValues VAlign,
        bool WrapText,
        bool Locked);

    List<FontKey> fonts = [new(false, false, null, null, null)];

    Dictionary<FontKey, uint> fontIndex = new()
    {
        [new(false, false, null, null, null)] = 0
    };

    List<string?> fills = [null, "gray125"];

    Dictionary<string, uint> fillIndex = [];

    List<string> customNumberFormats = [];
    Dictionary<string, uint> numberFormatIds = [];
    uint nextNumberFormatId = 164;

    List<CellFormatKey> cellFormats =
        [new(0, 0, null, HorizontalAlignmentValues.General, VerticalAlignmentValues.Bottom, false, true)];

    Dictionary<CellFormatKey, uint> cellFormatIndex = [];

    List<string> dxfFills = [];
    Dictionary<string, uint> dxfFillIndex = [];

    internal uint GetOrCreateDxfFillIndex(string color)
    {
        if (dxfFillIndex.TryGetValue(color, out var id))
        {
            return id;
        }

        id = (uint)dxfFills.Count;
        dxfFills.Add(color);
        dxfFillIndex[color] = id;
        return id;
    }

    internal uint GetOrCreateStyleIndex(CellStyle style)
    {
        var fontId = GetOrCreateFontId(style.Font);
        var fillId = GetOrCreateFillId(style.BackgroundColor);
        uint? nfId;
        if (style.NumberFormat == null)
        {
            nfId = null;
        }
        else
        {
            nfId = GetOrCreateNumberFormatId(style.NumberFormat);
        }

        var key = new CellFormatKey(
            fontId,
            fillId,
            nfId,
            style.Alignment.Horizontal,
            style.Alignment.Vertical,
            style.Alignment.WrapText,
            style.Locked);

        if (cellFormatIndex.TryGetValue(key, out var index))
        {
            return index;
        }

        index = (uint)cellFormats.Count;
        cellFormats.Add(key);
        cellFormatIndex[key] = index;
        return index;
    }

    uint GetOrCreateFontId(CellFont font)
    {
        var key = new FontKey(font.Bold, font.Underline, font.Color, font.Size, font.Name);
        if (fontIndex.TryGetValue(key, out var id))
        {
            return id;
        }

        id = (uint)fonts.Count;
        fonts.Add(key);
        fontIndex[key] = id;
        return id;
    }

    uint GetOrCreateFillId(string? backgroundColor)
    {
        if (backgroundColor == null)
        {
            return 0;
        }

        if (fillIndex.TryGetValue(backgroundColor, out var id))
        {
            return id;
        }

        id = (uint)fills.Count;
        fills.Add(backgroundColor);
        fillIndex[backgroundColor] = id;
        return id;
    }

    uint GetOrCreateNumberFormatId(string format)
    {
        if (numberFormatIds.TryGetValue(format, out var id))
        {
            return id;
        }

        id = nextNumberFormatId++;
        numberFormatIds[format] = id;
        customNumberFormats.Add(format);
        return id;
    }

    internal Stylesheet BuildStylesheet()
    {
        var stylesheet = new Stylesheet();

        if (customNumberFormats.Count > 0)
        {
            var nfs = new NumberingFormats
            {
                Count = (uint)customNumberFormats.Count
            };
            uint nfId = 164;
            foreach (var fmt in customNumberFormats)
            {
                nfs.Append(
                    new NumberingFormat
                    {
                        NumberFormatId = nfId++,
                        FormatCode = fmt
                    });
            }

            stylesheet.Append(nfs);
        }

        var fontsEl = new Fonts
        {
            Count = (uint)fonts.Count
        };

        foreach (var fontKey in fonts)
        {
            var font = new Font();
            if (fontKey.Bold)
            {
                font.Append(new Bold());
            }

            if (fontKey.Underline)
            {
                font.Append(new Underline());
            }

            font.Append(
                new FontSize
                {
                    Val = fontKey.Size ?? 11
                });

            // CT_Font requires color before name. A colored font emitted name-first (every link
            // cell is blue + underlined) is schema-invalid and makes Excel offer to "repair".
            if (fontKey.Color != null)
            {
                font.Append(
                    new Color
                    {
                        Rgb = NormaliseColor(fontKey.Color)
                    });
            }

            font.Append(
                new FontName
                {
                    Val = fontKey.Name ?? "Calibri"
                });

            fontsEl.Append(font);
        }

        stylesheet.Append(fontsEl);

        var fillsEl = new Fills
        {
            Count = (uint)fills.Count
        };
        fillsEl.Append(
            new Fill(
                new PatternFill
                {
                    PatternType = PatternValues.None
                }));
        fillsEl.Append(
            new Fill(
                new PatternFill
                {
                    PatternType = PatternValues.Gray125
                }));
        for (var i = 2; i < fills.Count; i++)
        {
            fillsEl.Append(
                new Fill(
                    new PatternFill(
                        new ForegroundColor
                        {
                            Rgb = NormaliseColor(fills[i]!)
                        })
                    {
                        PatternType = PatternValues.Solid
                    }));
        }

        stylesheet.Append(fillsEl);

        var borders = new Borders
        {
            Count = 1
        };
        borders.Append(
            new Border(
                new LeftBorder(),
                new RightBorder(),
                new TopBorder(),
                new BottomBorder(),
                new DiagonalBorder()));
        stylesheet.Append(borders);

        var cellFormatsEl = new CellFormats
        {
            Count = (uint)cellFormats.Count
        };
        foreach (var key in cellFormats)
        {
            var cellFormat = new CellFormat
            {
                FontId = key.FontId,
                FillId = key.FillId,
                BorderId = 0,
                ApplyFont = key.FontId > 0,
                ApplyFill = key.FillId > 0
            };

            if (key.NumberFormatId.HasValue)
            {
                cellFormat.NumberFormatId = key.NumberFormatId.Value;
                cellFormat.ApplyNumberFormat = true;
            }

            if (key.HAlign != HorizontalAlignmentValues.General ||
                key.VAlign != VerticalAlignmentValues.Bottom ||
                key.WrapText)
            {
                cellFormat.ApplyAlignment = true;
                cellFormat.Append(
                    new Alignment
                    {
                        Horizontal = key.HAlign,
                        Vertical = key.VAlign,
                        WrapText = key.WrapText
                    });
            }

            if (!key.Locked)
            {
                cellFormat.ApplyProtection = true;
                cellFormat.Append(
                    new Protection
                    {
                        Locked = false
                    });
            }

            cellFormatsEl.Append(cellFormat);
        }

        stylesheet.Append(cellFormatsEl);

        if (dxfFills.Count > 0)
        {
            var dxfs = new DifferentialFormats
            {
                Count = (uint)dxfFills.Count
            };
            foreach (var color in dxfFills)
            {
                var dxf = new DifferentialFormat();
                dxf.Append(
                    new Fill(
                        new PatternFill(
                            new BackgroundColor
                            {
                                Rgb = NormaliseColor(color)
                            })
                        {
                            PatternType = PatternValues.Solid
                        }));
                dxfs.Append(dxf);
            }

            stylesheet.Append(dxfs);
        }

        return stylesheet;
    }

    // Excel's `rgb` attribute is an 8-digit ARGB hex string. A 6-digit RGB value (with or without a
    // leading '#') is schema-invalid, so strip any '#' and prepend an opaque alpha when one is missing.
    static string NormaliseColor(string color)
    {
        if (color.StartsWith('#'))
        {
            color = color[1..];
        }

        return color.Length == 6 ? $"FF{color}" : color;
    }
}
