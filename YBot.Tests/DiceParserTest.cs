using Microsoft.Extensions.Logging;
using Moq;
using YBot.Services;

namespace YBot.Tests;

public class DiceParserTest
{
    private readonly DiceParser _parser;

    public DiceParserTest()
    {
        var logger = new Mock<ILogger<DiceParser>>();
        _parser = new DiceParser(logger.Object);
    }

    [Theory]
    [InlineData("+1", 1)]
    [InlineData("-1", -1)]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("(1)", 1)]
    [InlineData("(2)", 2)]
    [InlineData("d1", 1)]
    [InlineData("D1", 1)]
    [InlineData("1d1d1", 1)]
    [InlineData("2D1D1", 2)]
    [InlineData("100d1", 100)]
    [InlineData("100D1", 100)]
    [InlineData("100D(1D1)", 100)]
    [InlineData("3+3", 6)]
    [InlineData("3-3", 0)]
    [InlineData("3*3", 9)]
    [InlineData("3x3", 9)]
    [InlineData("3(3)", 9)]
    [InlineData("(3)3", 9)]
    [InlineData("3(3)+3(3)", 18)]
    [InlineData("(3)3+(3)3", 18)]
    [InlineData("3(3)3+3(3)3", 54)]
    [InlineData("1+(2*3", 7)]
    [InlineData("3/3", 1)]
    [InlineData("3\\3", 0)]
    [InlineData("3%3", 0)]
    [InlineData("3^3", 27)]
    [InlineData("3D1+3", 6)]
    [InlineData("3D1-3", 0)]
    [InlineData("3D1*3", 9)]
    [InlineData("3D1/3", 1)]
    [InlineData("3D1\\3", 0)]
    [InlineData("3D1^3", 27)]
    [InlineData("3+3*3", 12)]
    [InlineData("3*3+3", 12)]
    [InlineData("3*3^3", 81)]
    [InlineData("3^3*3", 81)]
    [InlineData("3+3*3^3", 84)]
    [InlineData("3+3^3*3", 84)]
    [InlineData("3*3^3+3", 84)]
    [InlineData("3^3*3+3", 84)]
    [InlineData("3*3+3^3", 36)]
    [InlineData("3^3+3*3", 36)]
    [InlineData("3*(3+3)^3", 648)]
    [InlineData("3^(3+3)*3", 2187)]
    [InlineData("3(3+3)^3", 648)]
    [InlineData("3^(3+3)3", 2187)]
    [InlineData("3D1*(3D1+3D1)^3D1", 648)]
    [InlineData("3D1^(3D1+3D1)*3D1", 2187)]
    [InlineData("3D1(3D1+3D1)^3D1", 648)]
    [InlineData("3D1^(3D1+3D1)3D1", 2187)]
    [InlineData("1d1x2d1+3D1^(4d1*5d1/6d1\\7d1)%8d1", 5)]
    public void QuerySpecifyTest(string query, int answer)
    {
        var tuple = _parser.Parse(query);
        Assert.NotNull(tuple);
        var (result, empty1, empty2) = tuple.Value;
        Assert.Null(empty1);
        Assert.Null(empty2);
        Assert.Equal(answer, result);
    }

    [Theory]
    [InlineData("D100")]
    [InlineData("1D100")]
    [InlineData("1D(100D1)")]
    [InlineData("100D100")]
    public void QueryNumberTest(string query)
    {
        var tuple = _parser.Parse(query);
        Assert.NotNull(tuple);
        var (result, empty1, empty2) = tuple.Value;
        Assert.Null(empty1);
        Assert.Null(empty2);
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData("1x")]
    [InlineData("1*")]
    [InlineData("1+")]
    [InlineData("1-")]
    [InlineData("1D")]
    [InlineData("1d")]
    [InlineData("()")]
    public void QueryNullThrowTest(string query) { Assert.Throws<ArgumentNullException>(() => _parser.Parse(query)); }

    [Theory]
    [InlineData("a")]
    [InlineData("a1")]
    [InlineData("1+1b")]
    [InlineData("1+1b1+1")]
    [InlineData("1b")]
    [InlineData("1s")]
    [InlineData("1+1)")]
    public void QueryErrorThrowTest(string query)
    {
        try { _parser.Parse(query); }
        catch (ArgumentException e) when (e is not ArgumentNullException) { }
    }

    [Theory]
    [InlineData("2>1")]
    [InlineData("2>=1")]
    [InlineData("1<2")]
    [InlineData("1<=2")]
    [InlineData("1>=1")]
    [InlineData("1<=1")]
    [InlineData("1=1")]
    [InlineData("2D1>1D1")]
    [InlineData("1D1<2D1")]
    [InlineData("2D1>=1D1")]
    [InlineData("1D1>=1D1")]
    [InlineData("1D1<=1D1")]
    [InlineData("1D1=1D1")]
    [InlineData("100D(1D1) > 1")]
    public void QueryBoolTest(string query)
    {
        var tuple = _parser.Parse(query);
        Assert.NotNull(tuple);
        var (_, empty1, empty2) = tuple.Value;
        Assert.NotNull(empty1);
        Assert.NotNull(empty2);
        Assert.True(empty1);
    }
}