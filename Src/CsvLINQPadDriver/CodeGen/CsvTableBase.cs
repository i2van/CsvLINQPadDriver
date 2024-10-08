﻿using System;
using System.Collections;
using System.Collections.Generic;

using CsvLINQPadDriver.Extensions;

namespace CsvLINQPadDriver.CodeGen;

public class CsvTableBase
{
    public static readonly StringComparer StringComparer = StringComparer.Ordinal;

    internal bool IsStringInternEnabled { get; }

    protected CsvTableBase(bool isStringInternEnabled) =>
        IsStringInternEnabled = isStringInternEnabled;
}

public abstract class CsvTableBase<TRow> : CsvTableBase, IEnumerable<TRow>
    where TRow : ICsvRowBase, new()
{
    private static CsvRowMappingBase<TRow>? _cachedCsvRowMappingBase;

    private readonly string? _csvSeparator;
    private readonly NoBomEncoding _noBomEncoding;
    private readonly StringComparer? _internStringComparer;
    private readonly bool _allowComments;
    private readonly char? _commentChar;
    private readonly char? _escapeChar;
    private readonly char? _quoteChar;
    private readonly bool _ignoreBadData;
    private readonly bool _autoDetectEncoding;
    private readonly bool _ignoreBlankLines;
    private readonly bool _doNotLockFiles;
    private readonly bool _addHeader;
    private readonly HeaderDetection? _headerDetection;
    private readonly CsvModeOptions? _csvMode;
    private readonly WhitespaceTrimOptions? _whitespaceTrimOptions;
    private readonly bool _allowSkipLeadingRows;
    private readonly int _skipLeadingRowsCount;

    protected readonly string FilePath;

    protected CsvTableBase(
        bool isStringInternEnabled,
        StringComparer? internStringComparer,
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
        HeaderDetection? headerDetection,
        CsvModeOptions? csvMode,
        WhitespaceTrimOptions? whitespaceTrimOptions,
        bool allowSkipLeadingRows,
        int skipLeadingRowsCount,
        string filePath,
        IEnumerable<CsvColumnInfo> propertiesInfo,
        Action<TRow> relationsInit)
        : base(isStringInternEnabled)
    {
        _internStringComparer = internStringComparer;
        _csvSeparator = csvSeparator;
        _noBomEncoding = noBomEncoding;
        _allowComments = allowComments;
        _commentChar = commentChar;
        _escapeChar = escapeChar;
        _quoteChar = quoteChar;
        _ignoreBadData = ignoreBadData;
        _autoDetectEncoding = autoDetectEncoding;
        _ignoreBlankLines = ignoreBlankLines;
        _doNotLockFiles = doNotLockFiles;
        _addHeader = addHeader;
        _headerDetection = headerDetection;
        _csvMode = csvMode;
        _whitespaceTrimOptions = whitespaceTrimOptions;
        _allowSkipLeadingRows = allowSkipLeadingRows;
        _skipLeadingRowsCount = skipLeadingRowsCount;

        FilePath = filePath;

#pragma warning disable S3010
        _cachedCsvRowMappingBase ??= new CsvRowMappingBase<TRow>(propertiesInfo, relationsInit);
#pragma warning restore S3010
    }

    protected IEnumerable<TRow> ReadData() =>
        FilePath.CsvReadRows(
            _csvSeparator,
            IsStringInternEnabled,
            _internStringComparer,
            _noBomEncoding,
            _allowComments,
            _escapeChar,
            _quoteChar,
            _commentChar,
            _ignoreBadData,
            _autoDetectEncoding,
            _ignoreBlankLines,
            _doNotLockFiles,
            _addHeader,
            _headerDetection,
            _csvMode,
            _whitespaceTrimOptions,
            _allowSkipLeadingRows,
            _skipLeadingRowsCount,
            _cachedCsvRowMappingBase!);

    // ReSharper disable once UnusedMember.Global
    public abstract IEnumerable<TRow> WhereIndexed(Func<TRow, string> getProperty, string propertyName, params string[] values);

    public abstract IEnumerator<TRow> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
