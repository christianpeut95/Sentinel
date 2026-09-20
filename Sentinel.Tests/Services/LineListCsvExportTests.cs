using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class LineListCsvExportTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"https://example.test\",\"open\")", "\"'=HYPERLINK(\"\"https://example.test\"\",\"\"open\"\")\"")]
    [InlineData("+123", "'+123")]
    [InlineData("-123", "'-123")]
    [InlineData("@SUM(A1:A2)", "'@SUM(A1:A2)")]
    [InlineData("\t=1+1", "'\t=1+1")]
    public void EscapeCsvCell_TreatsFormulaLikeUserContentAsText(string value, string expected)
    {
        Assert.Equal(expected, LineListService.EscapeCsvCell(value));
    }

    [Theory]
    [InlineData("Adelaide", "Adelaide")]
    [InlineData("Smith, Jane", "\"Smith, Jane\"")]
    [InlineData("A \"quoted\" value", "\"A \"\"quoted\"\" value\"")]
    public void EscapeCsvCell_PreservesOrdinaryCsvEscaping(string value, string expected)
    {
        Assert.Equal(expected, LineListService.EscapeCsvCell(value));
    }
}
