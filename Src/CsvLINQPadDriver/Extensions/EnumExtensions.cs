using System;
using System.ComponentModel;
using System.Globalization;

namespace CsvLINQPadDriver.Extensions;

internal static class EnumExtensions
{
    public static Func<int, string> GetFormatFunc<T>(this T value) where T: struct, Enum
    {
        var name =
#if NET5_0_OR_GREATER
            Enum.GetName(value)
#else
            Enum.GetName(typeof(T), value)
#endif
            ;

        if (name is null)
        {
            throw new InvalidEnumArgumentException($"Unknown {typeof(T).Name} {value}");
        }

        var format = $"{name[..^1]}{{0}}";
        var startIndex = name[^1] == '0' ? 0 : 1;

        return i => string.Format(CultureInfo.InvariantCulture, format, i + startIndex);
    }
}
