﻿using System.Collections.Generic;

namespace CsvLINQPadDriver.DataModel
{
    public record CsvTable
    (
        string FilePath,
        char CsvSeparator,

        IList<CsvColumn> Columns,
        IList<CsvRelation> Relations
    ) : ICsvNames
    {
        public string? CodeName { get; set; }
        public string? DisplayName { get; set; }
        
        public string? ClassName
        {
            get => _className ?? CodeName;
            init => _className = value;
        }

        private readonly string? _className;
    }
}
