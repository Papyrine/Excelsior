internal static class ColumnConfigMerge
{
    /// <summary>
    /// Applies user-supplied settings (everything except TModel-aware members like
    /// CellStyle/Render/Formula) from <paramref name="settings"/> onto <paramref name="target"/>.
    /// Null/unset values on the source leave the target unchanged.
    /// </summary>
    public static void ApplyUserSettings<TModel>(IColumnSettings settings, ColumnConfig<TModel> target)
    {
        if (settings.Heading != null)
        {
            target.Heading = settings.Heading;
        }

        if (settings.Order != null)
        {
            target.Order = settings.Order;
        }

        if (settings.Width != null)
        {
            target.Width = settings.Width;
        }

        if (settings.MinWidth != null)
        {
            target.MinWidth = settings.MinWidth;
        }

        if (settings.MaxWidth != null)
        {
            target.MaxWidth = settings.MaxWidth;
        }

        if (settings.HeadingStyle != null)
        {
            target.HeadingStyle = settings.HeadingStyle;
        }

        if (settings.Format != null)
        {
            target.Format = settings.Format;
        }

        if (settings.NullDisplay != null)
        {
            target.NullDisplay = settings.NullDisplay;
        }

        if (settings.IsHtml is { } isHtml)
        {
            if (target.IsHtmlExplicit && target.IsHtml != isHtml)
            {
                throw new($"Column '{target.Name}': mismatched IsHtml — attribute says {target.IsHtml}, fluent configuration says {isHtml}.");
            }

            target.IsHtml = isHtml;
            target.IsHtmlExplicit = true;
        }

        if (settings.Filter != null)
        {
            target.Filter = settings.Filter.Value;
        }

        if (settings.Include != null)
        {
            target.Include = settings.Include.Value;
        }

        if (settings.DisableAllowedValues)
        {
            target.AllowedValues = null;
        }
        else if (settings.AllowedValues != null)
        {
            target.AllowedValues = settings.AllowedValues;
        }

        if (settings.NumericMin.HasValue)
        {
            target.NumericMin = settings.NumericMin;
        }

        if (settings.NumericMax.HasValue)
        {
            target.NumericMax = settings.NumericMax;
        }

        if (settings.DateMin.HasValue)
        {
            target.DateMin = settings.DateMin;
        }

        if (settings.DateMax.HasValue)
        {
            target.DateMax = settings.DateMax;
        }

        if (settings.Required.HasValue)
        {
            target.Required = settings.Required.Value;
        }

        if (settings.Locked.HasValue)
        {
            target.Locked = settings.Locked;
        }

        if (settings.InputTitle != null)
        {
            target.InputTitle = settings.InputTitle;
        }

        if (settings.InputMessage != null)
        {
            target.InputMessage = settings.InputMessage;
        }

        if (settings.ErrorTitle != null)
        {
            target.ErrorTitle = settings.ErrorTitle;
        }

        if (settings.ErrorMessage != null)
        {
            target.ErrorMessage = settings.ErrorMessage;
        }

        if (settings.ErrorStyle.HasValue)
        {
            target.ErrorStyle = settings.ErrorStyle;
        }

        if (settings.DisableInputMessage)
        {
            target.DisableInputMessage = true;
        }

        if (settings.Note != null)
        {
            target.Note = settings.Note;
        }
    }
}
