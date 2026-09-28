// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
public class ColumnsTests
{
    class NoOrderModel
    {
        public string First { get; set; } = "";
        public string Second { get; set; } = "";
        public string Third { get; set; } = "";
    }

    [Test]
    public async Task NoOrder_UsesDeclarationOrder()
    {
        var columns = new Columns<NoOrderModel>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["First", "Second", "Third"], CollectionOrdering.Matching);
    }

    class MixedOrderModel
    {
        public string NoOrder1 { get; set; } = "";

        [Column(Order = 5)]
        public string Ordered { get; set; } = "";

        public string NoOrder2 { get; set; } = "";
    }

    [Test]
    public async Task MixedOrder_UnorderedMaintainDeclarationPosition()
    {
        var columns = new Columns<MixedOrderModel>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["Ordered", "NoOrder1", "NoOrder2"], CollectionOrdering.Matching);
    }

    class AllOrderedModel
    {
        [Column(Order = 3)]
        public string Third { get; set; } = "";

        [Column(Order = 1)]
        public string First { get; set; } = "";

        [Column(Order = 2)]
        public string Second { get; set; } = "";
    }

    [Test]
    public async Task AllOrdered_SortsByOrder()
    {
        var columns = new Columns<AllOrderedModel>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["First", "Second", "Third"], CollectionOrdering.Matching);
    }

    record NoOrderRecord(string First, string Second, string Third);

    [Test]
    public async Task Record_NoOrder_UsesDeclarationOrder()
    {
        var columns = new Columns<NoOrderRecord>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["First", "Second", "Third"], CollectionOrdering.Matching);
    }

    record MixedOrderRecord(
        string NoOrder1,
        [Column(Order = 5)] string Ordered,
        string NoOrder2);

    [Test]
    public async Task Record_MixedOrder_UnorderedMaintainDeclarationPosition()
    {
        var columns = new Columns<MixedOrderRecord>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["Ordered", "NoOrder1", "NoOrder2"], CollectionOrdering.Matching);
    }

    record MixedConstructorAndProperties(string First, string Second)
    {
        public string Third { get; init; } = "";
        public string Fourth { get; init; } = "";
    }

    [Test]
    public async Task Record_MixedConstructorAndProperties_UsesDeclarationOrder()
    {
        var columns = new Columns<MixedConstructorAndProperties>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["First", "Second", "Third", "Fourth"], CollectionOrdering.Matching);
    }

    record MixedConstructorAndPropertiesWithOrder(
        string NoOrder1,
        [Column(Order = 10)] string Ordered1)
    {
        public string NoOrder2 { get; init; } = "";

        [Column(Order = 5)]
        public string Ordered2 { get; init; } = "";
    }

    [Test]
    public async Task Record_MixedConstructorAndPropertiesWithOrder()
    {
        var columns = new Columns<MixedConstructorAndPropertiesWithOrder>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["Ordered2", "Ordered1", "NoOrder1", "NoOrder2"], CollectionOrdering.Matching);
    }

    record AllOrderedRecord(
        [Column(Order = 3)] string Third,
        [Column(Order = 1)] string First,
        [Column(Order = 2)] string Second);

    [Test]
    public async Task Record_AllOrdered_SortsByOrder()
    {
        var columns = new Columns<AllOrderedRecord>();
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["First", "Second", "Third"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Fluent_ReorderColumns()
    {
        var columns = new Columns<NoOrderModel>();
        columns.Add<string>(_ => _.Third, _ => _.Order = 1);
        columns.Add<string>(_ => _.First, _ => _.Order = 2);
        columns.Add<string>(_ => _.Second, _ => _.Order = 3);
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["Third", "First", "Second"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Fluent_PartialOrder_UnorderedAfterOrdered()
    {
        var columns = new Columns<NoOrderModel>();
        columns.Add<string>(_ => _.Third, _ => _.Order = 1);
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["Third", "First", "Second"], CollectionOrdering.Matching);
    }

    class AttributeOrderModel
    {
        [Column(Order = 2)]
        public string A { get; set; } = "";

        public string B { get; set; } = "";

        [Column(Order = 1)]
        public string C { get; set; } = "";
    }

    [Test]
    public async Task Fluent_OverridesAttributeOrder()
    {
        var columns = new Columns<AttributeOrderModel>();
        columns.Add<string>(_ => _.B, _ => _.Order = 0);
        var ordered = columns.OrderedColumns();

        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["B", "C", "A"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Fluent_MixedWithAttributeAndPositional()
    {
        var columns = new Columns<MixedOrderModel>();
        // MixedOrderModel: NoOrder1 (no attr), Ordered (Order=5), NoOrder2 (no attr)
        // Fluent sets NoOrder2 to Order=3
        columns.Add<string>(_ => _.NoOrder2, _ => _.Order = 3);
        var ordered = columns.OrderedColumns();

        // Ordered(5), NoOrder2(3) are explicitly ordered; NoOrder1 is positional
        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["NoOrder2", "Ordered", "NoOrder1"], CollectionOrdering.Matching);
    }

    record FluentRecordModel(string A, string B, string C, string D);

    [Test]
    public async Task Fluent_Record_MixOrderedAndPositional()
    {
        var columns = new Columns<FluentRecordModel>();
        columns.Add<string>(_ => _.C, _ => _.Order = 1);
        columns.Add<string>(_ => _.A, _ => _.Order = 2);
        var ordered = columns.OrderedColumns();

        // C(1), A(2) explicitly ordered; B, D positional
        await Assert.That(ordered.Select(_ => _.Name).ToList()).IsEquivalentTo(["C", "A", "B", "D"], CollectionOrdering.Matching);
    }
}
