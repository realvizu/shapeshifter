﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shapeshifter.Core.Deserialization;

namespace Shapeshifter.Core.Converters
{
    /// <summary>
    ///     An <see cref="IValueConverter" /> implementation for converting <see cref="KeyValuePair{TKey,TValue}" /> to JSON
    ///     format and back
    /// </summary>
    internal class KeyValuePairConverter
    {
        public object ConvertToPackformat(object value)
        {
            //TODO replace with generated code
            Type type = value.GetType();
            PropertyInfo keyPropInfo = type.GetProperty("Key");
            PropertyInfo valuePropInfo = type.GetProperty("Value");
            object keyVal = keyPropInfo.GetValue(value, null);
            object valueVal = valuePropInfo.GetValue(value, null);
            return new[] {keyVal, valueVal};
        }

        public object ConvertFromPackformat(ValueConverter valueConverter, Type targetType, object value)
        {
            var valArray = value as IList;
            if (valArray == null)
            {
                throw Exceptions.InvalidInputValueForConverter(value);
            }

            //TODO replace with generate code (speed opt)
            ConstructorInfo constructor = targetType.GetConstructors()[0];
            Type keyType = targetType.GetGenericArguments()[0];
            Type valueType = targetType.GetGenericArguments()[1];
            object result = constructor.Invoke(new[]
            {
                valueConverter.ConvertValueToTargetType(keyType, valArray[0]),
                valueConverter.ConvertValueToTargetType(valueType, valArray[1])
            });
            return result;
        }

        public bool CanConvert(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (KeyValuePair<,>);
        }
    }
}