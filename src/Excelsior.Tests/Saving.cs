// ReSharper disable UnusedParameter.Local
public class Saving
{
    [Test]
    public async Task ToStream()
    {
        var data = SampleData.Employees();

        #region ToStream

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var stream = new MemoryStream();
        await builder.ToStream(stream);

        #endregion

        await Verify(stream, extension: "xlsx");
    }

    [Test]
    public async Task ToBytes()
    {
        var data = SampleData.Employees();

        #region ToBytes

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var bytes = await builder.ToBytes();

        #endregion

        await Verify(bytes, extension: "xlsx");
    }

    [Test]
    public async Task ToMemoryStream()
    {
        var data = SampleData.Employees();

        #region ToMemoryStream

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var stream = await builder.ToMemoryStream();

        #endregion

        await Verify(stream, extension: "xlsx");
    }
}

// ReSharper disable UnusedParameter.Local

