﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CsvLINQPadDriver.CodeGen;

internal sealed class CsvTableEnumerable<TRow> : CsvTableBase<TRow>
    where TRow : ICsvRowBase, new()
{
    public CsvTableEnumerable(
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
        : base(
            isStringInternEnabled,
            internStringComparer,
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
            addHeader,
            headerDetection,
            csvMode,
            whitespaceTrimOptions,
            allowSkipLeadingRows,
            skipLeadingRowsCount,
            filePath,
            propertiesInfo,
            relationsInit)
    {
    }

    public override IEnumerator<TRow> GetEnumerator() =>
        ReadData().GetEnumerator();

    public override IEnumerable<TRow> WhereIndexed(Func<TRow, string> getProperty, string propertyName, params string[] values) =>
        this.Where(row => values.Contains(getProperty(row), StringComparer));
}
