static class CamelCase
{
    public static string Split(string text)
    {
        var spaceCount = 0;
        for (var i = 1; i < text.Length; i++)
        {
            if (IsWordStart(text, i))
            {
                spaceCount++;
            }
        }

        if (spaceCount == 0)
        {
            return text;
        }

        var result = new char[text.Length + spaceCount];
        var position = 0;
        result[position++] = text[0];
        for (var i = 1; i < text.Length; i++)
        {
            if (IsWordStart(text, i))
            {
                result[position++] = ' ';
            }

            result[position++] = text[i];
        }

        return new(result);
    }

    // An uppercase letter starts a new word when it follows a lowercase letter ("OrderId" ->
    // "Order Id") or ends a run of uppercase letters that begins the next word ("HTTPStatus" ->
    // "HTTP Status"). Consecutive uppercase letters of an acronym stay together, so "OrderID"
    // stays "Order ID" and "ID" stays "ID" rather than being split into "Order I D" / "I D".
    static bool IsWordStart(string text, int index)
    {
        if (!char.IsUpper(text[index]))
        {
            return false;
        }

        if (char.IsLower(text[index - 1]))
        {
            return true;
        }

        return index + 1 < text.Length &&
               char.IsLower(text[index + 1]);
    }
}
