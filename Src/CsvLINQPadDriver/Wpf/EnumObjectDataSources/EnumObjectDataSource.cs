using System;
using System.ComponentModel;
using System.Linq;

namespace CsvLINQPadDriver.Wpf.EnumObjectDataSources;

internal abstract class EnumObjectDataSource<T> where T : struct, Enum
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public Tuple<T, string>[] GetValues() =>
#if NET5_0_OR_GREATER
        Enum.GetValues<T>()
#else
        Enum.GetValues(typeof(T)).Cast<T>()
#endif
        .Select(value =>
        {
            var valueAsString = value.ToString();
            var fieldInfo = typeof(T).GetField(valueAsString);
            var descriptionAttributes = (DescriptionAttribute[])fieldInfo!.GetCustomAttributes(typeof(DescriptionAttribute), true);
            return Tuple.Create(value, descriptionAttributes.FirstOrDefault()?.Description ?? valueAsString);
        }).ToArray();
}
