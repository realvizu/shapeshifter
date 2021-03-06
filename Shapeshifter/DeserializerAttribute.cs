﻿using System;
using Shapeshifter.Core;

namespace Shapeshifter
{
    /// <summary>
    ///     Attribute for marking custom deserializers of a given type and version. A custom deserializer can be any method (static or instance) with the required signature 
    ///      static object AnyName(IShapeshifterReader reader). 
    /// </summary>
    /// <remarks>
    ///     If it is applied to a static method it means that for the defined name and version this method will be called during
    ///     deserialization. Custom deserializer method must be either static or instance of a class with a default public constructor.
    ///     The signature must conform to object MyMethod(IShapeshifterReader reader).
    ///     Multiple DeserializerAttributes can be applied to a single method.
    ///     For the attribute a name and a version can be specified. The name is the name of the class in the serialized data
    ///     and the version is its serialized version
    ///     Versions are calculated automatically by Shapeshifter or defined by the <see cref="SerializerAttribute" /> or <see cref="ShapeshifterRootAttribute" />.
    ///     If no version is specified on a method that deserializer becomes the deserializer for the current version. Use this
    ///     feature with care as the current version might change if you temper the class. Also in such scenario target type must be given.
    /// </remarks>
    /// <example>
    /// <code>
    ///     [Deserializer(typeof(MyClass), 56789)]
    ///     public static object DeserializerForMyClass(IShapeshifterReader reader)
    ///     {}
    ///
    ///     [Deserializer("MyOldClass", 12345)]
    ///     public static object DeserializerForAllOldVersions(IShapeshifterReader reader)
    ///     {}
    ///
    ///     [Deserializer("MyOldClass", 32456)]
    ///     [Deserializer("MyOldClass", 67890)]
    ///     public static object DeserializerForSpecifiedOldVersions(IShapeshifterReader reader)
    ///     {}
    /// 
    ///     [Deserializer(typeof(MyBase),1 , ForAllDescendants = true)]
    ///     public static object DeserializerForAllDescendants(IShapeshifterReader reader, Type targetType)
    ///     {}
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class DeserializerAttribute : Attribute
    {
        private readonly Type _targeType;
        private readonly string _packformatName;
        private readonly uint? _version;
        private bool _forAllDescendants = false;

        /// <summary>
        /// Creates an instance of the attribute with the specified targetType
        /// </summary>
        /// <param name="targetType">Target type for the deserializer</param>
        public DeserializerAttribute(Type targetType)
            : this(targetType, targetType.GetPrettyName(), null)
        {
        }

        /// <summary>
        /// Creates an instance of the attribute with the specified targetType and version
        /// </summary>
        /// <param name="targetType">Target type for the deserializer</param>
        /// <param name="version">Target version for the deserializer</param>
        public DeserializerAttribute(Type targetType, uint version)
            : this(targetType, targetType.GetPrettyName(), version)
        {
        }

        /// <summary>
        /// Creates an instance of the attribute with the specified serialized type name and version
        /// </summary>
        /// <param name="packformatName">Serialized name of the target type</param>
        /// <param name="version">Target version for the deserializer</param>
        public DeserializerAttribute(string packformatName, uint version)
            : this(null, packformatName, version)
        {
        }

        private DeserializerAttribute(Type targetType, string packformatName, uint? version)
        {
            _targeType = targetType;
            _packformatName = packformatName;
            _version = version;            
        }

        /// <summary>
        /// Returns the type provided by the custom deserializer marked by this attribute
        /// </summary>
        public Type TargeType
        {
            get { return _targeType; }
        }

        /// <summary>
        /// Returns the serialized type name targeted by the custom deserializer marked by this attribute
        /// </summary>
        public string PackformatName
        {
            get { return _packformatName; }
        }

        /// <summary>
        /// Returns the version targeted by the custom deserializer marked by this attribute
        /// </summary>
        public uint? Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Specifies if the marked custom deserializer method should be used for all descendants of the given target type.
        /// Descendant detection uses the descendantSearchScope specified when creating <see cref="ShapeshifterSerializer"/>.
        /// </summary>
        public bool ForAllDescendants
        {
            get { return _forAllDescendants; }
            set { _forAllDescendants = value; }
        }
    }
}