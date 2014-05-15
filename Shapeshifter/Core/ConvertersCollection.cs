﻿using System;
using System.Collections.Generic;
using System.Linq;
using Shapeshifter.Core.Converters;

namespace Shapeshifter.Core
{
    /// <summary>
    ///     Collection of <see cref="IValueConverter" /> implementations
    /// </summary>
    internal class ConvertersCollection
    {
        private readonly List<IValueConverter> _converters = new List<IValueConverter>();

        public ConvertersCollection(IEnumerable<IPackformatSurrogateConverter> customConverters)
        {
            //built-in converters 
            _converters.Add(new GuidToStringConverter());
            _converters.Add(new EnumToStringConverter());
            _converters.Add(new KeyValuePairConverter());
            //and custom ones
            _converters.AddRange(customConverters.Select(customConverter => new SurrogateConverterWrapper(customConverter)));
        }

        public IValueConverter ResolveConverter(Type type)
        {
            return _converters.FirstOrDefault(valueConverter => valueConverter.CanConvert(type));
        }
    }
}