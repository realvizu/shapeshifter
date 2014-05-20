using System;
using System.Reflection;

namespace Shapeshifter.Core
{
    /// <summary>
    ///     Helper data holder for storing serializable member information.
    /// </summary>
    /// <remarks>
    ///     It can be used for both fields and properties, internally it will use the correct Info
    /// </remarks>
    internal class SerializableTypeMemberInfo
    {
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;

        public SerializableTypeMemberInfo(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public SerializableTypeMemberInfo(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public string Name
        {
            get
            {
                if (_fieldInfo != null) return _fieldInfo.Name;
                return _propertyInfo.Name;
            }
        }

        public Type Type
        {
            get
            {
                if (_fieldInfo != null) return _fieldInfo.FieldType;
                return _propertyInfo.PropertyType;
            }
        }

        public object GetValueFor(object instance)
        {
            if (_fieldInfo != null)
            {
                return _fieldInfo.GetValue(instance);
            }
            return _propertyInfo.GetValue(instance, null);
        }

        public void SetValueFor(object instance, object value)
        {
            if (_fieldInfo != null)
            {
                _fieldInfo.SetValue(instance, value);
            }
            else
            {
                _propertyInfo.SetValue(instance, value, null);
            }
        }
    }
}