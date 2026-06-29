# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Excelsior is a .NET library for Excel spreadsheet generation (and now deserialization) using a data-driven approach. It uses [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) for spreadsheet creation and [OpenXmlHtml](https://github.com/Papyrine/OpenXmlHtml) for HTML cell rendering. There is also a Word table generator (`WordTableBuilder`).

## Build & Test Commands

All commands must be run from the repository root.

```bash
# Build
dotnet build src --configuration Release

# Run all tests
dotnet test src --configuration Release

# Run a single test project
dotnet test src/Excelsior.Tests

# Run a specific test
dotnet test src/Excelsior.Tests --filter "FullyQualifiedName~UsageTests"
```

CI uses AppVeyor (config at `src/appveyor.yml`).

### Approving Verify snapshots

When a Verify-based test produces a `*.received.{txt,png,xlsx}` file, rename it to `*.verified.{...}` to approve. The first run of any new Verify test always fails — that's expected.

## Architecture

### Write side (`src/Excelsior/`)
- `BookBuilder` — entry point; sheet registration, build orchestration, and stream/file output. Writes a custom XML metadata part to each workbook (`MetadataNamespace` const) recording `column index → property name` per sheet — this is what the reader uses for round-trip column resolution.
- `Renderer` — writes headers and data rows to a sheet
- `SheetBuilder` / `ISheetBuilder<TModel>` — fluent API for per-column write configuration (heading, order, width, render, cell style, format, validation, formulas)
- `ColumnConfig` — holds per-column settings; `Columns` manages the ordered list
- `CellStyle` / `StyleManager` — style container + dedup'd OpenXml stylesheet cache
- `SheetContext` — wraps WorksheetPart for cell access and column letter conversion
- `Property` / `Properties` — reflection-based property discovery with attribute support, including `[Split]` recursion for flattening nested types
- `ValueRenderer` — static config for type-to-string conversion (dates, enums, bool display, whitespace trimming). Settings must be configured via `[ModuleInitializer]` before any `BookBuilder` is constructed; otherwise `ThrowIfBookBuilderUsed` fires.
- `TemplateSheetBuilder` — for emitting empty data-entry templates with explicit column declarations
- Attributes: `ColumnAttribute`, `SplitAttribute`, `IgnoreAttribute`, `SheetModelAttribute` (in `Attributes/`)
- Word generator: `Word/WordTableBuilder.cs`, `Word/WordTableRenderer.cs`

### Read side (`src/Excelsior/`)
- `BookReader` — entry point; mirrors `BookBuilder.AddSheet`. `Convert(stream)` throws `ReadException` on failure; `TryConvert(stream)` returns a `ReadResult` (implicit `bool` + `ReadError[]`). Exception's `Errors` matches the result's `Errors`.
- `SheetReader<TModel>` / `ISheetReader<TModel>` — strong-typed; auto-discovers properties from `TModel`, reuses `[Column]`/`[Display]`/`[DisplayName]` attributes for heading resolution.
- `DictionarySheetReader` / `IDictionarySheetReader` — explicit-column path for sheets without a backing model. Each row is `IReadOnlyDictionary<string, object?>`.
- `SheetParser` — header→column resolution (metadata XML first, heading-text fallback), row enumeration, dispatch.
- `CellConverter` — primitive parsing inverse of `Renderer.SetCellValue`; honours `ValueRenderer` global config (BoolDisplay, enum humanizer, TrimWhitespace).
- `ModelActivator<T>` — instantiates models via `ConstructorInfo.Invoke` (parameterless ctor preferred, falls back to longest ctor whose param names match property names). `ConstructorInfo.Invoke` bypasses the `required`-members runtime check that `Activator.CreateInstance` would enforce.
- Per-column delegate conversion: `sheet.Convert(_ => _.Prop, cell => ...)` — the user delegate receives the raw OpenXml `Cell`.

### Test projects
- `Excelsior.Tests` — main suite, NUnit + Verify
- `Excelsior.SourceGenerator.Tests` — source generator tests
- `StaticSettingsTests` — tests for global static settings (date formats, whitespace trimming, enum rendering); separate process to avoid `ValueRenderer` global-state contamination
- `SheetRender` — .NET Framework 4.8 utility using Excel Interop. `RenderExcel` opens all `.verified.xlsx` files, renders each sheet's used range to a bitmap, saves as `_SheetName.png`. Tests are `[Explicit]` (manual run only).
- `Model` — shared model classes used by tests (`Employee`, `EmployeeStatus`, `SampleData`)

### Source generator (`src/Excelsior.SourceGenerator/`)

Referenced as an Analyzer from `Excelsior.csproj`. Emits `GeneratedColumnAttributes` so consumers can attach column metadata via partial method declarations rather than runtime attributes — useful for scenarios where the model assembly cannot reference Excelsior.

## Documentation

The `readme.md` uses [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets) with `InPlaceOverwrite` convention (configured in `src/mdsnippets.json`). Code samples are pulled from test files via `#region SnippetName` / `#endregion` markers. Snippets populate automatically during build (via `MarkdownSnippets.MsBuild`). When adding new code samples, use `<!-- snippet: SnippetName -->` / `<!-- endSnippet -->` references backed by region markers in test code rather than inline code blocks.

## Key Conventions

- Target framework: `net10.0` (with `LangVersion` set to `preview`)
- Central package management via `src/Directory.Packages.props`
- `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` are both enabled — IDE style rules (e.g. `IDE0007 use 'var'`) fail the build rather than warning. Use `var` everywhere; for tests demonstrating an implicit conversion, use a cast (`var x = (TargetType)source`) rather than declaring the type.
- Global usings live in three places: each project's `GlobalUsings.cs`, the auto-generated `*.GlobalUsings.g.cs` (implicit usings), and the `ProjectDefaults` NuGet package which adds `System.Text`, `System.Reflection`, `System.Diagnostics`, etc. Important type aliases: `Date = System.DateOnly`, `Time = System.TimeOnly`, `Cancel = CancellationToken`, `CancelSource = CancellationTokenSource`.
- `ValueRenderer` static methods (`For<T>`, `ForEnums`, `BoolDisplay`, `NullDisplayFor<T>`, `DisableWhitespaceTrimming`, `Default*Format`) must be called from a `[ModuleInitializer]` — they throw if invoked after the first `BookBuilder` is constructed.
- **Binary compatibility with Parchment**: [Parchment](https://github.com/Papyrine/Parchment) consumes `WordTableBuilder` via a *compiled* `new WordTableBuilder<T>(data).Build(mainPart)` call in its `ExcelsiorTableBridge` (only the generic-method dispatch is reflection; the ctor call is baked IL). Changing a public `WordTableBuilder` signature — **including adding an optional constructor parameter** — is a binary-breaking change: a Parchment build compiled against the old signature throws `MissingMethodException: WordTableBuilder..ctor(...)` at render time until it is rebuilt and republished. Prefer **additive fluent methods** (e.g. `BodyStyle`, `HeadingParagraphStyle`, `BodyParagraphStyle`) over new/changed constructor parameters so existing Parchment builds keep working.
- **Word table cell text follows the host's paragraph styles, not the table style.** Cells the renderer emits have no `pStyle`, so they inherit the document's default paragraph style (`Normal`), and a paragraph style outranks a table style's `rPr` per OpenXML precedence — so branding a table's font via `TableGrid`/`rPr` alone silently loses to `Normal`. Style body text either with the run-level `bodyStyle`/`CellStyle` callbacks (plain-text cells only) or, to reach `IsHtml`/`Link` cells too, with the named-paragraph-style methods (`HeadingParagraphStyle`/`BodyParagraphStyle`).
