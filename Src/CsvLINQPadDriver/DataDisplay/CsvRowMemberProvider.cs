﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CsvLINQPadDriver.CodeGen;

using LINQPad;

using static System.Linq.Expressions.Expression;

#if NETCOREAPP
using System.Collections.Immutable;
#else
using CsvLINQPadDriver.Bcl.Extensions;
#endif

namespace CsvLINQPadDriver.DataDisplay;

internal sealed class CsvRowMemberProvider : ICustomMemberProvider
{
    private static readonly Dictionary<Type, ProviderData> ProvidersDataCache = new();

    private readonly object _objectToDisplay;
    private readonly ProviderData _providerData;

    private CsvRowMemberProvider(object objectToDisplay, ProviderData providerData)
    {
        _objectToDisplay = objectToDisplay;
        _providerData = providerData;
    }

    public IEnumerable<string> GetNames() =>
        _providerData.Properties
            .Select(static propertyInfo => propertyInfo.Name)
            .Concat(_providerData.Fields.Select(static fieldInfo => fieldInfo.Name));

    public IEnumerable<Type> GetTypes() =>
        _providerData.Properties
            .Select(static propertyInfo => propertyInfo.PropertyType)
            .Concat(_providerData.Fields.Select(static fieldInfo => fieldInfo.FieldType));

    public IEnumerable<object> GetValues() =>
        _providerData.ValuesGetter(_objectToDisplay);

    private sealed record ProviderData(IList<PropertyInfo> Properties, IList<FieldInfo> Fields, Func<object, object[]> ValuesGetter);

    private static ProviderData GetProviderData(Type objectType)
    {
        var param = Parameter(typeof(object));
        var properties = objectType.GetProperties().Where(IsMemberVisible).ToImmutableList();
        var fields = objectType.GetFields().Where(IsMemberVisible).ToImmutableList();

        return new ProviderData(
            properties,
            fields,
            Lambda<Func<object, object[]>>(
                    NewArrayInit(typeof(object),
                        properties
                            .Where(static propertyInfo => propertyInfo.GetIndexParameters().Length == 0)
                            .Select(propertyInfo => Property(TypeAs(param, objectType), propertyInfo))
                            .Concat(fields.Select(fieldInfo => Field(TypeAs(param, objectType), fieldInfo)))),
                    param)
                .Compile()
        );

        static bool IsMemberVisible(MemberInfo member) =>
            (member.MemberType & (MemberTypes.Field | MemberTypes.Property)) != 0 &&
            !member.GetCustomAttributes(typeof(HideFromDumpAttribute), true).Any();
    }

    public static ICustomMemberProvider? GetCsvRowMemberProvider(object objectToDisplay)
    {
        var objectType = objectToDisplay.GetType();
        if (!IsSupportedType(objectType))
        {
            return null;
        }

        if (!ProvidersDataCache.TryGetValue(objectType, out var providerData))
        {
            providerData = GetProviderData(objectType);
            ProvidersDataCache.Add(objectType, providerData);
        }

        return new CsvRowMemberProvider(objectToDisplay, providerData);

        static bool IsSupportedType(Type objectType) =>
            typeof(ICsvRowBase).IsAssignableFrom(objectType);
    }
}
