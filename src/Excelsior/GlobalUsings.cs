global using System.Collections.Concurrent;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Text.Json;
global using System.Xml.Linq;
global using DocumentFormat.OpenXml;
global using DocumentFormat.OpenXml.Packaging;
global using DocumentFormat.OpenXml.Spreadsheet;
global using Excelsior;
global using OpenXmlKit;
// Colour is the one thing wanted out of OpenXmlKit.Word, and Color here is already the
// spreadsheet one.
global using KitColor = OpenXmlKit.Word.Color;
global using OpenXmlHtml;
global using Cell = DocumentFormat.OpenXml.Spreadsheet.Cell;
global using CellStyle = Excelsior.CellStyle;
global using Color = DocumentFormat.OpenXml.Spreadsheet.Color;
global using Column = DocumentFormat.OpenXml.Spreadsheet.Column;
global using Font = DocumentFormat.OpenXml.Spreadsheet.Font;