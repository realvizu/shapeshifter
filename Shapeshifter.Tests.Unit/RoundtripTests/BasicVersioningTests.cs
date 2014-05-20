﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Shapeshifter.Core;

namespace Shapeshifter.Tests.Unit.RoundtripTests
{
    [TestFixture]
    public class BasicVersioningTests : TestsBase
    {
        [Test]
        public void ReadOldVersionIntoNewVersion()
        {
            var input = new MyClassVersionOne() {Id = "42"};
            var serializer = GetSerializer<MyClassVersionOne>();
            var deserializer = GetSerializer<MyClassVersionTwo>();
            var packed = serializer.Serialize(input);

            var result = deserializer.Deserialize(packed);

            result.Id.Should().Be(42);
        }

        [Test]
        public void HandleMultipleVersions()
        {
            var inputOne = new MyClassVersionOne() { Id = "42" };
            var inputTwo = new MyClassVersionTwo() { Id = 100 };

            var serializerOne = GetSerializer<MyClassVersionOne>();
            var serializerTwo = GetSerializer<MyClassVersionTwo>();
            var deserializer = GetSerializer<MyClassVersionThree>();
            var packedOne = serializerOne.Serialize(inputOne);
            var packedTwo = serializerTwo.Serialize(inputTwo);

            var resultOne = deserializer.Deserialize(packedOne);
            var resultTwo = deserializer.Deserialize(packedTwo);

            resultOne.UserId.Should().Be("42");
            resultTwo.UserId.Should().Be("100");
        }

        [Test]
        public void DetectTheVersionNumberHelper()
        {
            var ti = new TypeInspector(typeof (MyClassVersionTwo));
            Debug.Print(ti.Version.ToString());
        }
    }
    
    [DataContract]
    [Serializer]
    public class MyClassVersionOne
    {
        [DataMember]
        public string Id { get; set; }
    }

    [DataContract]
    [Serializer]
    public class MyClassVersionTwo //example for switching property type
    {
        [DataMember]
        public int Id { get; set; }

        [Deserializer("MyClassVersionOne", 1098654145)]
        private static object TransformVersionOne(IPackformatValueReader reader)
        {
            var oldFormatValue = reader.GetValue<string>("Id");
            var newFormatValue = Int32.Parse(oldFormatValue);
            return new MyClassVersionTwo() {Id = newFormatValue};
        }
    }

    [DataContract]
    [Serializer]
    public class MyClassVersionThree //example for switching property name
    {
        [DataMember]
        public string UserId { get; set; }

        [Deserializer("MyClassVersionOne", 1098654145)]
        private static object TransformVersionOne(IPackformatValueReader reader)
        {
            var value = reader.GetValue<string>("Id");
            return new MyClassVersionThree() { UserId = value };
        }

        [Deserializer("MyClassVersionTwo", 2143954606)]
        private static object TransformVersionTwo(IPackformatValueReader reader)
        {
            var value = reader.GetValue<int>("Id");
            return new MyClassVersionThree() { UserId = value.ToString() };
        }
    }


}