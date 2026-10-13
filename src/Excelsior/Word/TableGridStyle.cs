using W = DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Idempotently inserts the two built-in Word table styles Excelsior depends on —
/// <c>TableNormal</c> (the default table style that supplies cell margins) and
/// <c>TableGrid</c> (single-line borders, inherits cell margins from <c>TableNormal</c>) —
/// into a host document's <see cref="StyleDefinitionsPart"/>. Word ships both at the
/// application level but only writes them into a doc's <c>styles.xml</c> when a table that
/// uses them is inserted, so programmatically-built hosts and templates authored without
/// tables won't have them on disk. Inserting both — with their stock definitions matching
/// what Word produces — is what makes <c>&lt;w:tblStyle w:val="TableGrid"/&gt;</c> render
/// with the expected borders <em>and</em> cell padding regardless of the host. Existing
/// definitions are left untouched so user customizations survive.
/// </summary>
static class TableGridStyle
{
    public const string StyleId = "TableGrid";
    const string tableNormalId = "TableNormal";

    // Built once per process; CloneNode is faster than rebuilding the element tree, and the
    // prototype itself is never appended (OpenXmlElement instances can't be shared between
    // parents — appending detaches from the prior one).
    static readonly W.Style tableNormalPrototype = BuildTableNormalStyle();
    static readonly W.Style tableGridPrototype = BuildTableGridStyle();

    public static void EnsurePresent(MainDocumentPart mainPart, RelationshipIds relationshipIds)
    {
        var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>(relationshipIds.Named("rIdStyles"));
        stylesPart.Styles ??= new();

        EnsureStyle(stylesPart.Styles, tableNormalId, tableNormalPrototype);
        EnsureStyle(stylesPart.Styles, StyleId, tableGridPrototype);
    }

    static void EnsureStyle(W.Styles styles, string styleId, W.Style prototype)
    {
        var existing = styles
            .Elements<W.Style>()
            .Any(_ => _.Type?.Value == W.StyleValues.Table &&
                      _.StyleId?.Value == styleId);

        if (existing)
        {
            return;
        }

        styles.Append((W.Style) prototype.CloneNode(true));
    }

    // Mirrors the TableNormal Word ships in user-authored docs: zero indentation, 108dxa
    // left/right cell margins, marked default so any table that doesn't reference an
    // explicit style still inherits this padding.
    static W.Style BuildTableNormalStyle() =>
        new(
            new W.StyleName
            {
                Val = "Normal Table"
            },
            new W.UIPriority
            {
                Val = 99
            },
            new W.SemiHidden(),
            new W.UnhideWhenUsed(),
            new W.StyleTableProperties(
                new W.TableIndentation
                {
                    Width = 0,
                    Type = W.TableWidthUnitValues.Dxa
                },
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
                    })))
        {
            Type = W.StyleValues.Table,
            StyleId = tableNormalId,
            Default = true,
        };

    // Mirrors the TableGrid Word ships when a user inserts a table from the ribbon: borders
    // only, no cell margins of its own. Cell padding flows in via basedOn=TableNormal.
    static W.Style BuildTableGridStyle() =>
        new(
            new W.StyleName
            {
                Val = "Table Grid"
            },
            new W.BasedOn
            {
                Val = tableNormalId
            },
            new W.UIPriority
            {
                Val = 39
            },
            new W.StyleParagraphProperties(
                new W.SpacingBetweenLines
                {
                    After = "0",
                    Line = "240",
                    LineRule = W.LineSpacingRuleValues.Auto
                }),
            new W.StyleTableProperties(
                new W.TableBorders(
                    new W.TopBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    },
                    new W.LeftBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    },
                    new W.BottomBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    },
                    new W.RightBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    },
                    new W.InsideHorizontalBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    },
                    new W.InsideVerticalBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Space = 0,
                        Color = "auto"
                    })))
        {
            Type = W.StyleValues.Table,
            StyleId = StyleId,
        };
}
