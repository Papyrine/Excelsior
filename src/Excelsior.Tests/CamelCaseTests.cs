[TestFixture]
public class CamelCaseTests
{
    [TestCase("", "")]
    [TestCase("A", "A")]
    [TestCase("Name", "Name")]
    [TestCase("FirstName", "First Name")]
    [TestCase("NoAttributes", "No Attributes")]
    [TestCase("ID", "ID")]
    [TestCase("OrderID", "Order ID")]
    [TestCase("HTTPStatus", "HTTP Status")]
    [TestCase("IOError", "IO Error")]
    [TestCase("XMLParser", "XML Parser")]
    public void Split(string input, string expected) =>
        Assert.That(CamelCase.Split(input), Is.EqualTo(expected));
}
