namespace nunit_tests;

public class UnitTest1
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase("a")]
    [TestCase("b")]
    public void Test1_DataDriven_Success(string expectedValue)
    {
        Assert.That(expectedValue, Is.EqualTo("a"));
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}