﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AwesomeAssertions;

using Moq;

using NUnit.Framework;

using CsvLINQPadDriver;
using CsvLINQPadDriver.Extensions;

using LPRun;

#pragma warning disable IDE0079
#pragma warning disable CA1416
#pragma warning restore IDE0079

namespace CsvLINQPadDriverTest;

[TestFixture]
public sealed partial class LPRunTests
{
    private static readonly string Files = Context.GetDataFullPath("**.csv");

    public sealed record ScriptWithDriverPropertiesTestData(string LinqScriptName, string? Context, ICsvDataContextDriverProperties DriverProperties, params string?[] Defines)
    {
        public int Index { get; init; }
    }

    [Test]
    [TestCaseSource(nameof(ParallelizableScriptWithDriverPropertiesTestDataTestsData))]
    public async Task Execute_ScriptWithDriverProperties_Success(ScriptWithDriverPropertiesTestData testData)
    {
        var (linqScriptName, context, driverProperties, defines) = testData;

        var queryConfig = GetQueryHeaders().Aggregate(new StringBuilder(), static (stringBuilder, header) =>
        {
            if (ShouldRender(header))
            {
                stringBuilder.AppendLine(header);
                stringBuilder.AppendLine();
            }

            return stringBuilder;
        }).ToString();

        var linqScript = LinqScript.FromFile($"{linqScriptName}.linq", queryConfig, $"{linqScriptName}_{testData.Index}");

        Console.Write($"{linqScript}{Environment.NewLine}{Environment.NewLine}{queryConfig}");

        var (output, error, exitCode) = await ExecuteAsync(linqScript);

        if (ShouldRender(output))
        {
            Console.WriteLine(output);
        }

        error.Should().BeNullOrWhiteSpace();
        exitCode.Should().Be(0);

        IEnumerable<string> GetQueryHeaders()
        {
            yield return ConnectionHeader.Get("CsvLINQPadDriver", "CsvLINQPadDriver.CsvDataContextDriver", driverProperties, "System.Numerics", "System.Runtime.CompilerServices", nameof(CsvLINQPadDriver));
            yield return defines.Where(ShouldRender).Select(static define => $"#define {define}").JoinNewLine();
            yield return @"string Reason([CallerLineNumber] int sourceLineNumber = 0) => $""something went wrong at line #{sourceLineNumber}"";";
            if (ShouldRender(context))
            {
                yield return $"var context = {context};";
            }
        }

        static bool ShouldRender(string? str) =>
            !string.IsNullOrWhiteSpace(str);
    }

    private static IEnumerable<ScriptWithDriverPropertiesTestData> ParallelizableScriptWithDriverPropertiesTestDataTestsData() =>
        ScriptWithDriverPropertiesTestDataTestsData().AugmentWithFileIndex(
            static testData => testData.LinqScriptName,
            static (testData, index) => testData with { Index = index });

    private static IEnumerable<ScriptWithDriverPropertiesTestData> ScriptWithDriverPropertiesTestDataTestsData()
    {
        const StringComparison defaultStringComparison = StringComparison.InvariantCulture;

        var defaultCsvDataContextDriverProperties = GetDefaultCsvDataContextDriverPropertiesObject(defaultStringComparison);

        return GetTestData().SelectMany(static t => t);

        IEnumerable<IEnumerable<ScriptWithDriverPropertiesTestData>> GetTestData()
        {
            const string? noContext = null;

            // Multiple driver properties.
            yield return GetCsvDataContextDriverProperties()
                .SelectMany(static driverProperties => new[] { "Generation", "Relations"}
                    .Select(linqScriptName => new ScriptWithDriverPropertiesTestData(
                        linqScriptName,
                        $"new {{ {nameof(driverProperties.UseSingleClassForSameFiles)} = {driverProperties.UseSingleClassForSameFiles.ToString().ToLowerInvariant()} }}",
                        driverProperties,
                        driverProperties.UseRecordType ? "USE_RECORD_TYPE" : null)));

            // Single driver properties.
            yield return new[] { "Extensions", "ExtensionsSafe", "SimilarFilesRelations" }
                .Select(linqFile => new ScriptWithDriverPropertiesTestData(
                    linqFile,
                    noContext,
                    defaultCsvDataContextDriverProperties
#if NET5_0_OR_GREATER
                    , "NET5_0_OR_GREATER"
#endif
#if NET6_0_OR_GREATER
                    , "NET6_0_OR_GREATER"
#endif
#if NET7_0_OR_GREATER
                    , "NET7_0_OR_GREATER"
#endif
#if NET8_0_OR_GREATER
                    , "NET8_0_OR_GREATER"
#endif
                ));

            // String comparison.
            yield return GetStringComparisons()
                .SelectMany(static stringComparison => new[] { "StringComparison" }
                    .Select(linqFile => new ScriptWithDriverPropertiesTestData(
                        linqFile,
                        GetStringComparisonContext(stringComparison),
                        GetDefaultCsvDataContextDriverPropertiesObject(stringComparison))));

            // Encoding detection.
            yield return GetFileEncodings()
                .SelectMany(static fileEncodings => new[] { "Encoding" }
                    .Select(linqFile => new ScriptWithDriverPropertiesTestData(
                        linqFile,
                        GetEncodingContext(fileEncodings),
                        GetDefaultCsvDataContextDriverPropertiesObject(defaultStringComparison))));

            // String comparison for interning.
            yield return GetStringComparisons()
                .Select(static stringComparison => new ScriptWithDriverPropertiesTestData(
                    "StringComparisonForInterning",
                    GetStringComparisonContext(stringComparison),
                    GetDefaultCsvDataContextDriverPropertiesObject(
                        stringComparison,
                        useStringComparerForStringIntern: true)));

            // Allow comments.
            yield return new[] { true, false }
                .Select(static allowComments => new ScriptWithDriverPropertiesTestData(
                    "Comments",
                    $"new {{ ExpectedCount = {(allowComments ? 1 : 2)} }}",
                    GetDefaultCsvDataContextDriverPropertiesObject(
                        defaultStringComparison,
                        allowComments)));

            // Skip leading rows.
            yield return new (string LinqScriptName, int Count)[] { ("SkipLeadingRows", 3), ("SkipLeadingRowsAll", 100) }
                .Select(static skip => new ScriptWithDriverPropertiesTestData(
                    skip.LinqScriptName,
                    noContext,
                    GetDefaultCsvDataContextDriverPropertiesObject(
                        defaultStringComparison,
                        skipLeadingRowsCount: skip.Count)));

            // Rename table.
            yield return new[] { true, false }
                .Select(static useSingleClassForSameFiles => new ScriptWithDriverPropertiesTestData(
                    "RenameTable",
                    noContext,
                    GetDefaultCsvDataContextDriverPropertiesObject(
                        defaultStringComparison,
                        useSingleClassForSameFiles: useSingleClassForSameFiles,
                        renameTable: true)));
        }

        IEnumerable<ICsvDataContextDriverProperties> GetCsvDataContextDriverProperties()
        {
            yield return defaultCsvDataContextDriverProperties;

            yield return GetCsvDataContextDriverPropertiesWithUseRecordType(false);
            yield return GetCsvDataContextDriverPropertiesWithUseRecordType(true);

            static ICsvDataContextDriverProperties GetCsvDataContextDriverPropertiesWithUseRecordType(bool useRecordType) =>
                Mock.Of<ICsvDataContextDriverProperties>(csvDataContextDriverProperties =>
                        csvDataContextDriverProperties.Files == Files &&
                        csvDataContextDriverProperties.AutoDetectEncoding &&
                        csvDataContextDriverProperties.DebugInfo &&
                        csvDataContextDriverProperties.DetectRelations &&
                        csvDataContextDriverProperties.UseRecordType == useRecordType &&
                        csvDataContextDriverProperties.StringComparison == defaultStringComparison &&
#pragma warning disable S1125
                        csvDataContextDriverProperties.UseSingleClassForSameFiles == false &&
                        csvDataContextDriverProperties.IsStringInternEnabled == false &&
                        csvDataContextDriverProperties.IgnoreInvalidFiles == false &&
                        csvDataContextDriverProperties.IsCacheEnabled == false &&
                        csvDataContextDriverProperties.HideRelationsFromDump == false &&
                        csvDataContextDriverProperties.Persist == false
#pragma warning restore S1125
                );
        }

        static ICsvDataContextDriverProperties GetDefaultCsvDataContextDriverPropertiesObject(
            StringComparison stringComparison,
            bool allowComments = false,
            bool useStringComparerForStringIntern = false,
            int skipLeadingRowsCount = 0,
            bool renameTable = false,
            bool useSingleClassForSameFiles = true,
            TableNameFormat tableNameFormat = TableNameFormat.table_1) =>
            Mock.Of<ICsvDataContextDriverProperties>(csvDataContextDriverProperties =>
                csvDataContextDriverProperties.Files == Files &&
                csvDataContextDriverProperties.AutoDetectEncoding &&
                csvDataContextDriverProperties.DebugInfo &&
                csvDataContextDriverProperties.DetectRelations &&
                csvDataContextDriverProperties.UseRecordType &&
                csvDataContextDriverProperties.UseSingleClassForSameFiles == useSingleClassForSameFiles &&
                csvDataContextDriverProperties.AllowComments == allowComments &&
                csvDataContextDriverProperties.StringComparison == stringComparison &&
                csvDataContextDriverProperties.IsStringInternEnabled &&
                csvDataContextDriverProperties.UseStringComparerForStringIntern == useStringComparerForStringIntern &&
                csvDataContextDriverProperties.IgnoreInvalidFiles &&
                csvDataContextDriverProperties.IsCacheEnabled &&
                csvDataContextDriverProperties.HideRelationsFromDump &&
                csvDataContextDriverProperties.Persist &&
                csvDataContextDriverProperties.AllowSkipLeadingRows &&
                csvDataContextDriverProperties.SkipLeadingRowsCount == skipLeadingRowsCount &&
                csvDataContextDriverProperties.RenameTable == renameTable &&
                csvDataContextDriverProperties.TableNameFormat == tableNameFormat
            );

        static IEnumerable<StringComparison> GetStringComparisons() =>
            Enum.GetValues(typeof(StringComparison)).Cast<StringComparison>();

        static string GetStringComparisonContext(StringComparison stringComparison) =>
            $"new {{ StringComparison = StringComparison.{stringComparison} }}";

        static string GetEncodingContext(IEnumerable<string> objects)
        {
            return $@"new[]
{{
{string.Join($",{Environment.NewLine}", objects.Select(static obj => $"\t{obj}"))}
}}";
        }

        static IEnumerable<IEnumerable<string>> GetFileEncodings()
        {
            yield return GetBomFiles();
            yield return GetEncodingDetectionFiles();

            static IEnumerable<string> GetBomFiles()
            {
                yield return "Utf8Cp65001_Encoding";
                yield return "Utf16BomCp1200_Encoding";
                yield return "Utf16BomCp1201_Encoding";
                yield return "Utf8BomCp65001_Encoding";
                yield return "Utf32Bom_Encoding";
            }

            static IEnumerable<string> GetEncodingDetectionFiles()
            {
                yield return "Cp1252_Encoding_German";
                yield return "Utf16BomCp1200_Encoding_German";
                yield return "Utf16BomCp1201_Encoding_German";
                yield return "Utf8BomCp65001_Encoding_German";
                yield return "Utf32Bom_Encoding_German";
            }
        }
    }
}
