using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

public class WordTableBuilderTests
{
    [Test]
    public async Task RendersTableFromAttributedModel()
    {
        var employees = SampleData.Employees();

        #region WordTableUsage

        var builder = new WordTableBuilder<Employee>(employees);

        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new(new Body());

            var table = builder.Build(mainPart);
            var body = mainPart.Document.Body!;
            body.Append(table);

            #endregion

            body.Append(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 12240,
                        Height = 15840
                    },
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440,
                        Bottom = 1440,
                        Left = 1440,
                        Header = 720,
                        Footer = 720
                    }));
        }

        stream.Position = 0;
        await Verify(stream, "docx");
    }

    [Test]
    public async Task HeaderRowIsBoldAndLeftAligned()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees());
        var table = builder.Build();

        var headerRow = table.Elements<TableRow>().First();
        var headerParagraph = headerRow.Elements<TableCell>().First().GetFirstChild<Paragraph>()!;
        var justification = headerParagraph.ParagraphProperties!.GetFirstChild<Justification>()!;
        await Assert.That(justification.Val?.Value).IsEqualTo(JustificationValues.Left);

        var headerRun = headerParagraph.GetFirstChild<Run>()!;
        await Assert.That(headerRun.RunProperties!.GetFirstChild<Bold>()).IsNotNull();
    }

    [Test]
    public async Task HeaderRowRepeatsAcrossPages()
    {
        var table = new WordTableBuilder<Employee>(SampleData.Employees()).Build();

        var rows = table.Elements<TableRow>().ToList();
        await Assert.That(rows[0].GetFirstChild<TableRowProperties>()?.GetFirstChild<TableHeader>()).IsNotNull();

        // Only the heading repeats. Marking a data row would repeat it on every page too.
        foreach (var row in rows.Skip(1))
        {
            await Assert.That(row.GetFirstChild<TableRowProperties>()?.GetFirstChild<TableHeader>()).IsNull();
        }
    }

    [Test]
    public async Task DataRowsMatchEmployeeCount()
    {
        var employees = SampleData.Employees();
        var table = new WordTableBuilder<Employee>(employees).Build();

        var rows = table.Elements<TableRow>().ToList();
        // +1 for header row
        await Assert.That(rows.Count).IsEqualTo(employees.Count + 1);
    }

    [Test]
    public async Task ColumnHeadingsHonorColumnAttribute()
    {
        var table = new WordTableBuilder<Employee>([]).Build();
        var headerCells = table.Elements<TableRow>().First().Elements<TableCell>().ToList();
        var headings = headerCells
            .Select(_ => _.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.GetFirstChild<Text>()!.Text)
            .ToList();

        // Employee model declares Order=1..5 with explicit Headings; IsActive/Status fall after.
        await Assert.That(headings[0]).IsEqualTo("Employee ID");
        await Assert.That(headings[1]).IsEqualTo("Full Name");
        await Assert.That(headings[2]).IsEqualTo("Email Address");
    }

    public class LinkRow
    {
        public required string Label { get; init; }
        public required Link Site { get; init; }
    }

    [Test]
    public async Task LinkValueProducesHyperlinkWhenMainPartGiven()
    {
        var rows = new[]
        {
            new LinkRow
            {
                Label = "Excelsior",
                Site = new("http://github.com/Papyrine/Excelsior", "Home")
            }
        };

        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());

        var table = new WordTableBuilder<LinkRow>(rows).Build(mainPart);

        var cells = table.Elements<TableRow>()
            .Skip(1)
            .First()
            .Elements<TableCell>()
            .ToList();
        var linkCell = cells[1];
        var hyperlink = linkCell
            .GetFirstChild<Paragraph>()!
            .GetFirstChild<Hyperlink>();
        await Assert.That(hyperlink).IsNotNull();

        var rel = mainPart.HyperlinkRelationships.Single();
        await Assert.That(rel.Uri.ToString()).IsEqualTo("http://github.com/Papyrine/Excelsior");
        await Assert.That(hyperlink!.Id?.Value).IsEqualTo(rel.Id);

        var run = hyperlink.GetFirstChild<Run>()!;
        await Assert.That(run.GetFirstChild<Text>()!.Text).IsEqualTo("Home");
        await Assert.That(run.RunProperties!.GetFirstChild<Color>()).IsNotNull();
        await Assert.That(run.RunProperties.GetFirstChild<Underline>()).IsNotNull();
    }

    [Test]
    public async Task LinkValueFallsBackToTextWhenMainPartOmitted()
    {
        var rows = new[]
        {
            new LinkRow
            {
                Label = "Excelsior",
                Site = new("http://github.com/Papyrine/Excelsior", "Home")
            }
        };

        var table = new WordTableBuilder<LinkRow>(rows).Build();

        var cells = table
            .Elements<TableRow>()
            .Skip(1)
            .First()
            .Elements<TableCell>()
            .ToList();
        var linkCell = cells[1];
        var paragraph = linkCell.GetFirstChild<Paragraph>()!;
        await Assert.That(paragraph.GetFirstChild<Hyperlink>()).IsNull();

        var run = paragraph.GetFirstChild<Run>()!;
        await Assert.That(run.GetFirstChild<Text>()!.Text).IsEqualTo("Home");
    }

    public record HtmlRow
    {
        [Column(IsHtml = true)]
        public required string Name { get; init; }
    }

    [Test]
    public async Task IsHtmlColumnRendersInlineFormattingAsRunProperties()
    {
        var rows = new[]
        {
            new HtmlRow
            {
                Name = "<i>A. Smith</i>"
            }
        };

        var table = new WordTableBuilder<HtmlRow>(rows).Build();

        var dataCell = table.Elements<TableRow>().Skip(1).First().GetFirstChild<TableCell>()!;
        var paragraph = dataCell.GetFirstChild<Paragraph>()!;
        var run = paragraph.GetFirstChild<Run>()!;
        await Assert.That(run.RunProperties!.GetFirstChild<Italic>()).IsNotNull();
        await Assert.That(run.GetFirstChild<Text>()!.Text).IsEqualTo("A. Smith");
    }

    [Test]
    public async Task FormulaColumnThrows()
    {
        var employees = SampleData.Employees();
        var builder = new WordTableBuilder<Employee>(employees)
            .Column(
                _ => _.Salary,
                _ => _.Formula = (employee, context) =>
                    $"={context.Ref(_ => _.Id)} * 10000");

        var exception = await Assert.That(() => builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Formula");
        await Assert.That(exception.Message).Contains("not supported in Word tables");
    }

    [Test]
    public Task TableLevelHeadingStyleAppliesShadingAndFontToEveryHeaderCell()
    {
        #region WordTableHeadingStyle

        var builder = new WordTableBuilder<Employee>(
            SampleData.Employees(),
            _ =>
            {
                _.BackgroundColor = "4472C4";
                _.Font.Color = "FFFFFF";
                _.Font.Name = "Arial";
                _.Font.Size = 12;
                _.Font.Underline = true;
            });

        #endregion

        return VerifyTable(builder);
    }

    [Test]
    public Task ColumnHeadingStyleOverridesTableHeadingStyle()
    {
        #region WordTableColumnHeadingStyle

        var builder = new WordTableBuilder<Employee>(
                SampleData.Employees(),
                _ => _.BackgroundColor = "000000")
            .Column(
                _ => _.Name,
                _ => _.HeadingStyle = cell => cell.BackgroundColor = "FF0000");

        #endregion

        return VerifyTable(builder);
    }

    [Test]
    public async Task HeadingBackgroundAcceptsLeadingHash()
    {
        var builder = new WordTableBuilder<Employee>(
            SampleData.Employees(),
            _ => _.BackgroundColor = "#ABCDEF");

        await VerifyTable(builder);
    }

    static async Task VerifyTable<T>(WordTableBuilder<T> builder)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new(new Body());

            var table = builder.Build(mainPart);
            var body = mainPart.Document.Body!;
            body.Append(table);
            body.Append(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 12240,
                        Height = 15840
                    },
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440,
                        Bottom = 1440,
                        Left = 1440,
                        Header = 720,
                        Footer = 720
                    }));
        }

        stream.Position = 0;
        await Verify(stream, "docx");
    }

    [Test]
    public async Task StandaloneTableCarriesInlineBordersAndFullWidth()
    {
        // Without a MainDocumentPart there's no styles part to add TableGrid to, so the renderer
        // falls back to inline borders. tblW pct=5000 is always emitted so the table fills the
        // content area regardless of where it's appended.
        var table = new WordTableBuilder<Employee>([]).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        await Assert.That(props.GetFirstChild<TableBorders>()).IsNotNull();
        await Assert.That(props.GetFirstChild<TableCellMarginDefault>()).IsNotNull();
        await Assert.That(props.GetFirstChild<TableStyle>()).IsNull();

        var width = props.GetFirstChild<TableWidth>()!;
        await Assert.That(width.Width?.Value).IsEqualTo("5000");
        await Assert.That(width.Type?.Value).IsEqualTo(TableWidthUnitValues.Pct);

        var look = props.GetFirstChild<TableLook>()!;
        await Assert.That(look.FirstRow?.Value).IsTrue();
        await Assert.That(look.NoVerticalBand?.Value).IsTrue();
    }

    [Test]
    public async Task HostBuiltTableReferencesTableGridStyleAndIsFullWidth()
    {
        // Build(mainPart) emits a tblStyle reference to the built-in TableGrid style and the
        // helper inserts the style definition into the host's styles part if it isn't already
        // there — Word's own behavior when a table is inserted via the ribbon.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());

        var table = new WordTableBuilder<Employee>([]).Build(mainPart);
        var props = table.GetFirstChild<TableProperties>()!;

        var tableStyle = props.GetFirstChild<TableStyle>()!;
        await Assert.That(tableStyle.Val?.Value).IsEqualTo("TableGrid");

        var width = props.GetFirstChild<TableWidth>()!;
        await Assert.That(width.Width?.Value).IsEqualTo("5000");
        await Assert.That(width.Type?.Value).IsEqualTo(TableWidthUnitValues.Pct);

        // No inline borders/margins when a tblStyle is referenced — the style owns them.
        await Assert.That(props.GetFirstChild<TableBorders>()).IsNull();
        await Assert.That(props.GetFirstChild<TableCellMarginDefault>()).IsNull();

        var styles = mainPart.StyleDefinitionsPart!.Styles!.Elements<Style>().ToList();
        var tableGrid = styles.Single(_ => _.StyleId?.Value == "TableGrid");
        await Assert.That(tableGrid.Type?.Value).IsEqualTo(StyleValues.Table);
        await Assert.That(tableGrid.Descendants<TableBorders>().FirstOrDefault()).IsNotNull();
    }

    [Test]
    public async Task NamedTableStyleReplacesTableGrid()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());

        // Literal rather than the constant below: this is the readme's snippet, and a style id is
        // the one thing a reader needs to see spelled out.
        #region WordTableStyle

        var table = new WordTableBuilder<Employee>([])
            .TableStyle("LinedColumns")
            .Build(mainPart);

        #endregion

        var props = table.GetFirstChild<TableProperties>()!;
        await Assert.That(props.GetFirstChild<TableStyle>()!.Val?.Value).IsEqualTo(linedColumnsStyleId);

        // The style belongs to the host document. TableGrid is inserted when missing because it is
        // a Word built-in with a known definition; inventing one for a template's own style would
        // style the table as something other than what the template says.
        var styles = mainPart.StyleDefinitionsPart?.Styles?.Elements<Style>().ToList() ?? [];
        await Assert.That(styles.Any(_ => _.StyleId?.Value == linedColumnsStyleId)).IsFalse();
        await Assert.That(styles.Any(_ => _.StyleId?.Value == "TableGrid")).IsFalse();
    }

    // The rendered file, so the style's effect is visible rather than inferred from a tblStyle
    // reference: column rules only, no row rules, which is what a lined-columns look is and what a
    // TableGrid table is not.
    [Test]
    public async Task NamedTableStyleRendersTheTemplatesLook()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .TableStyle(linedColumnsStyleId);

        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new(new Body());

            AddLinedColumnsStyle(mainPart);

            var body = mainPart.Document.Body!;
            body.Append(builder.Build(mainPart));
            body.Append(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 12240,
                        Height = 15840
                    },
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440,
                        Bottom = 1440,
                        Left = 1440,
                        Header = 720,
                        Footer = 720
                    }));
        }

        stream.Position = 0;
        await Verify(stream, "docx");
    }

    const string linedColumnsStyleId = "LinedColumns";

    /// <summary>
    /// A table style a template might define: vertical rules between columns, a rule under the
    /// header, and nothing else. Deliberately unlike <c>TableGrid</c>, so a render that ignored
    /// the style would be obvious on sight.
    /// </summary>
    static void AddLinedColumnsStyle(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new(
            new Style(
                new StyleName
                {
                    Val = "Lined Columns"
                },
                new TableProperties(
                    new TableBorders(
                        new BottomBorder
                        {
                            Val = BorderValues.Single,
                            Size = 8,
                            Color = "C00000"
                        },
                        new InsideVerticalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 8,
                            Color = "C00000"
                        }),
                    new TableCellMarginDefault(
                        new TopMargin
                        {
                            Width = "60",
                            Type = TableWidthUnitValues.Dxa
                        },
                        new BottomMargin
                        {
                            Width = "60",
                            Type = TableWidthUnitValues.Dxa
                        })))
            {
                Type = StyleValues.Table,
                StyleId = linedColumnsStyleId
            });
    }

    [Test]
    public async Task NamedTableStyleKeepsColumnWidths()
    {
        // The style drives borders and margins; widths stay the caller's, so the two compose.
        var table = new WordTableBuilder<Employee>(SampleData.Employees())
            .TableStyle(linedColumnsStyleId)
            .Column(_ => _.Name, _ => _.Width = 40)
            .Build();

        var props = table.GetFirstChild<TableProperties>()!;
        await Assert.That(props.GetFirstChild<TableLayout>()!.Type?.Value).IsEqualTo(TableLayoutValues.Fixed);
        await Assert.That(table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().Any(_ => _.Width != null)).IsTrue();
    }

    [Test]
    public async Task EnsureTableGridStyleIsIdempotent_AcrossMultipleBuilds()
    {
        // Building two tables against the same host must not duplicate the TableGrid definition.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());

        new WordTableBuilder<Employee>([]).Build(mainPart);
        new WordTableBuilder<Employee>([]).Build(mainPart);

        var tableGridCount = mainPart.StyleDefinitionsPart!.Styles!
            .Elements<Style>()
            .Count(_ => _.StyleId?.Value == "TableGrid");
        await Assert.That(tableGridCount).IsEqualTo(1);
    }

    [Test]
    public async Task EnsureTableGridStyleAddsStockTableNormalWithCellMarginsWhenHostHasNone()
    {
        // TableGrid inherits its cell padding from TableNormal via basedOn. A programmatically
        // built host has no styles part at all — so the helper must add a stock TableNormal
        // (matching what Word ships) so the rendered table picks up the expected 108dxa
        // left/right cell padding.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());

        new WordTableBuilder<Employee>([]).Build(mainPart);

        var tableNormal = mainPart.StyleDefinitionsPart!.Styles!
            .Elements<Style>()
            .Single(_ => _.StyleId?.Value == "TableNormal");
        await Assert.That(tableNormal.Default?.Value).IsTrue();

        var cellMargins = tableNormal.Descendants<TableCellMarginDefault>().Single();
        await Assert.That(cellMargins.GetFirstChild<StartMargin>()!.Width?.Value).IsEqualTo("108");
        await Assert.That(cellMargins.GetFirstChild<EndMargin>()!.Width?.Value).IsEqualTo("108");
    }

    [Test]
    public async Task EnsureTableGridStyleLeavesPreExistingTableNormalUntouched()
    {
        // A Word-authored host always ships TableNormal in its styles part — sometimes with
        // customizations the template author intentionally made (different cell margins, etc.).
        // The helper must never replace, duplicate, or strip those.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new(
            new Style(
                new StyleName
                {
                    Val = "Normal Table"
                },
                new TableProperties(
                    new TableCellMarginDefault(
                        new TopMargin
                        {
                            Width = "20",
                            Type = TableWidthUnitValues.Dxa
                        },
                        new StartMargin
                        {
                            Width = "200",
                            Type = TableWidthUnitValues.Dxa
                        },
                        new BottomMargin
                        {
                            Width = "20",
                            Type = TableWidthUnitValues.Dxa
                        },
                        new EndMargin
                        {
                            Width = "200",
                            Type = TableWidthUnitValues.Dxa
                        })))
            {
                Type = StyleValues.Table,
                StyleId = "TableNormal",
                Default = true,
            });
        var preExisting = stylesPart.Styles.Elements<Style>().Single(_ => _.StyleId?.Value == "TableNormal");

        new WordTableBuilder<Employee>([]).Build(mainPart);

        var tableNormals = stylesPart.Styles.Elements<Style>().Where(_ => _.StyleId?.Value == "TableNormal").ToList();
        await Assert.That(tableNormals.Count).IsEqualTo(1);
        await Assert.That(tableNormals[0]).IsSameReferenceAs(preExisting);
        var cellMargins = tableNormals[0].Descendants<TableCellMarginDefault>().Single();
        await Assert.That(cellMargins.GetFirstChild<StartMargin>()!.Width?.Value).IsEqualTo("200");
    }

    [Test]
    public async Task EnsureTableGridStyleIsIdempotent_LeavesPreExistingTableGridUntouched()
    {
        // A template authored in Word with tables already present ships TableGrid in styles.xml.
        // Build(mainPart) must detect the existing definition and leave it alone — not replace,
        // duplicate, or strip any customizations the template author made to the style.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new(new Body());
        AddCustomizedTableGridStyle(mainPart);

        var preExisting = mainPart.StyleDefinitionsPart!.Styles!
            .Elements<Style>()
            .Single(_ => _.StyleId?.Value == "TableGrid");

        new WordTableBuilder<Employee>([]).Build(mainPart);

        var styles = mainPart.StyleDefinitionsPart.Styles
            .Elements<Style>()
            .Where(_ => _.StyleId?.Value == "TableGrid")
            .ToList();
        await Assert.That(styles.Count).IsEqualTo(1);
        // Same instance — confirms no replacement happened, just left in place.
        await Assert.That(styles[0]).IsSameReferenceAs(preExisting);
        // Customizations remain intact.
        var borders = styles[0].Descendants<TableBorders>().Single();
        await Assert.That(borders.GetFirstChild<TopBorder>()!.Val?.Value).IsEqualTo(BorderValues.Double);
        await Assert.That(borders.GetFirstChild<TopBorder>()!.Color?.Value).IsEqualTo("1F4E79");
    }

    [Test]
    public async Task InheritsBordersFromHostCustomizedTableGrid()
    {
        // The supported way to rebrand Excelsior tables is to customize TableGrid in the host
        // template — Excelsior emits a tblStyle reference, so any borders/cell-margin overrides
        // declared on TableGrid in the host's styles part flow straight through.
        var builder = new WordTableBuilder<Employee>(SampleData.Employees());
        await VerifyTableInDocWithCustomizedTableGrid(builder);
    }

    static void AddCustomizedTableGridStyle(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new(
            new Style(
                new StyleName
                {
                    Val = "Table Grid"
                },
                new TableProperties(
                    new TableBorders(
                        new TopBorder
                        {
                            Val = BorderValues.Double,
                            Size = 12,
                            Color = "1F4E79"
                        },
                        new LeftBorder
                        {
                            Val = BorderValues.Double,
                            Size = 12,
                            Color = "1F4E79"
                        },
                        new BottomBorder
                        {
                            Val = BorderValues.Double,
                            Size = 12,
                            Color = "1F4E79"
                        },
                        new RightBorder
                        {
                            Val = BorderValues.Double,
                            Size = 12,
                            Color = "1F4E79"
                        },
                        new InsideHorizontalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4,
                            Color = "1F4E79"
                        },
                        new InsideVerticalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4,
                            Color = "1F4E79"
                        }),
                    new TableCellMarginDefault(
                        new TopMargin
                        {
                            Width = "60",
                            Type = TableWidthUnitValues.Dxa
                        },
                        new BottomMargin
                        {
                            Width = "60",
                            Type = TableWidthUnitValues.Dxa
                        })),
                new TableStyleProperties(
                    new RunPropertiesBaseStyle(
                        new Bold(),
                        new Color
                        {
                            Val = "FFFFFF"
                        }),
                    new TableStyleConditionalFormattingTableCellProperties(
                        new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Color = "auto",
                            Fill = "1F4E79"
                        }))
                {
                    Type = TableStyleOverrideValues.FirstRow
                })
            {
                Type = StyleValues.Table,
                StyleId = "TableGrid",
            });
    }

    static async Task VerifyTableInDocWithCustomizedTableGrid(WordTableBuilder<Employee> builder)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new(new Body());

            AddCustomizedTableGridStyle(mainPart);

            var table = builder.Build(mainPart);
            var body = mainPart.Document.Body!;
            body.Append(table);
            body.Append(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 12240,
                        Height = 15840
                    },
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440,
                        Bottom = 1440,
                        Left = 1440,
                        Header = 720,
                        Footer = 720
                    }));
        }

        stream.Position = 0;
        await Verify(stream, "docx");
    }

    [Test]
    public async Task BodyStyleAppliesFontAndAlignmentToEveryDataCell()
    {
        #region WordTableBodyStyle

        var builder = new WordTableBuilder<Employee>(
            SampleData.Employees(),
            bodyStyle: _ =>
            {
                _.Font.Size = 9;
                _.Font.Name = "Arial";
            });

        #endregion

        var table = builder.Build();
        var dataRow = table.Elements<TableRow>().Skip(1).First();
        var paragraph = dataRow.GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;

        // Body alignment is emitted on the paragraph.
        await Assert.That(paragraph.ParagraphProperties!.GetFirstChild<Justification>()).IsNotNull();

        var runProperties = paragraph.GetFirstChild<Run>()!.RunProperties!;
        // 9pt -> 18 half-points.
        await Assert.That(runProperties.GetFirstChild<FontSize>()!.Val?.Value).IsEqualTo("18");
        await Assert.That(runProperties.GetFirstChild<RunFonts>()!.Ascii?.Value).IsEqualTo("Arial");
    }

    [Test]
    public async Task ColumnCellStyleAppliesToDataCells()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .Column(
                _ => _.Name,
                _ => _.CellStyle = (cell, _, _) => cell.BackgroundColor = "FFFF00");

        var table = builder.Build();
        var dataRow = table.Elements<TableRow>().Skip(1).First();

        // Id is column 0, Name is column 1 — only Name should be shaded.
        var idCell = dataRow.Elements<TableCell>().ElementAt(0);
        await Assert.That(idCell.TableCellProperties?.GetFirstChild<Shading>()).IsNull();

        var nameCell = dataRow.Elements<TableCell>().ElementAt(1);
        var shading = nameCell.TableCellProperties!.GetFirstChild<Shading>()!;
        await Assert.That(shading.Fill?.Value).IsEqualTo("FFFF00");
    }

    [Test]
    public async Task ColumnCellStyleCanStyleConditionallyOnValue()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .Column(
                _ => _.Salary,
                _ => _.CellStyle = (cell, _, value) =>
                {
                    if (value > 100_000)
                    {
                        cell.Font.Bold = true;
                    }
                });

        var table = builder.Build();
        var employees = SampleData.Employees();
        var salaryIndex = table.Elements<TableRow>()
            .First()
            .Elements<TableCell>()
            .Select(_ => _.InnerText)
            .ToList()
            .IndexOf("Annual Salary");
        await Assert.That(salaryIndex >= 0).IsTrue();

        var dataRows = table.Elements<TableRow>().Skip(1).ToList();
        for (var i = 0; i < employees.Count; i++)
        {
            var salaryCell = dataRows[i].Elements<TableCell>().ElementAt(salaryIndex);
            var bold = salaryCell.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.RunProperties?.GetFirstChild<Bold>();
            if (employees[i].Salary > 100_000)
            {
                await Assert.That(bold).IsNotNull();
            }
            else
            {
                await Assert.That(bold).IsNull();
            }
        }
    }

    [Test]
    public async Task NoBodyStyleLeavesDataCellsBare()
    {
        // Backward compatibility: without bodyStyle or a column CellStyle, data cells carry no
        // paragraph or run properties (a bare run), exactly as before.
        var table = new WordTableBuilder<Employee>(SampleData.Employees()).Build();

        var dataRow = table.Elements<TableRow>().Skip(1).First();
        var paragraph = dataRow.GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        await Assert.That(paragraph.ParagraphProperties).IsNull();
        await Assert.That(paragraph.GetFirstChild<Run>()!.RunProperties).IsNull();
    }

    [Test]
    public async Task ParagraphStylesAppliedToHeaderAndBodyCells()
    {
        #region WordTableParagraphStyles

        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .HeadingParagraphStyle("TBLHeading")
            .BodyParagraphStyle("TBLText");

        #endregion

        var table = builder.Build();
        var rows = table.Elements<TableRow>().ToList();

        var headerParagraph = rows[0].GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        await Assert.That(headerParagraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value).IsEqualTo("TBLHeading");

        var bodyParagraph = rows[1].GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        await Assert.That(bodyParagraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value).IsEqualTo("TBLText");
    }

    [Test]
    public async Task BodyParagraphStyleReachesHtmlCells()
    {
        // A named paragraph style must reach IsHtml cells (unlike the run-level bodyStyle), while
        // leaving the HTML-derived inline formatting intact.
        var rows = new[]
        {
            new HtmlRow
            {
                Name = "<i>A. Smith</i>"
            }
        };

        var table = new WordTableBuilder<HtmlRow>(rows)
            .BodyParagraphStyle("TBLText")
            .Build();

        var paragraph = table.Elements<TableRow>().Skip(1).First().GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        await Assert.That(paragraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value).IsEqualTo("TBLText");
        await Assert.That(paragraph.GetFirstChild<Run>()!.RunProperties!.GetFirstChild<Italic>()).IsNotNull();
    }

    public class WidthRow
    {
        [Column(Heading = "A", Order = 1, Width = 10)]
        public required string Fixed { get; init; }

        [Column(Heading = "B", Order = 2, MaxWidth = 8)]
        public required string Clamped { get; init; }

        [Column(Heading = "C", Order = 3, MinWidth = 30)]
        public required string Raised { get; init; }
    }

    static WidthRow[] WidthRows() =>
    [
        new()
        {
            Fixed = "short",
            // Long enough that auto-sizing would far exceed the MaxWidth of 8.
            Clamped = "a deliberately very long value that would auto-size wide",
            Raised = "x"
        }
    ];

    [Test]
    public async Task WidthHintSwitchesTableToFixedDxaLayout()
    {
        var table = new WordTableBuilder<WidthRow>(WidthRows()).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        // A width hint flips the table from the default pct auto-layout to fixed dxa layout.
        var width = props.GetFirstChild<TableWidth>()!;
        await Assert.That(width.Type?.Value).IsEqualTo(TableWidthUnitValues.Dxa);

        var layout = props.GetFirstChild<TableLayout>()!;
        await Assert.That(layout.Type?.Value).IsEqualTo(TableLayoutValues.Fixed);

        // tblW is the sum of the grid column widths.
        var gridWidths = table.GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => int.Parse(_.Width!.Value!))
            .ToList();
        await Assert.That(int.Parse(width.Width!.Value!)).IsEqualTo(gridWidths.Sum());
    }

    [Test]
    public async Task WidthHintsResolveExplicitClampedAndRaisedColumns()
    {
        var table = new WordTableBuilder<WidthRow>(WidthRows()).Build();
        var gridWidths = table.GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => int.Parse(_.Width!.Value!))
            .ToList();

        // chars -> twips is (chars * 7 + 5) * 15.
        // Fixed: explicit Width = 10 -> (10*7+5)*15 = 1125.
        await Assert.That(gridWidths[0]).IsEqualTo(1125);
        // Clamped: long content auto-sizes wide but MaxWidth = 8 caps it -> (8*7+5)*15 = 915.
        await Assert.That(gridWidths[1]).IsEqualTo(915);
        // Raised: tiny content but MinWidth = 30 floors it -> (30*7+5)*15 = 3225.
        await Assert.That(gridWidths[2]).IsEqualTo(3225);
    }

    [Test]
    public async Task NoWidthHintKeepsAutoLayoutAndBareGridColumns()
    {
        // Regression: a model without any Width/MinWidth/MaxWidth keeps the prior output — a
        // pct=5000 width, no tblLayout, and grid columns with no explicit width.
        var table = new WordTableBuilder<Employee>(SampleData.Employees()).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        var width = props.GetFirstChild<TableWidth>()!;
        await Assert.That(width.Type?.Value).IsEqualTo(TableWidthUnitValues.Pct);
        await Assert.That(width.Width?.Value).IsEqualTo("5000");
        await Assert.That(props.GetFirstChild<TableLayout>()).IsNull();

        var gridColumns = table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().ToList();
        await Assert.That(gridColumns.All(_ => _.Width == null)).IsTrue();
    }

    [Test]
    public Task RendersTableWithColumnWidths()
    {
        // Snapshot showing the three width behaviours side by side: an explicit Width = 10 column,
        // a MaxWidth = 8 column whose long content wraps within the cap, and a MinWidth = 30 column
        // that stays wide despite tiny content. A width hint also flips the table to fixed layout.
        var rows = new[]
        {
            new WidthRow
            {
                Fixed = "Alpha",
                Clamped = "United Kingdom, France, Japan, United Arab Emirates, Canada",
                Raised = "x"
            },
            new WidthRow
            {
                Fixed = "Beta",
                Clamped = "Japan",
                Raised = "y"
            }
        };

        return VerifyTable(new WordTableBuilder<WidthRow>(rows));
    }

    [Test]
    public async Task FluentColumnConfigurationOverridesHeading()
    {
        var builder = new WordTableBuilder<Employee>([])
            .Column(
                _ => _.Name,
                _ => _.Heading = "Person");

        var table = builder.Build();
        var headerCells = table.Elements<TableRow>().First().Elements<TableCell>().ToList();
        var headings = headerCells
            .Select(_ => _.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.GetFirstChild<Text>()!.Text)
            .ToList();

        await Assert.That(headings.Contains("Person")).IsTrue();
        await Assert.That(headings.Contains("Full Name")).IsFalse();
    }

    static WordTableBuilder<Employee> RichlyStyledTable() =>
        new(
            SampleData.Employees(),
            bodyStyle: _ =>
            {
                _.Font.Bold = true;
                _.Font.Underline = true;
                _.Font.Color = "FF0000";
                _.Font.Size = 14;
                _.Font.Name = "Arial";
            });

    [Test]
    public async Task RunPropertiesFollowSchemaOrder()
    {
        // CT_RPr requires rFonts, b, color, sz, szCs, u in that order. Emitting them out of order
        // (e.g. underline before colour, or rFonts last) makes Word flag the document as corrupt.
        var runProperties = RichlyStyledTable()
            .Build()
            .Elements<TableRow>()
            .Skip(1)
            .First()
            .GetFirstChild<TableCell>()!
            .GetFirstChild<Paragraph>()!
            .GetFirstChild<Run>()!
            .RunProperties!;

        var order = runProperties.ChildElements.Select(_ => _.GetType()).ToList();
        await Assert.That(order).IsEquivalentTo(
            [
                typeof(RunFonts),
                typeof(Bold),
                typeof(Color),
                typeof(FontSize),
                typeof(FontSizeComplexScript),
                typeof(Underline)
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task StandaloneTableBordersFollowSchemaOrder()
    {
        // CT_TblBorders requires top, left, bottom, right, insideH, insideV. The standalone
        // (no MainDocumentPart) path emits these inline, so an out-of-order set corrupts the table.
        var borders = new WordTableBuilder<Employee>(SampleData.Employees())
            .Build()
            .GetFirstChild<TableProperties>()!
            .GetFirstChild<TableBorders>()!;

        var order = borders.ChildElements.Select(_ => _.GetType()).ToList();
        await Assert.That(order).IsEquivalentTo(
            [
                typeof(TopBorder),
                typeof(LeftBorder),
                typeof(BottomBorder),
                typeof(RightBorder),
                typeof(InsideHorizontalBorder),
                typeof(InsideVerticalBorder)
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task EightDigitArgbIsSchemaValidInWord()
    {
        // CellStyle accepts AARRGGBB — StyleManager prepends the alpha only when it is missing, and
        // the readme's own TemplateSheetFullFeatured sample passes FFEFEFEF. Word has nowhere to
        // put the alpha: w:shd/@w:fill and w:color/@w:val are both ST_HexColor, six digits or
        // "auto". Before the colour went through a parser this reached the file unchanged and
        // produced fourteen validation errors.
        var builder = new WordTableBuilder<Employee>(
            SampleData.Employees(),
            _ =>
            {
                _.BackgroundColor = "FFEFEFEF";
                _.Font.Color = "FF0563C1";
            });

        var table = builder.Build();

        var fills = table.Descendants<Shading>().Select(_ => _.Fill?.Value).Where(_ => _ != null);
        var colors = table.Descendants<Color>().Select(_ => _.Val?.Value).Where(_ => _ != "auto");
        await Assert.That(fills).All(_ => _ == "EFEFEF");
        await Assert.That(colors).All(_ => _ == "0563C1");

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new(new Body(table, new SectionProperties()));
        }

        stream.Position = 0;
        using var opened = WordprocessingDocument.Open(stream, false);
        var errors = new OpenXmlValidator(FileFormatVersions.Office2019)
            .Validate(opened)
            .Select(_ => _.Description);

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task StandaloneStyledTableIsSchemaValid()
    {
        // End-to-end guard over the inline tblBorders ordering and the rich run-property ordering:
        // a standalone table (inline borders) with a body style that exercises every rPr child.
        // OpenXmlValidator catches an ordering Word would otherwise reject as corrupt.
        var table = RichlyStyledTable().Build();

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new(new Body(table, new SectionProperties()));
        }

        stream.Position = 0;
        using var opened = WordprocessingDocument.Open(stream, false);
        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        var errors = validator
            .Validate(opened)
            .Select(_ => $"{_.Part?.Uri}: {_.Description}");

        await Assert.That(errors).IsEmpty();
    }
}
