﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Shapeshifter.Core;
using Shapeshifter.Core.Deserialization;
using Shapeshifter.Core.Detection;

// ReSharper disable IntroduceOptionalParameters.Global
//This is a library, we'd like to have clean public API.

namespace Shapeshifter.Builder
{
    /// <summary>
    ///     Class helping in object creation in custom deserializers.
    /// </summary>
    public class InstanceBuilder
    {
        private readonly TypeInspector _typeInspector;
        private readonly bool _enableInstanceManipulation;
        private object _instance;
        private bool _instanceRead = false;
        private readonly MemberSetRequests _memberSetsRequests;

        /// <summary>
        /// Creates a builder for the type given. If a reader is specified it tries to fill the new instance with the data from the reader.
        /// If instance manipulation is enabled one can set member values after acquiring the instance from the builder. 
        /// </summary>
        /// <param name="typeToBuild">Type to build.</param>
        /// <param name="reader">Reader with member data.</param>
        /// <param name="enableInstanceManipulation">Allow member value set after getting the instance.</param>
        /// <remarks>
        /// Instance manipulation be useful in scenarios where one of the values is not known at the custom deserializer, but can be set later on. 
        /// In such case the custom deserializer can return the instance and also save the builder. Later when the missing value is available (eg. on a 
        /// diffrent deserializer) the value can be set using the saved builder.
        /// Builder registers all data manipulation and defers the actual creation and data set until the last moment.
        /// </remarks>
        public InstanceBuilder(Type typeToBuild, IShapeshifterReader reader, bool enableInstanceManipulation)
        {
            _typeInspector = new TypeInspector(typeToBuild);
            _instance = FormatterServices.GetUninitializedObject(typeToBuild);
            _enableInstanceManipulation = enableInstanceManipulation;
            _memberSetsRequests = new MemberSetRequests(typeToBuild);

            if (reader != null)
            {
                SetMembersByReflection(reader);
            }
        }

        /// <summary>
        /// Creates a builder for the type and the reader given. The builder tries to fill the new instance with the data from the reader.
        /// </summary>
        /// <param name="typeToBuild">Type to build.</param>
        /// <param name="reader">Reader with member data.</param>

        public InstanceBuilder(Type typeToBuild, IShapeshifterReader reader):this(typeToBuild, reader, false)
        {}

        /// <summary>
        /// Creates a builder for the type given.
        /// </summary>
        /// <param name="typeToBuild">Type to build.</param>
        public InstanceBuilder(Type typeToBuild) : this(typeToBuild, null)
        {}

        /// <summary>
        /// Creates a builder for the type given.
        /// </summary>
        /// <param name="typeToBuild">Type to build.</param>
        /// <param name="enableInstanceManipulation">Allow member value set after getting the instance.</param>
        public InstanceBuilder(Type typeToBuild, bool enableInstanceManipulation)
            : this(typeToBuild, null, enableInstanceManipulation)
        { }



        private void SetMembersByReflection(IShapeshifterReader reader)
        {
            var packItemCandidates = _typeInspector.DataHolderMembers.ToDictionary(item => item.Name);

            foreach (var packItem in reader)
            {
                FieldOrPropertyMemberInfo target;
                if (packItemCandidates.TryGetValue(packItem.Key, out target))
                {
                    _memberSetsRequests.AddOrReplace(target, packItem.Value);
                }
            }
        }

        /// <summary>
        /// Sets the given member of the instance to the value.
        /// </summary>
        /// <param name="name">The name of the target field or property.</param>
        /// <param name="value">The value to set.</param>
        public void SetMember(string name, object value)
        {
            if (_instanceRead && !_enableInstanceManipulation)
            {
                throw Exceptions.InstanceAlreadyGivenAway();
            }

            var memberInfo = _typeInspector.GetFieldOrPropertyMemberInfo(name);

            if (!_instanceRead)
            {  //defer set
                _memberSetsRequests.AddOrReplace(memberInfo, value);
            }
            else
            {  //if the instance is already given away just set the value
                try
                {
                    var convertedValue = ValueConverter.ConvertValueToTargetType(memberInfo.Type, value);
                    memberInfo.SetValueFor(_instance, convertedValue);
                }
                catch (Exception ex)
                {
                    throw Exceptions.FailedToSetValue(memberInfo.Name, _typeInspector.Type, ex);
                }
            }
        }

        /// <summary>
        /// Gets the value of a member (field or property) specified by name as T.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="name">The name of the member sought.</param>
        /// <returns>The value of the given member.</returns>
        public T GetMember<T>(string name)
        {
            if (_instanceRead && !_enableInstanceManipulation)
            {
                throw Exceptions.InstanceAlreadyGivenAway();
            }

            var memberInfo = _typeInspector.GetFieldOrPropertyMemberInfo(name);

            if (!_instanceRead)
            { //read from the deferred set
                return _memberSetsRequests.Get<T>(memberInfo);
            }
            else
            { //instance is already given away, read from that
                try
                {
                    var value = memberInfo.GetValueFor(_instance);
                    return (T)ValueConverter.ConvertValueToTargetType(typeof(T), value);
                }
                catch (Exception ex)
                {
                    throw Exceptions.FailedToGetValue(name, _typeInspector.Type, ex);
                }
            }
        }

        /// <summary>
        /// Returns the instance created and set-up by the builder. Once an instance is given away the builder cannot be used further.
        /// </summary>
        /// <returns>The instance built.</returns>
        public object GetInstance()
        {
            if (_instanceRead)
            {
                throw Exceptions.InstanceAlreadyGivenAway();
            }

            _instance = _memberSetsRequests.CreateInstance();
            _instanceRead = true;
            return _instance;
        }


        private class MemberSetRequests
        {
            private readonly List<MemberSetRequest> _memberSetsRequests = new List<MemberSetRequest>();
            private readonly Type _typeToBuild;

            public MemberSetRequests(Type typeToBuild)
            {
                _typeToBuild = typeToBuild;
            }

            public void AddOrReplace(FieldOrPropertyMemberInfo memberInfo, object value)
            {
                var idxFound = _memberSetsRequests.FindIndex(item => item.MemberInfo.Equals(memberInfo));
                if (idxFound == -1)
                {
                    _memberSetsRequests.Add(new MemberSetRequest(memberInfo, value));
                }
                else
                {
                    _memberSetsRequests[idxFound] = new MemberSetRequest(memberInfo, value);
                }
            }

            public T Get<T>(FieldOrPropertyMemberInfo memberInfo)
            {
                try
                {
                    var setItem = _memberSetsRequests.FirstOrDefault(item => item.MemberInfo.Equals(memberInfo));
                    if (setItem == null)
                    {
                        return default(T);
                    }
                    return (T)ValueConverter.ConvertValueToTargetType(typeof(T), setItem.Value);
                }
                catch (Exception ex)
                {
                    throw Exceptions.FailedToGetValue(memberInfo.Name, _typeToBuild, ex);
                }
            }

            public object CreateInstance()
            {
                var instance = FormatterServices.GetUninitializedObject(_typeToBuild);
                foreach (var memberSetsRequest in _memberSetsRequests)
                {
                    var memberInfo = memberSetsRequest.MemberInfo;
                    var value = memberSetsRequest.Value;
                    try
                    {
                        var convertedValue = ValueConverter.ConvertValueToTargetType(memberInfo.Type, value);
                        memberInfo.SetValueFor(instance, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        throw Exceptions.FailedToSetValue(memberInfo.Name, _typeToBuild, ex);
                    }
                }

                return instance;
            }

            private class MemberSetRequest
            {
                private readonly FieldOrPropertyMemberInfo _memberInfo;
                private readonly object _value;

                public MemberSetRequest(FieldOrPropertyMemberInfo memberInfo, object value)
                {
                    _memberInfo = memberInfo;
                    _value = value;
                }

                public FieldOrPropertyMemberInfo MemberInfo
                {
                    get { return _memberInfo; }
                }

                public object Value
                {
                    get { return _value; }
                }
            }
        }
    }

    /// <summary>
    ///     Class helping in object creation in custom deserializers for type T.
    /// </summary>
    public class InstanceBuilder<T> : InstanceBuilder
    {
        /// <summary>
        /// Creates a builder for the type T. The builder tries to fill the new instance with the data in the reader.
        /// </summary>
        /// <param name="reader">Reader with member data.</param>
        public InstanceBuilder(IShapeshifterReader reader)
            : base(typeof(T), reader)
        {}

        /// <summary>
        /// Creates a builder for the type T.
        /// </summary>
        public InstanceBuilder()
            : base(typeof(T))
        { }

        /// <summary>
        /// Creates a builder for the type T. The builder tries to fill the new instance with the data in the reader.
        /// </summary>
        /// <param name="reader">Reader with member data.</param>
        /// <param name="enableInstanceManipulation">Allow member value set after getting the instance.</param>
        public InstanceBuilder(IShapeshifterReader reader, bool enableInstanceManipulation)
            : base(typeof(T), reader, enableInstanceManipulation)
        { }

        /// <summary>
        /// Creates a builder for the type T. 
        /// </summary>
        /// <param name="enableInstanceManipulation">Allow member value set after getting the instance.</param>
        public InstanceBuilder(bool enableInstanceManipulation)
            : base(typeof(T), enableInstanceManipulation)
        { }

        /// <summary>
        /// Returns the instance created and set-up by the builder. Once an instance is given away the builder cannot be used further.
        /// </summary>
        /// <returns>The instance built.</returns>
        public new T GetInstance()
        {
            return (T)base.GetInstance();
        }
    }
}