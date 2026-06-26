namespace Excelsior;

public static partial class ValueRenderer
{
    [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)]
    public static string DefaultDateFormat
    {
        internal get;
        set
        {
            ThrowIfBookBuilderUsed();
            field = value;
        }
    } = "yyyy-MM-dd";

    [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
    public static string DefaultDateTimeFormat
    {
        internal get;
        set
        {
            ThrowIfBookBuilderUsed();
            field = value;
        }
    } = "yyyy-MM-dd HH:mm:ss";

    [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
    public static string DefaultDateTimeOffsetFormat
    {
        internal get;
        set
        {
            ThrowIfBookBuilderUsed();
            field = value;
        }
    } = "yyyy-MM-dd HH:mm:ss zzz";

    [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)]
    public static string DefaultTimeFormat
    {
        internal get;
        set
        {
            ThrowIfBookBuilderUsed();
            field = value;
        }
    } = "HH:mm:ss";

    internal static string DefaultFormatFor(TemporalKind kind) =>
        kind switch
        {
            TemporalKind.Date => DefaultDateFormat,
            TemporalKind.Time => DefaultTimeFormat,
            TemporalKind.DateTimeOffset => DefaultDateTimeOffsetFormat,
            _ => DefaultDateTimeFormat
        };
}
