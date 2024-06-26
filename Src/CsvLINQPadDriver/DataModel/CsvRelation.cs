﻿namespace CsvLINQPadDriver.DataModel;

internal sealed record CsvRelation(
    CsvTable SourceTable,
    CsvTable TargetTable,
    CsvColumn SourceColumn,
    CsvColumn TargetColumn
) : ICsvNames
{
    public string? CodeName { get; set; }
    public string? DisplayName { get; set; }
}
