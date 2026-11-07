using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Humanizer;

using UnicodeCharsetDetector;

using CsvHelper;

using CsvLINQPadDriver.CodeGen;

#if NETCOREAPP
using System.Collections.Immutable;
#else
using CsvLINQPadDriver.Bcl.Extensions;
#endif

namespace CsvLINQPadDriver.Extensions;

internal static partial class FileExtensions
{
    public const string InlineComment = "#";

    public static readonly StringComparer FileNameComparer = StringComparer.OrdinalIgnoreCase;

    private const string RecursiveMaskMarker = "**";

    private const StringComparison FileNameComparison = StringComparison.OrdinalIgnoreCase;

    private static readonly UnicodeCharsetDetector.UnicodeCharsetDetector UnicodeCharsetDetector = new();

    private static readonly char[] TsvSeparators   = "\t,;".ToCharArray();
    private static readonly char[] CsvSeparators   = ",;\t".ToCharArray();
    private static readonly char[] WhiteSpaceChars = " \t" .ToCharArray();

    private static readonly char[] InlineCommentCharArray = InlineComment.ToCharArray();

    private static
#if NETCOREAPP
        HashSet<string>?
#else
        Dictionary<string, string>?
#endif
        _stringInternCache;

    private static readonly Dictionary<NoBomEncoding, Encoding> NoBomEncodings = [];

    private sealed record SupportedFileType(FileType FileType, string Extension, string Description)
    {
        private readonly string? _mask;

        public string Mask
        {
            get => _mask ?? Extension;
            init => _mask = value;
        }
    }

    private static readonly SupportedFileType[] SupportedFileTypes =
    [
        new ( FileType.CSV,  "csv", "CSV"  ),
        new ( FileType.TSV,  "tsv", "TSV"  ),
        new ( FileType.Text, "txt", "Text" ),
        new ( FileType.Log,  "log", "Log"  ),
        new ( FileType.All,  "",    "All"  ) { Mask = "*" }
    ];

    private static readonly HashSet<string> SupportedFileExtensions = new(
        SupportedFileTypes
            .Where(static supportedFileType => !string.IsNullOrWhiteSpace(supportedFileType.Extension))
            .Select(static supportedFileType => $".{supportedFileType.Extension}"),
        FileNameComparer);

    private static readonly FileType DefaultFileType = SupportedFileTypes[0].FileType;
    private static readonly string DefaultMask = GetMask(DefaultFileType);

#if NETCOREAPP
    static FileExtensions() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

    public static string GetMask(this FileType fileType, bool recursive = false) =>
        $"{(recursive ? RecursiveMaskMarker : "*")}.{fileType.GetSupportedFileType().Mask}";

    public static string GetExtension(this FileType fileType) =>
        fileType.GetSupportedFileType().Extension;

    public static int GetFilterIndex(this FileType fileType) =>
        Array.FindIndex(SupportedFileTypes, supportedFileType => supportedFileType.FileType == fileType) + 1;

    private static SupportedFileType GetSupportedFileType(this FileType fileType) =>
        SupportedFileTypes.FirstOrDefault(supportedFileType => supportedFileType.FileType == fileType) ?? throw new InvalidEnumArgumentException($"Unknown {nameof(FileType)} {fileType}");

    public static readonly string Filter = string.Join("|", SupportedFileTypes.Select(static supportedFileType => $"{supportedFileType.Description} Files (*.{supportedFileType.Mask})|*.{supportedFileType.Mask}"));

    public static IEnumerable<T> CsvReadRows<T>(
        this string fileName,
        string? csvSeparator,
        bool internString,
        StringComparer? internStringComparer,
        NoBomEncoding noBomEncoding,
        bool allowComments,
        char? commentChar,
        char? escapeChar,
        char? quoteChar,
        bool ignoreBadData,
        bool autoDetectEncoding,
        bool ignoreBlankLines,
        bool doNotLockFiles,
        bool addHeader,
        HeaderDetection? headerDetection,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount,
        CsvRowMappingBase<T> csvClassMap)
        where T : ICsvRowBase, new()
    {
        InitStringInternCache();

        return
            SkipHeader(
                CsvReadRows(
                    fileName,
                    csvSeparator,
                    noBomEncoding,
                    allowComments,
                    commentChar,
                    escapeChar,
                    quoteChar,
                    ignoreBadData,
                    autoDetectEncoding,
                    ignoreBlankLines,
                    doNotLockFiles,
                    csvMode,
                    whitespaceTrimOptions,
                    allowSkipLeadingRows,
                    skipLeadingRowsCount))
            .Select(GetRecord);

        IEnumerable<string[]> SkipHeader(IEnumerable<string[]> rows)
        {
            using var enumerator = rows.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var columnHeader = enumerator.Current;

                if (addHeader && !columnHeader.IsPresent(true, headerDetection ?? default))
                {
                    yield return columnHeader;
                }
            }

            while(enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        T GetRecord(string?[] columns)
        {
            if (internString)
            {
                for (var i = 0; i < columns.Length; i++)
                {
                    columns[i] = StringIntern(columns[i]);
                }
            }

            return csvClassMap.InitRowObject(columns);
        }

        void InitStringInternCache() =>
            _stringInternCache ??= internStringComparer is null
                ? []
                : new(internStringComparer);
    }

    public static IEnumerable<string> CsvReadHeader(
        this string fileName,
        string? csvSeparator,
        NoBomEncoding noBomEncoding,
        bool allowComments,
        char? commentChar,
        char? escapeChar,
        char? quoteChar,
        bool ignoreBadData,
        bool autoDetectEncoding,
        bool ignoreBlankLines,
        bool doNotLockFiles,
        bool addHeader,
        HeaderDetection headerDetection,
        HeaderFormat headerFormat,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount)
    {
        using var csvParser = CreateCsvParser(
            fileName,
            csvSeparator,
            noBomEncoding,
            allowComments,
            commentChar,
            escapeChar,
            quoteChar,
            ignoreBadData,
            autoDetectEncoding,
            ignoreBlankLines,
            doNotLockFiles,
            csvMode,
            whitespaceTrimOptions,
            allowSkipLeadingRows,
            skipLeadingRowsCount);

        return csvParser.Read()
            ? GetHeader()
            : [];

        string[] GetHeader()
        {
            var header = csvParser.GetRecord();
            var headerFormatFunc = headerFormat.GetFormatFunc();

            if (!header!.IsPresent(addHeader, headerDetection))
            {
                return
                    addHeader
                        ? Enumerable
                            .Range(0, header!.Length)
                            .Select(headerFormatFunc)
                            .ToArray()
                        : header!;
            }

            var getUniqueFallbackColumnNameFunc = GetUniqueFallbackColumnNameGenerator();

            return AdjustColumnNames();

            string[] AdjustColumnNames()
            {
                for (var i = 0; i < header!.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(header[i]))
                    {
                        header[i] = getUniqueFallbackColumnNameFunc();
                    }
                }

                return header;
            }

            Func<string> GetUniqueFallbackColumnNameGenerator()
            {
                // ReSharper disable once GenericEnumeratorNotDisposed
                var enumerator = Enumerable.Range(0, int.MaxValue).GetEnumerator();
                var lookup = new Lazy<HashSet<string>>(() => [..header!], LazyThreadSafetyMode.None);

                return GetUniqueFallbackColumnName;

                string GetUniqueFallbackColumnName()
                {
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        var columnName = headerFormatFunc(current);
                        if (lookup.Value.Add(columnName))
                        {
                            return columnName;
                        }
                    }

                    throw new InvalidOperationException("Unexpected error occurred");
                }
            }
        }
    }

    public static char CsvDetectSeparator(this string fileName, bool doNotLockFiles, bool debugInfo)
    {
        var defaultCsvSeparators = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".tsv" => TsvSeparators,
            _      => CsvSeparators
        };

        var csvSeparator = defaultCsvSeparators[0];

        var defaultCsvSeparator = csvSeparator;

        try
        {
            // Get most used char from separators as separator.
            csvSeparator = GetHeaderChars()
                .Where(defaultCsvSeparators.Contains)
                .GroupBy(static ch => ch)
                .OrderByDescending(static chGroup => chGroup.Count())
                .Select(static chGroup => chGroup.Key)
                .DefaultIfEmpty(csvSeparator)
                .First();

            IEnumerable<char> GetHeaderChars()
            {
                using var streamReader = new StreamReader(OpenFile(fileName, doNotLockFiles));
                return streamReader.ReadLine()?.ToCharArray() ?? [];
            }
        }
        catch (Exception exception) when (exception.CanBeHandled())
        {
            $"CSV separator detection failed for {fileName}".WriteToLog(debugInfo, exception);
        }

        if (csvSeparator != defaultCsvSeparator)
        {
            $"Using CSV separator '{csvSeparator}' for {fileName}".WriteToLog(debugInfo);
        }

        return csvSeparator;
    }

    public static bool IsCsvFormatValid(
        this string fileName,
        string? csvSeparator,
        NoBomEncoding noBomEncoding,
        bool allowComments,
        char? commentChar,
        char? escapeChar,
        char? quoteChar,
        bool ignoreBadData,
        bool autoDetectEncoding,
        bool ignoreBlankLines,
        bool doNotLockFiles,
        bool debugInfo,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount)
    {
        var header = $"{fileName} is not valid CSV file:";

        try
        {
            using var csvParser = CreateCsvParser(
                fileName,
                csvSeparator,
                noBomEncoding,
                allowComments,
                commentChar,
                escapeChar,
                quoteChar,
                ignoreBadData,
                autoDetectEncoding,
                ignoreBlankLines,
                doNotLockFiles,
                csvMode,
                whitespaceTrimOptions,
                allowSkipLeadingRows,
                skipLeadingRowsCount);

            if (!csvParser.Read())
            {
                $"{header} could not get CSV header".WriteToLog(debugInfo);

                return false;
            }

            var headerRow = csvParser.GetRecord();

            // No columns.
            if (!headerRow!.Any())
            {
                $"{header} CSV header had no columns".WriteToLog(debugInfo);

                return false;
            }

            if (!csvParser.Read())
            {
                $"{header} CSV has header but has no data".WriteToLog(debugInfo);

                return false;
            }

            var dataRow = csvParser.GetRecord();

            // Column count differs.
            if (headerRow!.Length != dataRow!.Length)
            {
                $"{header} CSV header column count does not match data column count".WriteToLog(debugInfo);

                return false;
            }

            // Too many strange characters.
            var charsCount = headerRow
                .Concat(dataRow)
                .Sum(static s => s.Length);

            var validCharsCount = headerRow
                .Concat(dataRow)
                .Sum(static s => Enumerable.Range(0, s.Length).Count(i => char.IsLetterOrDigit(s, i)));

            const double validCharsMinOkRatio = 0.5;

            return validCharsCount >= validCharsMinOkRatio * charsCount;
        }
        catch(Exception exception) when (exception.CanBeHandled())
        {
            $"{header} failed with exception".WriteToLog(debugInfo, exception);

            return false;
        }
    }

    public static string GetLongestCommonPrefixPath(this IEnumerable<string> paths)
    {
        var pathsValid = paths.GetFiles().ToImmutableList();

        // Get the longest common path prefix.
        var filePaths = pathsValid.FirstOrDefault()?.Split(Path.DirectorySeparatorChar) ?? [];

        var directorySeparator = Path.DirectorySeparatorChar
#if !NETCOREAPP
            .ToString()
#endif
            ;

        return Enumerable.Range(1, filePaths.Length)
            .Select(i => string.Join(directorySeparator, filePaths.Take(i).ToImmutableList()))
            .LastOrDefault(prefix => pathsValid.TrueForAll(path => path.StartsWith(prefix, FileNameComparison))) ?? string.Empty;
    }

    public static IEnumerable<string> EnumFiles(this IEnumerable<string> paths, ICollection<Exception>? exceptions = null) =>
        paths
            .GetFiles()
            .SelectMany(files => EnumFiles(files, exceptions))
            .Distinct(FileNameComparer);

    public static string GetHumanizedFileSize(this string fileName, bool debugInfo) =>
        GetHumanizedFileSize(GetFileSize(fileName, debugInfo));

    public static string GetHumanizedFileSize(this IEnumerable<string> files, bool debugInfo) =>
        GetHumanizedFileSize(files.Sum(fileName => GetFileSize(fileName, debugInfo)));

    public static IEnumerable<string> GetFiles(this string paths) =>
        GetFilesRegex().Split(paths).GetFiles();

    private static IEnumerable<string> GetFiles(this IEnumerable<string> paths) =>
        paths
            .Select(static path => path.Trim())
            .Where(static path => !path.IsInlineComment())
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(FileNameComparer);

    public static IEnumerable<string> OrderFiles(this IEnumerable<string> files, FilesOrderBy filesOrderBy)
    {
        if (filesOrderBy == FilesOrderBy.None)
        {
            return files;
        }

        var fileInfos = files.Select(static file => new FileInfo(file));

        fileInfos = filesOrderBy switch
        {
            FilesOrderBy.NameAsc           => fileInfos.OrderBy(static fileInfo => fileInfo.Name, FileNameComparer),
            FilesOrderBy.NameDesc          => fileInfos.OrderByDescending(static fileInfo => fileInfo.Name, FileNameComparer),
            FilesOrderBy.SizeAsc           => fileInfos.OrderBy(static fileInfo => fileInfo.Length),
            FilesOrderBy.SizeDesc          => fileInfos.OrderByDescending(static fileInfo => fileInfo.Length),
            FilesOrderBy.LastWriteTimeAsc  => fileInfos.OrderBy(static fileInfo => fileInfo.LastWriteTimeUtc),
            FilesOrderBy.LastWriteTimeDesc => fileInfos.OrderByDescending(static fileInfo => fileInfo.LastWriteTimeUtc),
            _                              => throw new InvalidEnumArgumentException($"Unknown {nameof(FilesOrderBy)} {filesOrderBy}")
        };

        return fileInfos.Select(static fileInfo => fileInfo.FullName);
    }

    public static string GetInlineCommentContent(this string line) =>
        line.Trim().TrimStart(InlineCommentCharArray);

    private static long GetFileSize(string fileName, bool debugInfo)
    {
        try
        {
            return new FileInfo(fileName).Length;
        }
        catch (Exception exception) when (exception.CanBeHandled())
        {
            $"Failed to get {fileName} size".WriteToLog(debugInfo, exception);
            return 0;
        }
    }

    private static string GetHumanizedFileSize(long size) =>
        size.Bytes().Humanize("0.#");

    private static CsvReader CreateCsvParser(
        string fileName,
        string? csvSeparator,
        NoBomEncoding noBomEncoding,
        bool allowComments,
        char? commentChar,
        char? escapeChar,
        char? quoteChar,
        bool ignoreBadData,
        bool autoDetectEncoding,
        bool ignoreBlankLines,
        bool doNotLockFiles,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount)
    {
        const int bufferSize = 4096 * 20;

        const char defaultQuoteChar  = '"';
        const char defaultEscapeChar = '"';

        var separator = GetSeparator();
        var mode      = GetCsvMode();
        var quote     = quoteChar ?? defaultQuoteChar;
        var escape    = mode == CsvMode.Escape ? escapeChar ?? defaultEscapeChar : CsvReaderOptions.Default.Escape;

        // Separator within whitespace characters is trimmed inside quotes only, outside quotes it still separates fields.
        var trimSeparator = whitespaceTrimOptions is WhitespaceTrimOptions.AllIncludingSeparator or WhitespaceTrimOptions.InsideQuotesIncludingSeparator;

        var csvReaderOptions = new CsvReaderOptions
        {
            Separator           = separator,
            DetectSeparatorHint = false,
            CommentPrefix       = allowComments ? commentChar ?? InlineComment[0] : null,
            HasHeader           = false,
            FieldCountMismatch  = CsvFieldCountHandling.Allow,
            MissingField        = CsvMissingFieldHandling.ReturnEmpty,
            MalformedData       = ignoreBadData ? CsvMalformedDataHandling.Recover : CsvMalformedDataHandling.Throw,
            SkipEmptyLines      = ignoreBlankLines,
            Mode                = mode,
            Quote               = quote,
            Escape              = escape,
            TrimWhitespace      = whitespaceTrimOptions is WhitespaceTrimOptions.All or WhitespaceTrimOptions.Trim or WhitespaceTrimOptions.AllIncludingSeparator,
            TrimInsideQuotes    = whitespaceTrimOptions is WhitespaceTrimOptions.All or WhitespaceTrimOptions.InsideQuotes or WhitespaceTrimOptions.AllIncludingSeparator or WhitespaceTrimOptions.InsideQuotesIncludingSeparator,
            WhitespaceCharacters = new string(WhiteSpaceChars
                .Where(ch => (trimSeparator || ch != separator) && ch != quote && ch != escape)
                .ToArray()),
            BufferSize          = bufferSize
        };

        var stream = OpenFile(fileName, doNotLockFiles);

        var encoding = (autoDetectEncoding ? DetectEncoding(stream) : null) ?? GetFallbackEncoding(noBomEncoding);

        stream.Position = 0;

        var csvParser = CsvReader.Create(new StreamReader(stream, encoding, !autoDetectEncoding, bufferSize / sizeof(char)), csvReaderOptions);

        SkipLeadingRows();

        return csvParser;

        char? GetSeparator()
        {
            if (csvSeparator is null)
            {
                return null;
            }

            if (csvSeparator.Length != 1 || csvSeparator[0] > 127)
            {
                throw new NotSupportedException($"CSV separator '{csvSeparator}' is not supported: it must be a single ASCII character");
            }

            return csvSeparator[0];
        }

        CsvMode GetCsvMode() =>
            csvMode switch
            {
                null or CsvModeOptions.Default     => CsvMode.Rfc4180,
                CsvModeOptions.Escape              => CsvMode.Escape,
                CsvModeOptions.NoEscape            => CsvMode.NoEscape,
                _                                  => throw new InvalidEnumArgumentException($"Unknown {nameof(CsvModeOptions)} {csvMode}")
            };

        void SkipLeadingRows()
        {
            if (!allowSkipLeadingRows)
            {
                return;
            }

            while (skipLeadingRowsCount-- > 0 && csvParser.Read())
            {
                // Skip row.
            }
        }
    }

    public sealed record DeduceFileOrFolderResult(bool IsFile, string Path);

    public static DeduceFileOrFolderResult DeduceIsFileOrFolder(this string path, bool removeMask = false) =>
        MatchDeduceIsFileOrFolderRegex().IsMatch(path)
            ? new (false, path)
            : DeduceIsFileOrFolderRegex().IsMatch(Path.GetFileName(path))
                ? new (true, removeMask ? Path.GetDirectoryName(path) ?? path : path)
                : new (SupportedFileExtensions.Contains(Path.GetExtension(path)), path);

    public static void Add(this ICollection<Exception>? exceptions, string file, Exception exception) =>
        exceptions?.Add(file, $"processing failed: {exception.Message}");

    public static void Add(this ICollection<Exception>? exceptions, string file, string message) =>
        exceptions?.Add(new Exception($"'{file}' {message}".AppendDot()));

    private static IEnumerable<string> EnumFiles(string path, ICollection<Exception>? exceptions = null)
    {
        try
        {
            var fileOrMask = Path.GetFileName(path);

            string baseDir;

            var isPathOnlyDir = string.IsNullOrWhiteSpace(fileOrMask) || Directory.Exists(path);
            if (isPathOnlyDir)
            {
                fileOrMask = DefaultMask;
                baseDir = path;
            }
            else
            {
                baseDir = Path.GetDirectoryName(path) ?? string.Empty;
            }

            return
#if NETCOREAPP
                Directory
#else
                exceptions
#endif
                    .EnumerateFiles(baseDir, fileOrMask,
                        Path.GetFileNameWithoutExtension(fileOrMask).Contains(RecursiveMaskMarker)
                            ? SearchOption.AllDirectories
                            : SearchOption.TopDirectoryOnly)
                    .SkipExceptions(exceptions);
        }
        catch (Exception exception) when (exception.CanBeHandled())
        {
            exceptions.Add(path, exception);

            return [];
        }
    }

    internal static IEnumerable<string[]> CsvReadRows(
        string fileName,
        string? csvSeparator,
        NoBomEncoding noBomEncoding,
        bool allowComments,
        char? commentChar,
        char? escapeChar,
        char? quoteChar,
        bool ignoreBadData,
        bool autoDetectEncoding,
        bool ignoreBlankLines,
        bool doNotLockFiles,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount)
    {
        using var csvParser = CreateCsvParser(
            fileName,
            csvSeparator,
            noBomEncoding,
            allowComments,
            commentChar,
            escapeChar,
            quoteChar,
            ignoreBadData,
            autoDetectEncoding,
            ignoreBlankLines,
            doNotLockFiles,
            csvMode,
            whitespaceTrimOptions,
            allowSkipLeadingRows,
            skipLeadingRowsCount);

        while (csvParser.Read())
        {
            yield return csvParser.GetRecord();
        }
    }

    private static string[] GetRecord(this CsvReader csvReader)
    {
        var record = new string[csvReader.FieldCount];

        for (var i = 0; i < record.Length; i++)
        {
            record[i] = csvReader.GetString(i);
        }

        return record;
    }

    private static bool IsPresent(this IEnumerable<string> header, bool addHeader, HeaderDetection headerDetection)
    {
        return
            addHeader &&
            TryGetHeaderDetectionRegex(headerDetection, out var headerDetectionRegex) &&
            header.All(columnHeader => string.IsNullOrWhiteSpace(columnHeader) || headerDetectionRegex.IsMatch(columnHeader));

        static bool TryGetHeaderDetectionRegex(HeaderDetection headerDetection, [NotNullWhen(true)] out Regex? headerDetectionRegex) =>
            (headerDetectionRegex = headerDetection switch
            {
                HeaderDetection.NoHeader                       => null,
                HeaderDetection.HasHeader                      => HeaderDetectionHasHeaderRegex(),
                HeaderDetection.AllLettersNumbersPunctuation   => HeaderDetectionAllLettersNumbersPunctuationRegex(),
                HeaderDetection.AllLettersNumbers              => HeaderDetectionAllLettersNumbersRegex(),
                HeaderDetection.AllLetters                     => HeaderDetectionAllLettersRegex(),
                HeaderDetection.LatinLettersNumbersPunctuation => HeaderDetectionLatinLettersNumbersPunctuationRegex(),
                HeaderDetection.LatinLettersNumbers            => HeaderDetectionLatinLettersNumbersRegex(),
                HeaderDetection.LatinLetters                   => HeaderDetectionLatinLettersRegex(),
                _                                              => throw new InvalidEnumArgumentException($"Unknown {nameof(HeaderDetection)} {headerDetection}")
            }) is not null;
    }

    private static Encoding GetFallbackEncoding(NoBomEncoding noBomEncoding)
    {
        if (!NoBomEncodings.TryGetValue(noBomEncoding, out var encoding))
        {
            NoBomEncodings.Add(noBomEncoding, encoding = GetEncoding());
        }

        return encoding;

        Encoding GetEncoding()
        {
            return noBomEncoding switch
            {
                NoBomEncoding.UTF8             => Encoding.UTF8,
                NoBomEncoding.Unicode          => Encoding.Unicode,
                NoBomEncoding.BigEndianUnicode => Encoding.BigEndianUnicode,
                NoBomEncoding.UTF32            => Encoding.UTF32,
                NoBomEncoding.BigEndianUTF32   => new UTF32Encoding(true, true),
#pragma warning disable SYSLIB0001
                NoBomEncoding.UTF7             => Encoding.UTF7,
#pragma warning restore SYSLIB0001
                NoBomEncoding.ASCII            => Encoding.ASCII,
                NoBomEncoding.SystemCodePage   => GetCodePageEncoding(false),
                NoBomEncoding.UserCodePage     => GetCodePageEncoding(true),
                _                              => Encoding.GetEncoding(FromCodePage())
            };

            static Encoding GetCodePageEncoding(bool user) =>
                Encoding.GetEncoding(CultureInfo.GetCultureInfo(user ? GetUserDefaultLCID() : GetSystemDefaultLCID()).TextInfo.ANSICodePage);

            int FromCodePage() =>
                Convert.ToInt32(noBomEncoding.ToString()[2..], CultureInfo.InvariantCulture);

            [DllImport("kernel32.dll")]
            static extern int GetSystemDefaultLCID();

            [DllImport("kernel32.dll")]
            static extern int GetUserDefaultLCID();
        }
    }

    private static Encoding? DetectEncoding(Stream fileStream)
    {
        try
        {
            return DetectEncodingInternal();
        }
        catch
        {
            return null;
        }

        Encoding? DetectEncodingInternal()
        {
            var stream = GetStream();

            if (stream is null)
            {
                return null;
            }

            using (stream)
            {
                var charset = UnicodeCharsetDetector.Check(stream);

                stream.Position = 0;

                return DetectAsciiEncoding() ?? charset.ToEncoding();

                Encoding? DetectAsciiEncoding()
                {
                    try
                    {
                        return charset switch
                        {
                            Charset.None  or
                            Charset.Ansi  or
                            Charset.Ascii => UtfUnknown.CharsetDetector.DetectFromStream(stream).Detected?.Encoding,
                            _             => null
                        };
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            Stream? GetStream()
            {
                var buffer = new byte[4096];
                var bufferSize = fileStream.Read(buffer, 0, buffer.Length);

                return bufferSize > 0
                    ? new MemoryStream(buffer, 0, bufferSize, false)
                    : null;
            }
        }
    }

    private static string? StringIntern(string? str)
    {
        if (str is null)
        {
            return null;
        }

        if (_stringInternCache!.TryGetValue(str, out var intern))
        {
            return intern;
        }

        _stringInternCache.Add(str
#if !NETCOREAPP
            , str
#endif
        );

        return str;
    }

    private static bool IsInlineComment(this string line) =>
        line.TrimStart().StartsWith(InlineComment);

    private static Stream OpenFile(string fileName, bool doNotLockFiles) =>
        new FileStream(
            fileName,
            FileMode.Open,
            FileAccess.Read,
            doNotLockFiles
                ? FileShare.ReadWrite
                : FileShare.Read);
}
