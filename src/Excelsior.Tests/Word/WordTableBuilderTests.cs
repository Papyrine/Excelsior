using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

[TestFixture]
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
    public void HeaderRowIsBoldAndLeftAligned()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees());
        var table = builder.Build();

        var headerRow = table.Elements<TableRow>().First();
        var headerParagraph = headerRow.Elements<TableCell>().First().GetFirstChild<Paragraph>()!;
        var justification = headerParagraph.ParagraphProperties!.GetFirstChild<Justification>()!;
        AreEqual(JustificationValues.Left, justification.Val?.Value);

        var headerRun = headerParagraph.GetFirstChild<Run>()!;
        IsNotNull(headerRun.RunProperties!.GetFirstChild<Bold>());
    }

    [Test]
    public void DataRowsMatchEmployeeCount()
    {
        var employees = SampleData.Employees();
        var table = new WordTableBuilder<Employee>(employees).Build();

        var rows = table.Elements<TableRow>().ToList();
        // +1 for header row
        AreEqual(employees.Count + 1, rows.Count);
    }

    [Test]
    public void ColumnHeadingsHonorColumnAttribute()
    {
        var table = new WordTableBuilder<Employee>([]).Build();
        var headerCells = table.Elements<TableRow>().First().Elements<TableCell>().ToList();
        var headings = headerCells
            .Select(_ => _.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.GetFirstChild<Text>()!.Text)
            .ToList();

        // Employee model declares Order=1..5 with explicit Headings; IsActive/Status fall after.
        AreEqual("Employee ID", headings[0]);
        AreEqual("Full Name", headings[1]);
        AreEqual("Email Address", headings[2]);
    }

    public class LinkRow
    {
        public required string Label { get; init; }
        public required Link Site { get; init; }
    }

    [Test]
    public void LinkValueProducesHyperlinkWhenMainPartGiven()
    {
        var rows = new[]
        {
            new LinkRow
            {
                Label = "Excelsior",
                Site = new("http://github.com/SimonCropp/Excelsior", "Home")
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
        IsNotNull(hyperlink);

        var rel = mainPart.HyperlinkRelationships.Single();
        AreEqual("http://github.com/SimonCropp/Excelsior", rel.Uri.ToString());
        AreEqual(rel.Id, hyperlink!.Id?.Value);

        var run = hyperlink.GetFirstChild<Run>()!;
        AreEqual("Home", run.GetFirstChild<Text>()!.Text);
        IsNotNull(run.RunProperties!.GetFirstChild<Color>());
        IsNotNull(run.RunProperties.GetFirstChild<Underline>());
    }

    [Test]
    public void LinkValueFallsBackToTextWhenMainPartOmitted()
    {
        var rows = new[]
        {
            new LinkRow
            {
                Label = "Excelsior",
                Site = new("http://github.com/SimonCropp/Excelsior", "Home")
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
        IsNull(paragraph.GetFirstChild<Hyperlink>());

        var run = paragraph.GetFirstChild<Run>()!;
        AreEqual("Home", run.GetFirstChild<Text>()!.Text);
    }

    public record HtmlRow
    {
        [Column(IsHtml = true)]
        public required string Name { get; init; }
    }

    [Test]
    public void IsHtmlColumnRendersInlineFormattingAsRunProperties()
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
        IsNotNull(run.RunProperties!.GetFirstChild<Italic>());
        AreEqual("A. Smith", run.GetFirstChild<Text>()!.Text);
    }

    [Test]
    public void FormulaColumnThrows()
    {
        var employees = SampleData.Employees();
        var builder = new WordTableBuilder<Employee>(employees)
            .Column(
                _ => _.Salary,
                _ => _.Formula = (employee, context) =>
                    $"={context.Ref(_ => _.Id)} * 10000");

        var exception = Assert.Throws<Exception>(() => builder.Build());
        Assert.That(exception!.Message, Does.Contain("Formula"));
        Assert.That(exception.Message, Does.Contain("not supported in Word tables"));
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
    public Task HeadingBackgroundAcceptsLeadingHash()
    {
        var builder = new WordTableBuilder<Employee>(
            SampleData.Employees(),
            _ => _.BackgroundColor = "#ABCDEF");

        return VerifyTable(builder);
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
    public void StandaloneTableCarriesInlineBordersAndFullWidth()
    {
        // Without a MainDocumentPart there's no styles part to add TableGrid to, so the renderer
        // falls back to inline borders. tblW pct=5000 is always emitted so the table fills the
        // content area regardless of where it's appended.
        var table = new WordTableBuilder<Employee>([]).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        IsNotNull(props.GetFirstChild<TableBorders>());
        IsNotNull(props.GetFirstChild<TableCellMarginDefault>());
        IsNull(props.GetFirstChild<TableStyle>());

        var width = props.GetFirstChild<TableWidth>()!;
        AreEqual("5000", width.Width?.Value);
        AreEqual(TableWidthUnitValues.Pct, width.Type?.Value);

        var look = props.GetFirstChild<TableLook>()!;
        AreEqual(true, look.FirstRow?.Value);
        AreEqual(true, look.NoVerticalBand?.Value);
    }

    [Test]
    public void HostBuiltTableReferencesTableGridStyleAndIsFullWidth()
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
        AreEqual("TableGrid", tableStyle.Val?.Value);

        var width = props.GetFirstChild<TableWidth>()!;
        AreEqual("5000", width.Width?.Value);
        AreEqual(TableWidthUnitValues.Pct, width.Type?.Value);

        // No inline borders/margins when a tblStyle is referenced — the style owns them.
        IsNull(props.GetFirstChild<TableBorders>());
        IsNull(props.GetFirstChild<TableCellMarginDefault>());

        var styles = mainPart.StyleDefinitionsPart!.Styles!.Elements<Style>().ToList();
        var tableGrid = styles.Single(_ => _.StyleId?.Value == "TableGrid");
        AreEqual(StyleValues.Table, tableGrid.Type?.Value);
        IsNotNull(tableGrid.Descendants<TableBorders>().FirstOrDefault());
    }

    [Test]
    public void EnsureTableGridStyleIsIdempotent_AcrossMultipleBuilds()
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
        AreEqual(1, tableGridCount);
    }

    [Test]
    public void EnsureTableGridStyleAddsStockTableNormalWithCellMarginsWhenHostHasNone()
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
        AreEqual(true, tableNormal.Default?.Value);

        var cellMargins = tableNormal.Descendants<TableCellMarginDefault>().Single();
        AreEqual("108", cellMargins.GetFirstChild<StartMargin>()!.Width?.Value);
        AreEqual("108", cellMargins.GetFirstChild<EndMargin>()!.Width?.Value);
    }

    [Test]
    public void EnsureTableGridStyleLeavesPreExistingTableNormalUntouched()
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
        AreEqual(1, tableNormals.Count);
        AreSame(preExisting, tableNormals[0]);
        var cellMargins = tableNormals[0].Descendants<TableCellMarginDefault>().Single();
        AreEqual("200", cellMargins.GetFirstChild<StartMargin>()!.Width?.Value);
    }

    [Test]
    public void EnsureTableGridStyleIsIdempotent_LeavesPreExistingTableGridUntouched()
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
        AreEqual(1, styles.Count);
        // Same instance — confirms no replacement happened, just left in place.
        AreSame(preExisting, styles[0]);
        // Customizations remain intact.
        var borders = styles[0].Descendants<TableBorders>().Single();
        AreEqual(BorderValues.Double, borders.GetFirstChild<TopBorder>()!.Val?.Value);
        AreEqual("1F4E79", borders.GetFirstChild<TopBorder>()!.Color?.Value);
    }

    [Test]
    public Task InheritsBordersFromHostCustomizedTableGrid()
    {
        // The supported way to rebrand Excelsior tables is to customize TableGrid in the host
        // template — Excelsior emits a tblStyle reference, so any borders/cell-margin overrides
        // declared on TableGrid in the host's styles part flow straight through.
        var builder = new WordTableBuilder<Employee>(SampleData.Employees());
        return VerifyTableInDocWithCustomizedTableGrid(builder);
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
                        new BottomBorder
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
                        new RightBorder
                        {
                            Val = BorderValues.Double,
                            Size = 12,
                            Color = "1F4E79"
                        },
                        new InsideHorizontalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4, Color = "1F4E79"
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
    public void BodyStyleAppliesFontAndAlignmentToEveryDataCell()
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
        IsNotNull(paragraph.ParagraphProperties!.GetFirstChild<Justification>());

        var runProperties = paragraph.GetFirstChild<Run>()!.RunProperties!;
        // 9pt -> 18 half-points.
        AreEqual("18", runProperties.GetFirstChild<FontSize>()!.Val?.Value);
        AreEqual("Arial", runProperties.GetFirstChild<RunFonts>()!.Ascii?.Value);
    }

    [Test]
    public void ColumnCellStyleAppliesToDataCells()
    {
        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .Column(
                _ => _.Name,
                _ => _.CellStyle = (cell, _, _) => cell.BackgroundColor = "FFFF00");

        var table = builder.Build();
        var dataRow = table.Elements<TableRow>().Skip(1).First();

        // Id is column 0, Name is column 1 — only Name should be shaded.
        var idCell = dataRow.Elements<TableCell>().ElementAt(0);
        IsNull(idCell.TableCellProperties?.GetFirstChild<Shading>());

        var nameCell = dataRow.Elements<TableCell>().ElementAt(1);
        var shading = nameCell.TableCellProperties!.GetFirstChild<Shading>()!;
        AreEqual("FFFF00", shading.Fill?.Value);
    }

    [Test]
    public void ColumnCellStyleCanStyleConditionallyOnValue()
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
        IsTrue(salaryIndex >= 0);

        var dataRows = table.Elements<TableRow>().Skip(1).ToList();
        for (var i = 0; i < employees.Count; i++)
        {
            var salaryCell = dataRows[i].Elements<TableCell>().ElementAt(salaryIndex);
            var bold = salaryCell.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.RunProperties?.GetFirstChild<Bold>();
            if (employees[i].Salary > 100_000)
            {
                IsNotNull(bold);
            }
            else
            {
                IsNull(bold);
            }
        }
    }

    [Test]
    public void NoBodyStyleLeavesDataCellsBare()
    {
        // Backward compatibility: without bodyStyle or a column CellStyle, data cells carry no
        // paragraph or run properties (a bare run), exactly as before.
        var table = new WordTableBuilder<Employee>(SampleData.Employees()).Build();

        var dataRow = table.Elements<TableRow>().Skip(1).First();
        var paragraph = dataRow.GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        IsNull(paragraph.ParagraphProperties);
        IsNull(paragraph.GetFirstChild<Run>()!.RunProperties);
    }

    [Test]
    public void ParagraphStylesAppliedToHeaderAndBodyCells()
    {
        #region WordTableParagraphStyles

        var builder = new WordTableBuilder<Employee>(SampleData.Employees())
            .HeadingParagraphStyle("TBLHeading")
            .BodyParagraphStyle("TBLText");

        #endregion

        var table = builder.Build();
        var rows = table.Elements<TableRow>().ToList();

        var headerParagraph = rows[0].GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        AreEqual("TBLHeading", headerParagraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value);

        var bodyParagraph = rows[1].GetFirstChild<TableCell>()!.GetFirstChild<Paragraph>()!;
        AreEqual("TBLText", bodyParagraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value);
    }

    [Test]
    public void BodyParagraphStyleReachesHtmlCells()
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
        AreEqual("TBLText", paragraph.ParagraphProperties!.ParagraphStyleId!.Val?.Value);
        IsNotNull(paragraph.GetFirstChild<Run>()!.RunProperties!.GetFirstChild<Italic>());
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
    public void WidthHintSwitchesTableToFixedDxaLayout()
    {
        var table = new WordTableBuilder<WidthRow>(WidthRows()).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        // A width hint flips the table from the default pct auto-layout to fixed dxa layout.
        var width = props.GetFirstChild<TableWidth>()!;
        AreEqual(TableWidthUnitValues.Dxa, width.Type?.Value);

        var layout = props.GetFirstChild<TableLayout>()!;
        AreEqual(TableLayoutValues.Fixed, layout.Type?.Value);

        // tblW is the sum of the grid column widths.
        var gridWidths = table.GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => int.Parse(_.Width!.Value!))
            .ToList();
        AreEqual(gridWidths.Sum(), int.Parse(width.Width!.Value!));
    }

    [Test]
    public void WidthHintsResolveExplicitClampedAndRaisedColumns()
    {
        var table = new WordTableBuilder<WidthRow>(WidthRows()).Build();
        var gridWidths = table.GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => int.Parse(_.Width!.Value!))
            .ToList();

        // chars -> twips is (chars * 7 + 5) * 15.
        // Fixed: explicit Width = 10 -> (10*7+5)*15 = 1125.
        AreEqual(1125, gridWidths[0]);
        // Clamped: long content auto-sizes wide but MaxWidth = 8 caps it -> (8*7+5)*15 = 915.
        AreEqual(915, gridWidths[1]);
        // Raised: tiny content but MinWidth = 30 floors it -> (30*7+5)*15 = 3225.
        AreEqual(3225, gridWidths[2]);
    }

    [Test]
    public void NoWidthHintKeepsAutoLayoutAndBareGridColumns()
    {
        // Regression: a model without any Width/MinWidth/MaxWidth keeps the prior output — a
        // pct=5000 width, no tblLayout, and grid columns with no explicit width.
        var table = new WordTableBuilder<Employee>(SampleData.Employees()).Build();
        var props = table.GetFirstChild<TableProperties>()!;

        var width = props.GetFirstChild<TableWidth>()!;
        AreEqual(TableWidthUnitValues.Pct, width.Type?.Value);
        AreEqual("5000", width.Width?.Value);
        IsNull(props.GetFirstChild<TableLayout>());

        var gridColumns = table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().ToList();
        IsTrue(gridColumns.All(_ => _.Width == null));
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
    public void FluentColumnConfigurationOverridesHeading()
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

        IsTrue(headings.Contains("Person"));
        IsFalse(headings.Contains("Full Name"));
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
    public void RunPropertiesFollowSchemaOrder()
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
        Assert.That(
            order,
            Is.EqualTo(
                new[]
                {
                    typeof(RunFonts),
                    typeof(Bold),
                    typeof(Color),
                    typeof(FontSize),
                    typeof(FontSizeComplexScript),
                    typeof(Underline)
                }));
    }

    [Test]
    public void StandaloneTableBordersFollowSchemaOrder()
    {
        // CT_TblBorders requires top, left, bottom, right, insideH, insideV. The standalone
        // (no MainDocumentPart) path emits these inline, so an out-of-order set corrupts the table.
        var borders = new WordTableBuilder<Employee>(SampleData.Employees())
            .Build()
            .GetFirstChild<TableProperties>()!
            .GetFirstChild<TableBorders>()!;

        var order = borders.ChildElements.Select(_ => _.GetType()).ToList();
        Assert.That(
            order,
            Is.EqualTo(
                new[]
                {
                    typeof(TopBorder),
                    typeof(LeftBorder),
                    typeof(BottomBorder),
                    typeof(RightBorder),
                    typeof(InsideHorizontalBorder),
                    typeof(InsideVerticalBorder)
                }));
    }

    [Test]
    public void StandaloneStyledTableIsSchemaValid()
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

        Assert.That(errors, Is.Empty);
    }
}
