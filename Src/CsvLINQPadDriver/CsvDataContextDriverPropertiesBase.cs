﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using CsvLINQPadDriver.Extensions;

// ReSharper disable UnusedMember.Global

namespace CsvLINQPadDriver;

public abstract class CsvDataContextDriverPropertiesBase : ICsvDataContextDriverProperties
{
    public abstract bool IsProduction { get; set; }
    public abstract bool Persist { get; set; }
    public abstract string Files { get; set; }
    public abstract FileType FileType { get; set; }
    public abstract FilesOrderBy FilesOrderBy { get; set; }
    public abstract NoBomEncoding NoBomEncoding { get; set; }
    public abstract bool IgnoreBadData { get; set; }
    public abstract bool AutoDetectEncoding { get; set; }
    public abstract bool AllowComments { get; set; }
    public abstract string CommentChars { get; set; }
    public abstract bool UseQuoteChar { get; set; }
    public abstract string QuoteChars { get; set; }
    public abstract bool UseEscapeChar { get; set; }
    public abstract string EscapeChars { get; set; }

    public char? CommentChar =>
        GetFirstCharOrNull(CommentChars);

    public char? QuoteChar =>
        GetFirstCharOrNull(QuoteChars);

    public char? EscapeChar => GetFirstCharOrNull(EscapeChars);

    private static char? GetFirstCharOrNull(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? null
            : s!.TrimStart()[0];

    public IEnumerable<string> ParsedFiles =>
        Files.GetFiles();

    public abstract string CsvSeparator { get; set; }

    public string? SafeCsvSeparator
    {
        get
        {
            var csvSeparator = CsvSeparator;

            if (string.IsNullOrEmpty(csvSeparator))
            {
                return null;
            }

            try
            {
                return Regex.Unescape(csvSeparator);
            }
            catch (Exception exception) when (exception.CanBeHandled())
            {
                $"Falling back to CSV separator '{csvSeparator}'".WriteToLog(DebugInfo, exception);

                return csvSeparator;
            }
        }
    }

    public abstract bool IgnoreBlankLines { get; set; }
    public abstract bool AddHeader { get; set; }
    public abstract HeaderDetection HeaderDetection { get; set; }
    public abstract HeaderFormat HeaderFormat { get; set; }
    public abstract bool AllowSkipLeadingRows { get; set; }
    public abstract int SkipLeadingRowsCount { get; set; }
    public abstract bool AllowCsvMode { get; set; }
    public abstract CsvModeOptions CsvMode { get; set; }
    public abstract bool TrimSpaces { get; set; }
    public abstract WhitespaceTrimOptions WhitespaceTrimOptions { get; set; }
    public abstract bool UseCsvHelperSeparatorAutoDetection { get; set; }
    public abstract bool RenameTable { get; set; }
    public abstract TableNameFormat TableNameFormat { get; set; }
    public abstract bool UseRecordType { get; set; }
    public abstract bool UseSingleClassForSameFiles { get; set; }
    public abstract bool ShowSameFilesNonGrouped { get; set; }
    public abstract StringComparison StringComparison { get; set; }
    public abstract bool DetectRelations { get; set; }
    public abstract bool HideRelationsFromDump { get; set; }
    public abstract bool DebugInfo { get; set; }
    public abstract bool ValidateFilePaths { get; set; }
    public abstract bool IgnoreInvalidFiles { get; set; }
    public abstract bool DoNotLockFiles { get; set; }
    public abstract bool IsStringInternEnabled { get; set; }
    public abstract bool UseStringComparerForStringIntern { get; set; }
    public abstract bool IsCacheEnabled { get; set; }
}
