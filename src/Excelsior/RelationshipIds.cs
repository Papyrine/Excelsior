namespace Excelsior;

/// <summary>
/// Picks the relationship id for a part or an external link Excelsior adds. Left to the Open XML
/// SDK an id is random (<c>R</c> and sixteen hex digits), and it lands in the <c>.rels</c> file
/// and in the xml that refers to it, so the same input would never produce the same bytes twice.
/// </summary>
/// <remarks>
/// Every id is checked against the ones the owning part already holds: the ids of a host document
/// are arbitrary, and a second table rendered into the same document meets the ids of the first.
/// An instance reads the ids of the owner once and remembers the ones it hands out, which is what
/// keeps a table or sheet of thousands of links from rereading them for each one. It does not see
/// an id something else adds after that first read, so an instance lasts one render, and the
/// prefixes used here (<c>rId</c> and a name) are ones OpenXmlHtml, which adds relationships for
/// html cells during a render, does not use.
/// </remarks>
class RelationshipIds(OpenXmlPartContainer owner)
{
    HashSet<string>? taken;
    Dictionary<string, int>? last;

    /// <summary>
    /// For a part the owner holds one of: <paramref name="id"/> itself, or <c>{id}2</c>,
    /// <c>{id}3</c>, ... when it is taken.
    /// </summary>
    public static string Named(OpenXmlPartContainer owner, string id) =>
        new RelationshipIds(owner).Named(id);

    /// <summary>
    /// For one of many: <c>{prefix}1</c>, <c>{prefix}2</c>, ... skipping any that are taken.
    /// </summary>
    public static string Next(OpenXmlPartContainer owner, string prefix) =>
        new RelationshipIds(owner).Next(prefix);

    public string Named(string id)
    {
        if (Taken().Add(id))
        {
            return id;
        }

        return Step(id, 1);
    }

    public string Next(string prefix) =>
        Step(prefix, 0);

    string Step(string prefix, int after)
    {
        var ids = Taken();
        last ??= new(StringComparer.Ordinal);
        var number = last.GetValueOrDefault(prefix, after);

        string id;
        do
        {
            number++;
            id = prefix + number.ToString(CultureInfo.InvariantCulture);
        } while (!ids.Add(id));

        last[prefix] = number;
        return id;
    }

    HashSet<string> Taken()
    {
        if (taken != null)
        {
            return taken;
        }

        taken = new(StringComparer.Ordinal);
        foreach (var pair in owner.Parts)
        {
            taken.Add(pair.RelationshipId);
        }

        foreach (var relationship in owner.ExternalRelationships)
        {
            taken.Add(relationship.Id);
        }

        foreach (var relationship in owner.HyperlinkRelationships)
        {
            taken.Add(relationship.Id);
        }

        foreach (var relationship in owner.DataPartReferenceRelationships)
        {
            taken.Add(relationship.Id);
        }

        return taken;
    }
}
