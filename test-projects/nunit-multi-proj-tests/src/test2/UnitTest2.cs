namespace nunit_tests;

public class UnitTest2
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase("c")]
    [TestCase("d")]
    public void Test2_DataDriven_Success(string expectedValue)
    {
        Assert.That(expectedValue, Is.EqualTo("c"));
    }

    [Test]
    public void Test2()
    {
        Assert.Pass();
    }

    public static object[] TestData =>
    [
        new object[] { "e" },
        new object[] { "f" },
        new object[] { "g" }
    ];

    [TestCaseSource(nameof(TestData))]
    public void Test2_UsingTestCaseSource(string expectedValue)
    {
        Assert.Equals(expectedValue, "f");
    }
}