public class SaveAsWriteOnlyStreamTests
{
    [Test]
    public async Task SaveAs_WriteOnlyStream()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());

        var memoryStream = new MemoryStream();
        await using var writeOnlyStream = new WriteOnlyStream(memoryStream);
        await builder.ToStream(writeOnlyStream);

        await Assert.That(memoryStream.Length).IsGreaterThan(0);
    }

    class WriteOnlyStream(Stream inner) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }
}
