namespace xunit_tests;

public class UnitTest1
{
    [Theory]
    [InlineData("a")]
    [InlineData("b")]
    public void Test1_Success(string expectedValue)
    {
        Assert.Equal("a", expectedValue);
    }

    [Fact]
    public void Test2()
    {

    }
}