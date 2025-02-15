namespace mstest_tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    [DataRow("a")]
    [DataRow("b")]
    public void Test1_Success(string expectedValue)
    {
        Assert.AreEqual("a", expectedValue);
    }

    [TestMethod]
    public void TestMethod2()
    {
    }
}