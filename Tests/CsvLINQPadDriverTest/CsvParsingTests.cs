using System;
using System.IO;
using System.Linq;
using System.Text;

using AwesomeAssertions;
using AwesomeAssertions.Equivalency;

using CsvHelper;

using NUnit.Framework;

using CsvLINQPadDriver;
using CsvLINQPadDriver.Extensions;

#pragma warning disable IDE0079
#pragma warning disable CA1416
#pragma warning restore IDE0079

namespace CsvLINQPadDriverTest;

// CsvHelper 34 changed its API and defaults; these tests ensure the driver keeps its CsvHelper 33 parsing behavior.
[TestFixture]
public sealed class CsvParsingTests
{
    private sealed record ParseOptions
    {
        public string? CsvSeparator { get; init; } = ",";
        public NoBomEncoding NoBomEncoding { get; init; } = NoBomEncoding.UTF8;
        public bool AutoDetectEncoding { get; init; } = true;
        public bool AllowComments { get; init; }
        public char? CommentChar { get; init; }
        public char? EscapeChar { get; init; }
        public char? QuoteChar { get; init; }
        public bool IgnoreBadData { get; init; }
        public bool IgnoreBlankLines { get; init; }
        public CsvModeOptions? CsvMode { get; init; }
        public WhitespaceTrimOptions? WhitespaceTrimOptions { get; init; }
        public int? SkipLeadingRowsCount { get; init; }
    }

    private static readonly ParseOptions Default = new();

    [TestCase(",",  "a,b,c")]
    [TestCase(";",  "a;b;c")]
    [TestCase("|",  "a|b|c")]
    [TestCase("\t", "a\tb\tc")]
    public void CsvReadRows_SingleCharSeparator_SplitsFields(string separator, string content) =>
        Read(content, Default with { CsvSeparator = separator })
            .Should().BeEquivalentTo(Rows(["a", "b", "c"]), Strict);

    [TestCase("||")]
    [TestCase("\u00A6")]
    public void CsvReadRows_UnsupportedSeparator_Throws(string separator)
    {
        var read = () => Read("a,b", Default with { CsvSeparator = separator });

        read.Should().Throw<NotSupportedException>().WithMessage($"*'{separator}'*single ASCII character*");
    }

    [Test]
    public void CsvReadRows_NoSeparator_DetectsSeparator() =>
        Read("a;b;c\n1;2;3", Default with { CsvSeparator = null })
            .Should().BeEquivalentTo(Rows(["a", "b", "c"], ["1", "2", "3"]), Strict);

    [Test]
    public void CsvReadRows_NoSeparator_IgnoresExcelSeparatorHint() =>
        Read("sep=;\na,b\n1,2", Default with { CsvSeparator = null })
            .Should().BeEquivalentTo(Rows(["sep=;"], ["a", "b"], ["1", "2"]), Strict);

    [Test]
    public void CsvReadRows_BlankLinesNotIgnored_ReturnsBlankLines() =>
        Read("a\n\nb", Default with { IgnoreBlankLines = false })
            .Select(static row => row.FirstOrDefault() ?? string.Empty)
            .Should().Equal("a", "", "b");

    [Test]
    public void CsvReadRows_BlankLinesIgnored_SkipsBlankLines() =>
        Read("a\n\nb", Default with { IgnoreBlankLines = true })
            .Should().BeEquivalentTo(Rows(["a"], ["b"]), Strict);

    [Test]
    public void CsvReadRows_CommentsNotAllowed_ReturnsCommentLines() =>
        Read("#comment,x\nvalue,y", Default with { AllowComments = false })
            .Should().BeEquivalentTo(Rows(["#comment", "x"], ["value", "y"]), Strict);

    [Test]
    public void CsvReadRows_CommentsAllowed_SkipsDefaultCommentLines() =>
        Read("#comment,x\nvalue,y", Default with { AllowComments = true })
            .Should().BeEquivalentTo(Rows(["value", "y"]), Strict);

    [Test]
    public void CsvReadRows_CommentsAllowedWithCommentChar_SkipsCustomCommentLines() =>
        Read(";comment,x\n#value,y", Default with { AllowComments = true, CommentChar = ';' })
            .Should().BeEquivalentTo(Rows(["#value", "y"]), Strict);

    [Test]
    public void CsvReadRows_Rfc4180_UnquotesFields() =>
        Read("\"a,1\",\"b\"\"2\",\"c\n3\"", Default)
            .Should().BeEquivalentTo(Rows(["a,1", "b\"2", "c\n3"]), Strict);

    [Test]
    public void CsvReadRows_QuoteChar_UsesQuoteChar() =>
        Read("'a,1','b''2',\"c\"", Default with { QuoteChar = '\'' })
            .Should().BeEquivalentTo(Rows(["a,1", "b'2", "\"c\""]), Strict);

    [Test]
    public void CsvReadRows_EscapeModeWithoutEscapeChar_UsesDoubleQuoteAsEscape() =>
        Read("a\",b,c\"\"d", Default with { CsvMode = CsvModeOptions.Escape })
            .Should().BeEquivalentTo(Rows(["a,b", "c\"d"]), Strict);

    [Test]
    public void CsvReadRows_EscapeModeWithEscapeChar_UsesEscapeChar() =>
        Read("a\\,b,c\\\\d,\"e\"", Default with { CsvMode = CsvModeOptions.Escape, EscapeChar = '\\' })
            .Should().BeEquivalentTo(Rows(["a,b", "c\\d", "\"e\""]), Strict);

    [Test]
    public void CsvReadRows_NoEscapeMode_KeepsQuotesAndEscapes() =>
        Read("\"a\",b\\c", Default with { CsvMode = CsvModeOptions.NoEscape })
            .Should().BeEquivalentTo(Rows(["\"a\"", "b\\c"]), Strict);

    [TestCase("a\"b,c")]
    [TestCase("\"a\"b,c")]
    public void CsvReadRows_BadDataNotIgnored_Throws(string content)
    {
        var read = () => Read(content, Default with { IgnoreBadData = false });

        read.Should().Throw<CsvFormatException>();
    }

    [TestCase("a\"b,c",   "a\"b")]
    [TestCase("\"a\"b,c", "ab")]
    public void CsvReadRows_BadDataIgnored_Recovers(string content, string expected) =>
        Read(content, Default with { IgnoreBadData = true })
            .Should().BeEquivalentTo(Rows([expected, "c"]), Strict);

    [TestCase(null,                                                 " a ", " b ")]
    [TestCase(WhitespaceTrimOptions.Trim,                           "a",   " b ")]
    [TestCase(WhitespaceTrimOptions.InsideQuotes,                   " a ", "b")]
    [TestCase(WhitespaceTrimOptions.All,                            "a",   "b")]
    [TestCase(WhitespaceTrimOptions.InsideQuotesIncludingSeparator, " a ", "b")]
    [TestCase(WhitespaceTrimOptions.AllIncludingSeparator,          "a",   "b")]
    public void CsvReadRows_WhitespaceTrimOptions_TrimsAsConfigured(WhitespaceTrimOptions? whitespaceTrimOptions, string expectedUnquoted, string expectedQuoted) =>
        Read(" a ,\" b \"", Default with { WhitespaceTrimOptions = whitespaceTrimOptions })
            .Should().BeEquivalentTo(Rows([expectedUnquoted, expectedQuoted]), Strict);

    [TestCase(WhitespaceTrimOptions.All)]
    [TestCase(WhitespaceTrimOptions.AllIncludingSeparator)]
    public void CsvReadRows_TabSeparatorWithTrim_KeepsTabAsSeparator(WhitespaceTrimOptions whitespaceTrimOptions) =>
        Read("a\t \tb", Default with { CsvSeparator = "\t", WhitespaceTrimOptions = whitespaceTrimOptions })
            .Should().BeEquivalentTo(Rows(["a", "", "b"]), Strict);

    // https://github.com/i2van/CsvLINQPadDriver/issues/9
    [TestCase(WhitespaceTrimOptions.InsideQuotes,                   "\t b \t", " c ")]
    [TestCase(WhitespaceTrimOptions.All,                            "\t b \t", "c")]
    [TestCase(WhitespaceTrimOptions.InsideQuotesIncludingSeparator, "b",       " c ")]
    [TestCase(WhitespaceTrimOptions.AllIncludingSeparator,          "b",       "c")]
    public void CsvReadRows_TabSeparatorWithTrim_TrimsSeparatorInsideQuotesAsConfigured(WhitespaceTrimOptions whitespaceTrimOptions, string expectedQuoted, string expectedUnquoted) =>
        Read("a\t\"\t b \t\"\t c ", Default with { CsvSeparator = "\t", WhitespaceTrimOptions = whitespaceTrimOptions })
            .Should().BeEquivalentTo(Rows(["a", expectedQuoted, expectedUnquoted]), Strict);

    [Test]
    public void CsvReadRows_DifferentFieldCounts_ReturnsAllRows() =>
        Read("a,b,c\n1\n1,2,3,4", Default)
            .Select(static row => row.Length)
            .Should().Equal(3, 1, 4);

    [TestCase(0, "h")]
    [TestCase(2, "v2")]
    public void CsvReadRows_SkipLeadingRows_SkipsRows(int skipLeadingRowsCount, string expectedFirst) =>
        Read("h\nv1\nv2", Default with { SkipLeadingRowsCount = skipLeadingRowsCount })
            .First().Should().Equal(expectedFirst);

    [Test]
    public void CsvReadRows_NoBomAndNoAutoDetection_UsesFallbackEncoding() =>
        Read("ä,ö", Default with { NoBomEncoding = NoBomEncoding.Unicode, AutoDetectEncoding = false }, new UnicodeEncoding(false, false))
            .Should().BeEquivalentTo(Rows(["ä", "ö"]), Strict);

    [Test]
    public void CsvReadHeader_ReturnsFirstRow() =>
        WithFile("h1,h2\nv1,v2", static fileName => fileName
                .CsvReadHeader(",", NoBomEncoding.UTF8, false, null, null, null, false, true, false, false, false, HeaderDetection.NoHeader, HeaderFormat.c1, null, null, false, 0)
                .ToArray())
            .Should().Equal("h1", "h2");

    private static string[][] Rows(params string[][] rows) =>
        rows;

    private static EquivalencyOptions<string[]> Strict(EquivalencyOptions<string[]> options) =>
        options.WithStrictOrdering();

    private static string[][] Read(string content, ParseOptions options, Encoding? encoding = null) =>
        WithFile(content, fileName => FileExtensions.CsvReadRows(
                fileName,
                options.CsvSeparator,
                options.NoBomEncoding,
                options.AllowComments,
                options.CommentChar,
                options.EscapeChar,
                options.QuoteChar,
                options.IgnoreBadData,
                options.AutoDetectEncoding,
                options.IgnoreBlankLines,
                false,
                options.CsvMode,
                options.WhitespaceTrimOptions,
                options.SkipLeadingRowsCount is not null,
                options.SkipLeadingRowsCount ?? 0)
            .ToArray(),
            encoding);

    private static T WithFile<T>(string content, Func<string, T> func, Encoding? encoding = null)
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"{nameof(CsvParsingTests)}_{Guid.NewGuid():N}.csv");

        File.WriteAllText(fileName, content, encoding ?? new UTF8Encoding(false));

        try
        {
            return func(fileName);
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
