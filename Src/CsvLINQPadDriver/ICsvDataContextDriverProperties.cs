﻿using System;
using System.Collections.Generic;

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace CsvLINQPadDriver;

public interface ICsvDataContextDriverProperties
{
    /// <summary>
    /// The data is the production one.
    /// </summary>
    bool IsProduction { get; set; }

    /// <summary>
    /// The connection is the persistent one.
    /// </summary>
    bool Persist { get; set; }

    /// <summary>
    /// The path to the directory with the CSV files or to the CSV file.
    /// </summary>
    string Files { get; set; }

    /// <summary>
    /// The default file type.
    /// </summary>
    FileType FileType { get; set; }

    /// <summary>
    /// The files order.
    /// </summary>
    FilesOrderBy FilesOrderBy { get; set; }

    /// <summary>
    /// The encoding for the files without BOM.
    /// </summary>
    NoBomEncoding NoBomEncoding { get; set; }

    /// <summary>
    /// Indicates that the malformed CSV data should be ignored.
    /// </summary>
    bool IgnoreBadData { get; set; }

    /// <summary>
    /// Indicates that the encoding should be auto-detected.
    /// </summary>
    bool AutoDetectEncoding { get; set; }

    /// <summary>
    /// Indicates that the CSV comments should be processed.
    /// </summary>
    bool AllowComments { get; set; }

    /// <summary>
    /// The single-line comment characters.
    /// </summary>
    string CommentChars { get; set; }

    /// <summary>
    /// The single-line comment character.
    /// </summary>
    char? CommentChar { get; }

    /// <summary>
    /// Indicates that the escape character should be used.
    /// </summary>
    bool UseEscapeChar { get; set; }

    /// <summary>
    /// The escape characters.
    /// </summary>
    string EscapeChars { get; set; }

    /// <summary>
    /// The escape character.
    /// </summary>
    char? EscapeChar { get; }

    /// <summary>
    /// Indicates that the quote character should be used.
    /// </summary>
    bool UseQuoteChar { get; set; }

    /// <summary>
    /// The quote characters.
    /// </summary>
    string QuoteChars { get; set; }

    /// <summary>
    /// The quote character.
    /// </summary>
    char? QuoteChar { get; }

    /// <summary>
    /// The parsed files.
    /// </summary>
    IEnumerable<string> ParsedFiles { get; }

    /// <summary>
    /// The default CSV separator. If empty or <c>null</c>, the separator will be auto-detected.
    /// </summary>
    string CsvSeparator { get; set; }

    /// <summary>
    /// The safe CSV separator.
    /// </summary>
    string? SafeCsvSeparator { get; }

    /// <summary>
    /// Indicates that the blank lines should be ignored.
    /// </summary>
    bool IgnoreBlankLines { get; }

    /// <summary>
    /// Indicates that the header should be added.
    /// </summary>
    bool AddHeader { get; }

    /// <summary>
    /// The header detection method.
    /// </summary>
    HeaderDetection HeaderDetection { get; }

    /// <summary>
    /// The header format.
    /// </summary>
    HeaderFormat HeaderFormat { get; }

    /// <summary>
    /// Indicates that the leading rows should be skipped.
    /// </summary>
    bool AllowSkipLeadingRows { get; set; }

    /// <summary>
    /// The number of leading rows to skip.
    /// </summary>
    int SkipLeadingRowsCount { get; set; }

    /// <summary>
    /// Indicates that the spaces should be trimmed.
    /// </summary>
    bool TrimSpaces { get; }

    /// <summary>
    /// Indicates that the CSV parsing mode should be used.
    /// </summary>
    bool AllowCsvMode { get; set; }

    /// <summary>
    /// The CSV parsing mode.
    /// </summary>
    CsvModeOptions CsvMode { get; set; }

    /// <summary>
    /// The spaces trimming method.
    /// </summary>
    WhitespaceTrimOptions WhitespaceTrimOptions { get; }

    /// <summary>
    /// Indicates that the CsvHelper separator auto-detection should be used.
    /// </summary>
    bool UseCsvHelperSeparatorAutoDetection { get; }

    /// <summary>
    /// Indicates that the table should be renamed.
    /// </summary>
    bool RenameTable { get; set; }

    /// <summary>
    /// The table name format.
    /// </summary>
    TableNameFormat TableNameFormat { get; set; }

    /// <summary>
    /// Indicates that the records should be created instead of the classes.
    /// </summary>
    bool UseRecordType { get; set; }

    /// <summary>
    /// Indicates that the single class should be generated for the similar CSV files.
    /// </summary>
    bool UseSingleClassForSameFiles { get; set; }

    /// <summary>
    /// Indicates that the similar files should be shown non-grouped in addition to the similar files groups.
    /// </summary>
    bool ShowSameFilesNonGrouped { get; set; }

    /// <summary>
    /// The generated class string comparison.
    /// </summary>
    StringComparison StringComparison { get; set; }

    /// <summary>
    /// Indicates that relations based on the files and the column names between the CSV files should be detected and created.
    /// </summary>
    bool DetectRelations { get; set; }

    /// <summary>
    /// Indicates that the LINQPad should not show the relations content in .Dump()
    /// </summary>
    /// <remarks>
    /// Prevents from loading too many data.
    /// </remarks>>
    bool HideRelationsFromDump { get; set; }

    /// <summary>
    /// Indicates that the debug info should be generated.
    /// </summary>
    bool DebugInfo { get; set; }

    /// <summary>
    /// Indicates that the file paths should be validated.
    /// </summary>
    bool ValidateFilePaths { get; set; }

    /// <summary>
    /// Indicates that the beginning of every file should be scanned and the suspicious files with the format not similar to CSV should be ignored.
    /// </summary>
    bool IgnoreInvalidFiles { get; set; }

    /// <summary>
    /// Indicates that the other processes can modify the files being read.
    /// </summary>
    bool DoNotLockFiles { get; set; }

    /// <summary>
    /// Indicates that all the strings are interned.
    /// </summary>
    /// <remarks>
    /// <b>May significantly reduce</b> memory consumption, especially when values in CSV are repeated many times; <b>may significantly increase</b> memory usage otherwise.
    /// Custom per context interning is used.
    /// </remarks>>
    bool IsStringInternEnabled { get; set; }

    /// <summary>
    /// Indicates that the strings interning uses the generation string comparer.
    /// </summary>
    bool UseStringComparerForStringIntern { get; set; }

    /// <summary>
    /// Indicates that the parsed rows are cached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cache survives multiple query runs, even when query is changed.
    /// Cache is cleared as soon as LINQPad clears Application Domain of query.
    /// <b>May significantly increase</b> memory usage.
    /// </para>
    /// <para>
    /// When disabled multiple enumerations of file content results in multiple reads and parsing of file.
    /// Can be significantly slower for complex queries.
    /// <b>Significantly reduces</b> memory usage. Useful when reading very large files.
    /// </para>
    /// </remarks>
    bool IsCacheEnabled { get; set; }
}
