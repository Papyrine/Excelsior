public class CamelCaseTests
{
    [Test]
    [Arguments("", "")]
    [Arguments("A", "A")]
    [Arguments("Name", "Name")]
    [Arguments("FirstName", "First Name")]
    [Arguments("NoAttributes", "No Attributes")]
    [Arguments("ID", "ID")]
    [Arguments("OrderID", "Order ID")]
    [Arguments("HTTPStatus", "HTTP Status")]
    [Arguments("IOError", "IO Error")]
    [Arguments("XMLParser", "XML Parser")]
    public async Task Split(string input, string expected) =>
        await Assert.That(CamelCase.Split(input)).IsEqualTo(expected);
}
