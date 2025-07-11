# CsvLINQPadDriver for LINQPad 8/7/6/5

[![Latest build](https://github.com/i2van/CsvLINQPadDriver/workflows/build/badge.svg)](https://github.com/i2van/CsvLINQPadDriver/actions)
[![NuGet](https://img.shields.io/nuget/v/CsvLINQPadDriver)](https://www.nuget.org/packages/CsvLINQPadDriver)
[![Downloads](https://img.shields.io/nuget/dt/CsvLINQPadDriver)](https://www.nuget.org/packages/CsvLINQPadDriver)
[![License](https://img.shields.io/badge/license-MIT-yellow)](https://opensource.org/licenses/MIT)

## Table of Contents

* [Description](#description)
* [Website](#website)
* [Download](#download)
* [Example](#example)
* [Prerequisites](#prerequisites)
* [Installation](#installation)
  * [LINQPad 8/7/6](#linqpad-876)
    * [NuGet](#nuget)
    * [Manual](#manual)
  * [LINQPad 5](#linqpad-5)
* [Usage](#usage)
* [Configuration Options](#configuration-options)
  * [CSV Files](#csv-files)
  * [Format](#format)
  * [Memory](#memory)
  * [Generation](#generation)
  * [Relations](#relations)
  * [Misc](#misc)
* [Relations Detection](#relations-detection)
* [Performance](#performance)
* [Data Types](#data-types)
* [Generated Data Object](#generated-data-object)
  * [Methods](#methods)
    * [ToString](#tostring)
    * [GetHashCode](#gethashcode)
    * [Equals](#equals)
    * [Overloaded Operators](#overloaded-operators)
    * [Indexers](#indexers)
  * [Properties Access](#properties-access)
  * [Extension Methods](#extension-methods)
* [Known Issues](#known-issues)
* [Troubleshooting](#troubleshooting)
* [Authors](#authors)
* [Credits](#credits)
  * [Tools](#tools)
  * [Libraries](#libraries)
* [License](#license)

## Description

**CsvLINQPadDriver** is LINQPad 8/7/6/5 data context dynamic driver for querying [CSV](https://en.wikipedia.org/wiki/Comma-separated_values) files.

* You can query data in CSV files with LINQ, just like it would be regular database. No need to write custom data model, mappings, etc.
* Driver automatically generates new data types for every CSV file with corresponding properties and mappings for all the columns. Every column is a `string`, no data type detection is provided.
* Based on column and file names, possible relations between CSV tables are detected and generated.
* Single class generation allows to join similar files and query over them. Might not work well for files with relations.

## Website

* [This project](https://github.com/i2van/CsvLINQPadDriver)
* [Original project](https://github.com/dobrou/CsvLINQPadDriver)

## Download

Latest [CsvLINQPadDriver.\*.lpx6/CsvLINQPadDriver.\*.lpx](https://github.com/i2van/CsvLINQPadDriver/releases) for LINQPad 8/7/6/5 manual installation.

## Example

Let's have 2 CSV files:

`Authors.csv`

```text
Id,Name
1,Author 1
2,Author 2
3,Author 3
```

`Books.csv`

```text
Id,Title,AuthorId
11,Author 1 Book 1,1
12,Author 1 Book 2,1
21,Author 2 Book 1,2
```

**CsvLINQPadDriver** will generate data context similar to (simplified) if [relations](#relations) detection is enabled:

```csharp
public class CsvDataContext
{
    public CsvTableBase<RAuthor> Authors { get; private set; }
    public CsvTableBase<RBook> Books { get; private set; }
}

// record/class for LINQPad 8/7/6, class for LINQPad 5.
public sealed record RAuthor
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    public IEnumerable<RBook>? Books { get; set; }
}

public sealed record RBook
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? AuthorId { get; set; }

    public IEnumerable<RAuthor>? Authors { get; set; }
}
```

And you can query data with LINQ like:

```csharp
from book in Books
join author in Authors on book.AuthorId equals author.Id
select new { author.Name, book.Title }
```

## Prerequisites

* [LINQPad 8](https://www.linqpad.net/LINQPad8.aspx): [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)/[.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)
* [LINQPad 7](https://www.linqpad.net/LINQPad7.aspx): [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)/[.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [LINQPad 6](https://www.linqpad.net/LINQPad6.aspx): [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [LINQPad 5](https://www.linqpad.net/LINQPad5.aspx): [.NET Framework 4.7.1](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net471)

## Installation

### LINQPad 8/7/6

#### NuGet

[![NuGet](https://img.shields.io/nuget/v/CsvLINQPadDriver)](https://www.nuget.org/packages/CsvLINQPadDriver)

* Open LINQPad 8/7/6.
* Click `Add connection` link.
* Click button `View more drivers...`
* Click radio button `Show all drivers` and type `CsvLINQPadDriver`
* Install.

#### Manual

Get latest [CsvLINQPadDriver.\*.lpx6](https://github.com/i2van/CsvLINQPadDriver/releases) file.

* Open LINQPad 8/7/6.
* Click `Add connection` link.
* Click button `View more drivers...`
* Click button `Install driver from .LPX6 file...` and select downloaded `lpx6` file.

### LINQPad 5

Get latest [CsvLINQPadDriver.\*.lpx](https://github.com/i2van/CsvLINQPadDriver/releases) file.

* Open LINQPad 5.
* Click `Add connection` link.
* Click button `View more drivers...`
* Click button `Browse...` and select downloaded `lpx` file.

## Usage

CSV files connection can be added to LINQPad 8/7/6/5 the same way as any other connection.

* Click `Add connection`
* Select `CSV Context Driver` and click `Next`
* Enter CSV file names or Drag&Drop (`Ctrl` adds files) from Explorer. Optionally configure other options.
* Query your data.

## Configuration Options

### CSV Files

* CSV files: list of CSV files and folders. Can be added via files/folder dialogs, context menu, hotkeys, by typing one file/folder per line or by Drag&drop (`Ctrl` adds files, `Alt` toggles `*` and `**` masks). Wildcards `?` and `*` are supported; `**.csv` searches in folder and its sub-folders.
  * `c:\Books\Books?.csv`: `Books.csv`, `Books1.csv`, etc. files in folder `c:\Books`
  * `c:\Books\*.csv`: all `*.csv` files in folder `c:\Books`
  * `c:\Books\**.csv`: all `*.csv` files in folder `c:\Books` and its sub-folders.
* Order files by: files sort order. Affects similar files order.
* Fallback encoding: [encoding](https://docs.microsoft.com/en-us/windows/win32/intl/code-page-identifiers) to use if file encoding could not be detected. `UTF-8` is used by default.
* Auto-detect file encodings: try to detect file encodings.
* Validate file paths: checks if file paths are valid.
* Ignore files with invalid format: files with content which does not resemble CSV will be ignored.
* Do not lock files being read: allow other processes to modify files being read.

> [!NOTE]
> Driver does not track files changes.

### Format

* Separator: characters used to separate columns in files. Can be `,`, `\t`, etc. Auto-detected if empty.
* Use [CsvHelper](https://joshclose.github.io/CsvHelper) library separator auto-detection: use CsvHelper library separator auto-detection instead of internal one.
* Ignore bad data: ignore malformed files.
* Ignore blank lines: do not process blank lines.
* Parsing mode:
  * Use [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180) format: if a field contains a `Separator` or `NewLine` character, it is wrapped in `Quote` characters. If quoted field contains a `Quote` character, it is preceded by `Escape` character.
  * Use escapes: if a field contains a `Separator`, `NewLine` or `Escape` character, it should be preceded by `Escape` character.
  * Do not use quotes or escapes: ignore quoting and escape characters. This means a field cannot contain a `Separator`, `Quote` or `NewLine` characters as they cannot be escaped.
* Escape: the character used to escape characters. `"` if empty.
* Quote: the character used to quote fields. `"` if empty.
* Skip leading rows: allow to skip the specified number of leading rows.
* Trim spaces: allow to trim spaces around fields and/or inside quotes around fields.
* Allow comments: allow single-line comments - lines starting with `#` (which is used by default) will be ignored.
* Header detection: detect or add header with specific column fallback name format if header could not be detected.
  * Header detection approach: specify whether header is present or not, or how to detect it by symbols it consists of.
  * Header column fallback name format: define generated columns names if there is no header.

### Memory

* Cache data in memory:
  * if checked: parsed rows are cached in memory. Cache survives multiple query runs, even when query is changed. Cache is cleared as soon as LINQPad clears query data. **May significantly increase** memory usage.
  * if unchecked: disable cache. Multiple enumerations of file content results in multiple reads and parsing of file. Can be significantly slower for complex queries. **Significantly reduces** memory usage. Useful when reading very large files.
* Intern strings: intern strings. **May significantly reduce** memory consumption when CSV contains repeatable values; **may significantly increase** memory usage otherwise.
  * Use [generation](#generation) string comparison: compare interned strings using generation string comparison. `Ordinal` is used by default.

### Generation

* Use [record](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records) type: generate records instead of classes (LINQPad 8/7/6 only).
* Generate single type for similar files: single type will be generated for similar files which allows to join similar files and query over them. Relations support is limited.
  * Also show similar files non-grouped: show similar files non-grouped in addition to similar files groups.
* Rename table:
  * if checked: the table name will be renamed according to the selected table name format.
  * if unchecked: the table name is the file name.
* String comparison: string comparison for `Equals` and `GetHashCode` methods.

### Relations

* Detect relations: driver will try to detect and generate relations between files.
  * Hide relations from `Dump()`: LINQPad will not show relations content when `Dump()`ed. This prevents loading too many data.

### Misc

* Debug info: show additional driver debug info, e.g. generated data context source, and enable logging.
* Remember this connection: connection will be available on next run.
* Contains production data: files contain production data.

## Relations Detection

There is no definition of relations between CSV files, but we can guess some relations from files and columns names.
Relations between `fileName.columnName` are detected in cases similar to following examples:

```text
Books.AuthorId  <-> Authors.Id
Books.AuthorsId <-> Authors.Id
Books.AuthorId  <-> Authors.AuthorId
Books.Id        <-> Authors.BookId
```

## Performance

When executing LINQ query for CSV connection:

* Only files used in query are loaded from disk.
* As soon as any record from file is accessed, whole file is loaded into memory.
* Relations are lazily evaluated and retrieved using cached lookup tables.

Don't expect performance comparable with SQL server. But for reasonably sized CSV files there should not be any problem.

## Data Types

Everything is `string`. Because there is no data type info in CSV files, this is the best we can do - see conversion [extension methods](#extension-methods).

## Generated Data Object

Generated data object is sealed mutable class or [record](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records) (LINQPad 8/7/6 only). You can create record's shallow copy using [with](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/with-expression) expression.

### Methods

```csharp
string? ToString();

bool Equals(T? obj);
bool Equals(object? obj);

static bool operator == (T? obj1, T? obj2);
static bool operator != (T? obj1, T? obj2);

int GetHashCode();

string? this[int index] { get; set; }
string? this[string index] { get; set; }
```

#### ToString

```csharp
string? ToString();
```

Formats object the way PowerShell [Format-List](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/format-list) does.

> [!NOTE]
> Relations are not participated.

#### GetHashCode

```csharp
int GetHashCode();
```

Returns object hash code.

> [!IMPORTANT]
> * Generated data object is mutable.
> * Hash code is not cached and recalculated each time method is called.
> * Each time driver is reloaded string hash codes will be [different](https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/).

> [!NOTE]
> Depends on string comparison driver setting. Relations are not participated.

#### Equals

```csharp
bool Equals(T? obj);
bool Equals(object? obj);
```

> [!NOTE]
> Depends on string comparison driver setting. Relations are not participated.

#### Overloaded Operators

```csharp
static bool operator == (T? obj1, T? obj2);
static bool operator != (T? obj1, T? obj2);
```

> [!NOTE]
> Depends on string comparison driver setting. Relations are not participated.

#### Indexers

```csharp
string? this[int index] { get; set; }
string? this[string index] { get; set; }
```

See [properties access](#properties-access) below.

> [!NOTE]
> Relations are not participated.

### Properties Access

* Generated data objects are mutable, however saving changes is not supported.
* Generated data object properties can be accessed either by case-sensitive name or via indexer.
* Index can be integer (zero-based property index) or string (case-sensitive property name). If there is no index `IndexOutOfRangeException` will be thrown.
* Relations cannot be accessed via indexers.

```csharp
var author = Authors.First();

// Property (preferable).
var name = author.Name;
author.Name = name;

// Integer indexer.
var name = author[0];
author[0] = name;

// String indexer.
var name = author["Name"];
author["Name"] = name;
```

Property index can be found by hovering over property name at the connection pane or by using code below:

```csharp
Authors.First()
    .GetType().GetProperties()
    .Where(p => !p.GetCustomAttributes().Any())
    .Select((p, i) => new { Index = i, p.Name })
```

### Extension Methods

* Driver provides extension methods for converting `string` (and `ReadOnlySpan<char>` for .NET Core/.NET) to `T?`
* `CultureInfo.InvariantCulture` is used for `provider` by default.
* `null` is returned for `null` or empty input.
* `CsvLINQPadDriver.ConvertException` is thrown for non-`Safe` methods.
* **These methods are much more (~3-5 times) slower than .NET methods due to road-trip to driver.**

```csharp
public static class Styles
{
    public const NumberStyles Integer          =
        NumberStyles.Integer |
        NumberStyles.AllowThousands;

    public const NumberStyles Float            =
        NumberStyles.Float   |
        NumberStyles.AllowThousands;

    public const NumberStyles Decimal          =
        NumberStyles.Number;

    public const DateTimeStyles DateTimeOffset =
        DateTimeStyles.None;

    public const DateTimeStyles DateTime       =
        DateTimeStyles.None;

    public const DateTimeStyles UtcDateTime    =
        DateTimeStyles.AdjustToUniversal |
        DateTimeStyles.AssumeUniversal;

    public const TimeSpanStyles TimeSpan       =
        TimeSpanStyles.None;

    // .NET 6+
    public const DateTimeStyles DateOnly       =
        DateTimeStyles.None;

    // .NET 6+
    public const DateTimeStyles TimeOnly       =
        DateTimeStyles.None;
}

// Bool
bool? ToBool(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

bool? ToBoolSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// SByte
sbyte? ToSByte(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

sbyte? ToSByteSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Byte
byte? ToByte(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

byte? ToByteSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Short
short? ToShort(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

short? ToShortSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// UShort
ushort? ToUShort(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

ushort? ToUShortSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Int
int? ToInt(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

int? ToIntSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// UInt
uint? ToUInt(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

uint? ToUIntSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Long
long? ToLong(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

long? ToLongSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// ULong
ulong? ToULong(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

ulong? ToULongSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// NInt: .NET 5+
nint? ToNInt(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

nint? ToNIntSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// NUInt: .NET 5+
nuint? ToNUInt(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

nuint? ToNUIntSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// BigInteger
BigInteger? ToBigInteger(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

BigInteger? ToBigIntegerSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Float
float? ToFloat(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

float? ToFloatSafe(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

// Double
double? ToDouble(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

double? ToDoubleSafe(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

// Decimal
decimal? ToDecimal(
    NumberStyles style = Styles.Decimal,
    IFormatProvider? provider = null);

decimal? ToDecimalSafe(
    NumberStyles style = Styles.Decimal,
    IFormatProvider? provider = null);

// Half: .NET 5+
Half? ToHalf(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

Half? ToHalfSafe(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

// Int128: .NET 7+
static Int128? ToInt128(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

static Int128? ToInt128Safe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// UInt128: .NET 7+
static UInt128? ToUInt128(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

static UInt128? ToUInt128Safe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// Complex
Complex? ToComplex(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

Complex? ToComplexSafe(
    NumberStyles style = Styles.Float,
    IFormatProvider? provider = null);

// Guid
Guid? ToGuid();
Guid? ToGuidSafe();

Guid? ToGuid(string format);
Guid? ToGuidSafe(string format);

Guid? ToGuid(ReadOnlySpan<char> format);
Guid? ToGuidSafe(ReadOnlySpan<char> format);

Guid? ToGuid(string[] formats);
Guid? ToGuidSafe(string[] formats);

// DateTime
DateTime? ToDateTime(
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTimeSafe(
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTime(
    string format,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTimeSafe(
    string format,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

// .NET Core/.NET
DateTime? ToDateTime(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTimeSafe(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTime(
    string[] formats,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToDateTimeSafe(
    string[] formats,
    DateTimeStyles style = Styles.DateTime,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTime(
    DateTimeStyles style = Styles.UtcDateTime,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTimeSafe(
    DateTimeStyles style = Styles.UtcDateTime,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTimeFromUnixTimeSeconds(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTimeFromUnixTimeSecondsSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTimeFromUnixTimeMilliseconds(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTime? ToUtcDateTimeFromUnixTimeMillisecondsSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// DateTimeOffset
DateTimeOffset? ToDateTimeOffset(
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetSafe(
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffset(
    string format,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetSafe(
    string format,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

// .NET Core/.NET
DateTimeOffset? ToDateTimeOffset(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetSafe(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffset(
    string[] formats,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetSafe(
    string[] formats,
    DateTimeStyles style = Styles.DateTimeOffset,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetFromUnixTimeSeconds(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetFromUnixTimeSecondsSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetFromUnixTimeMilliseconds(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

DateTimeOffset? ToDateTimeOffsetFromUnixTimeMillisecondsSafe(
    NumberStyles style = Styles.Integer,
    IFormatProvider? provider = null);

// TimeSpan
TimeSpan? ToTimeSpan(
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpanSafe(
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpan(
    string format,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpanSafe(
    string format,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

// .NET Core/.NET
TimeSpan? ToTimeSpan(
    ReadOnlySpan<char> format,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpanSafe(
    ReadOnlySpan<char> format,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpan(
    string[] formats,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

TimeSpan? ToTimeSpanSafe(
    string[] formats,
    TimeSpanStyles style = Styles.TimeSpan,
    IFormatProvider? provider = null);

// DateOnly: .NET 6+
DateOnly? ToDateOnly(
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnlySafe(
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnly(
    string format,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnlySafe(
    string format,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

// .NET Core/.NET
DateOnly? ToDateOnly(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnlySafe(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnly(
    string[] formats,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

DateOnly? ToDateOnlySafe(
    string[] formats,
    DateTimeStyles style = Styles.DateOnly,
    IFormatProvider? provider = null);

// TimeOnly: .NET 6+
TimeOnly? ToTimeOnly(
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnlySafe(
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnly(
    string format,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnlySafe(
    string format,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

// .NET Core/.NET
TimeOnly? ToTimeOnly(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnlySafe(
    ReadOnlySpan<char> format,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnly(
    string[] formats,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);

TimeOnly? ToTimeOnlySafe(
    string[] formats,
    DateTimeStyles style = Styles.TimeOnly,
    IFormatProvider? provider = null);
```

## Known Issues

* Default encoding for files without BOM is UTF-8.
* Some strange Unicode characters in column names may cause errors in generated data context source code.
* Writing changed objects back to CSV is not directly supported, there is no `SubmitChanges()`. But you can use LINQPad's `Util.WriteCsv`
* Relations detection does not work well for similar files single class generation. However, you can query over related multiple files.
* Relations detection with file sorting might produce broken source code for similar files single class generation.

## Troubleshooting

* In case of `Cannot load type 'LINQPad.User.***' from cache` error, use connection context menu **Close all connections**.
* In case of `BadDataException: You can ignore bad data by setting BadDataFound to null` error, check `Ignore bad data` at [Format](#format) section.
* **CsvLINQPadDriver** writes `CsvLINQPadDriver.txt` log file to the `%LOCALAPPDATA%\LINQPad\Logs` for the [LINQPad 5](https://www.linqpad.net/LINQPad5.aspx) or to the corresponding `%LOCALAPPDATA%\LINQPad\Logs.*` [LINQPad 8](https://www.linqpad.net/LINQPad8.aspx)/[LINQPad 7](https://www.linqpad.net/LINQPad7.aspx)/[LINQPad 6](https://www.linqpad.net/LINQPad6.aspx) folders. This file is never truncated.

## Authors

* [Martin Dobroucký](https://github.com/dobrou)
* [Ivan Ivon](https://github.com/i2van)

## Credits

### Tools

* [LINQPad 8](https://www.linqpad.net/LINQPad8.aspx)/[LINQPad 7](https://www.linqpad.net/LINQPad7.aspx)/[LINQPad 6](https://www.linqpad.net/LINQPad6.aspx)/[LINQPad 5](https://www.linqpad.net/LINQPad5.aspx)
* [LINQPad Command-Line and Scripting (LPRun)](https://www.linqpad.net/lprun.aspx)

### Libraries

* [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions)
* [CsvHelper](https://github.com/JoshClose/CsvHelper)
* [Humanizer](https://github.com/Humanizr/Humanizer)
* [IsExternalInit](https://github.com/manuelroemer/IsExternalInit)
* [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode) (for LINQPad 5 only)
* [Moq](https://github.com/moq/moq4)
* [Nullable](https://github.com/manuelroemer/Nullable) (for LINQPad 5 only)
* [NUnit](https://github.com/nunit/nunit)
* [UnicodeCharsetDetector](https://github.com/i2van/UnicodeCharsetDetector)
* [UTF.Unknown](https://www.nuget.org/packages/UTF.Unknown)
* [Windows API Code Pack](https://github.com/samypr100/Windows-API-Code-Pack-1.1)
* [Windows API Code Pack Shell](https://github.com/samypr100/Windows-API-Code-Pack-1.1)

## License

* [LICENSE](https://github.com/i2van/CsvLINQPadDriver/blob/master/LICENSE) ([MIT](https://opensource.org/licenses/MIT))
