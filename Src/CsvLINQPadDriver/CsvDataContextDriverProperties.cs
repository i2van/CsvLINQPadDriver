﻿using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using CsvLINQPadDriver.Extensions;

using LINQPad.Extensibility.DataContext;

namespace CsvLINQPadDriver;

public class CsvDataContextDriverProperties : CsvDataContextDriverPropertiesBase
{
    private readonly XElement _driverData;

    public CsvDataContextDriverProperties(IConnectionInfo connectionInfo) =>
        _driverData = connectionInfo.DriverData;

    public override bool IsProduction
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool Persist
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override string Files
    {
        get => GetValue(string.Empty)!;
        set => SetValue(value);
    }

    public override FileType FileType
    {
        get => GetValue(FileType.CSV);
        set => SetValue(value);
    }

    public override FilesOrderBy FilesOrderBy
    {
        get => GetValue(FilesOrderBy.None);
        set => SetValue(value);
    }

    public override NoBomEncoding NoBomEncoding
    {
        get => GetValue(NoBomEncoding.UTF8);
        set => SetValue(value);
    }

    public override bool IgnoreBadData
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool AutoDetectEncoding
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool AllowComments
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override string CommentChars
    {
        get => GetValue(string.Empty)!;
        set => SetValue(value);
    }

    public override bool UseEscapeChar
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override string EscapeChars
    {
        get => GetValue(string.Empty)!;
        set => SetValue(value);
    }

    public override bool UseQuoteChar
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override string QuoteChars
    {
        get => GetValue(string.Empty)!;
        set => SetValue(value);
    }

    public override string CsvSeparator
    {
        get => GetValue(string.Empty)!;
        set => SetValue(value);
    }

    public override bool IgnoreBlankLines
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool AddHeader
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override HeaderDetection HeaderDetection
    {
        get => GetValue(HeaderDetection.AllLettersNumbersPunctuation);
        set => SetValue(value);
    }

    public override HeaderFormat HeaderFormat
    {
        get => GetValue(HeaderFormat.c1);
        set => SetValue(value);
    }

    public override bool AllowSkipLeadingRows
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    private const int DefaultSkipLeadingRowsCount = 0;
    private const int MaxSkipLeadingRowsCount     = 999;

    public override int SkipLeadingRowsCount
    {
        get => GetValue(DefaultSkipLeadingRowsCount,
                        static v => v is >= DefaultSkipLeadingRowsCount and <= MaxSkipLeadingRowsCount
                            ? v
                            : DefaultSkipLeadingRowsCount);
        set => SetValue(value);
    }

    public override bool AllowCsvMode
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override CsvModeOptions CsvMode
    {
        get => GetValue(CsvModeOptions.Default);
        set => SetValue(value);
    }

    public override bool TrimSpaces
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override WhitespaceTrimOptions WhitespaceTrimOptions
    {
        get => GetValue(WhitespaceTrimOptions.All);
        set => SetValue(value);
    }

    public override bool UseCsvHelperSeparatorAutoDetection
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool RenameTable
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override TableNameFormat TableNameFormat
    {
        get => GetValue(TableNameFormat.table1);
        set => SetValue(value);
    }

    public override bool UseRecordType
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool UseSingleClassForSameFiles
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool ShowSameFilesNonGrouped
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override StringComparison StringComparison
    {
        get => GetValue(StringComparison.Ordinal);
        set => SetValue(value);
    }

    public override bool DetectRelations
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool HideRelationsFromDump
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool DebugInfo
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool ValidateFilePaths
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool IgnoreInvalidFiles
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool DoNotLockFiles
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool IsStringInternEnabled
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public override bool UseStringComparerForStringIntern
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    public override bool IsCacheEnabled
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    private T GetValue<T>(Func<string?, T> convert, T defaultValue, [CallerMemberName] string callerMemberName = "") =>
        convert(_driverData.Element(callerMemberName)?.Value) ?? defaultValue;

    private bool GetValue(bool defaultValue, [CallerMemberName] string callerMemberName = "") =>
        GetValue(static v => v.ToBoolSafe(), defaultValue, callerMemberName)!.Value;

    private int GetValue(int defaultValue, Func<int, int> adjustValueFunc, [CallerMemberName] string callerMemberName = "") =>
        adjustValueFunc(GetValue(static v => v.ToIntSafe(), defaultValue, callerMemberName)!.Value);

    private string? GetValue(string defaultValue, [CallerMemberName] string callerMemberName = "") =>
        GetValue(static v => v, defaultValue, callerMemberName);

    private T GetValue<T>(T defaultValue, [CallerMemberName] string callerMemberName = "")
#if NETCOREAPP
        where T: Enum =>
        (T)
#else
        where T : struct =>
#endif
        GetValue(v =>
#if NETCOREAPP
            Enum.TryParse(typeof(T), v, out var val)
#else
            Enum.TryParse(v, out T val)
#endif
                ? val
                : defaultValue,
            defaultValue, callerMemberName)!;

    private void SetValue<T>(T value, [CallerMemberName] string callerMemberName = "") =>
        _driverData.SetElementValue(callerMemberName, value);
}
