﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CsvLINQPadDriver.CodeGen;

using LINQPad;

using static System.Linq.Expressions.Expression;

namespace CsvLINQPadDriver.DataDisplay
{
    internal class CsvRowMemberProvider : ICustomMemberProvider
    {
        private static readonly Dictionary<Type, ProviderData> ProvidersDataCache = new Dictionary<Type, ProviderData>();

        private readonly object _objectToDisplay;
        private readonly ProviderData _providerData;

        protected CsvRowMemberProvider(object objectToDisplay, ProviderData providerData)
        {
            _objectToDisplay = objectToDisplay;
            _providerData = providerData;
        }

        public IEnumerable<string> GetNames() =>
            _providerData.Properties
                .Select(propertyInfo => propertyInfo.Name)
                .Concat(_providerData.Fields.Select(fieldInfo => fieldInfo.Name));

        public IEnumerable<Type> GetTypes() =>
            _providerData.Properties
                .Select(propertyInfo => propertyInfo.PropertyType)
                .Concat(_providerData.Fields.Select(fieldInfo => fieldInfo.FieldType));

        public IEnumerable<object> GetValues() =>
            _providerData.ValuesGetter(_objectToDisplay);

        protected class ProviderData
        {
            public IList<PropertyInfo> Properties;
            public IList<FieldInfo> Fields;
            public Func<object, object[]> ValuesGetter;
        }

        protected static ProviderData GetProviderData(Type objectType)
        {
            var param = Parameter(typeof(object));
            var properties = objectType.GetProperties().Where(IsMemberVisible).ToList();
            var fields = objectType.GetFields().Where(IsMemberVisible).ToList();

            return new ProviderData
            {
                Properties = properties,
                Fields = fields,
                ValuesGetter = Lambda<Func<object, object[]>>(
                    NewArrayInit(typeof(object),
                        properties
                            .Select(propertyInfo => Property(TypeAs(param, objectType), propertyInfo))
                            .Concat(fields.Select(fieldInfo => Field(TypeAs(param, objectType), fieldInfo)))),
                    param)
                    .Compile()
            };

            static bool IsMemberVisible(MemberInfo member) =>
                 (MemberTypes.Field | MemberTypes.Property).HasFlag(member.MemberType) &&
                 !member.GetCustomAttributes(typeof(HideFromDumpAttribute), true).Any();
        }
        
        public static ICustomMemberProvider GetCsvRowMemberProvider(object objectToDisplay)
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
                typeof(CsvRowBase).IsAssignableFrom(objectType);
        }
    }
}
