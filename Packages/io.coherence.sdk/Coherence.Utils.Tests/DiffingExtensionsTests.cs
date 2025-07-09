// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils.Tests
{
    using System;
    using Coherence.Tests;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Numerics;

    public class DiffingExtensionsTests : CoherenceTest
    {
        private static object[][] DiffersTestData => new[]
        {
            // string
            new object[] { "string 1", new List<string>(){"coherence", "coherence"}, false },
            new object[] { "string 2", new List<string>(){"foo", "bar"}, true },
            // double
            new object[] { "double 1", new List<double>(){15.01d, 15.01d}, false },
            new object[] { "double 2", new List<double>(){10.0d, 10.0d + (0.5d * DiffingExtensions.EpsilonDouble)}, false },
            new object[] { "double 3", new List<double>(){15.0d, 15.0d + (2.0d * DiffingExtensions.EpsilonDouble)}, true },
            // float
            new object[] { "float 1", new List<float>(){12.01f, 12.01f}, false },
            new object[] { "float 2", new List<float>(){12.0f, 12.0f + (0.5f *DiffingExtensions.EpsilonFloat)}, false },
            new object[] { "float 3", new List<float>(){12.0f, 12.0f + (2.0f * DiffingExtensions.EpsilonFloat)}, true },
            // Vector2
            new object[] { "Vector2 1", new List<Vector2>(){new (1.0f, 1.0f), new (1.0f, 0.999999f)}, false },
            new object[] { "Vector2 2", new List<Vector2>(){new (1.0f, 1.0f), new (1.0f, 1.0f + (2.0f * DiffingExtensions.EpsilonFloat))}, true },
            // Vector3
            new object[] { "Vector3 1", new List<Vector3>(){new (1.0f, 1.0f, 0.0f), new (1.0f, 0.999999f, 0.0f)}, false },
            new object[] { "Vector3 2", new List<Vector3>(){new (1.0f, 1.0f, 0.0f), new (1.0f, 1.0f + (2.0f * DiffingExtensions.EpsilonFloat), 0.0f)}, true },
            // Vector4
            new object[] { "Vector4 1", new List<Vector4>(){new (1.0f, 1.0f, 1.0f, 0.0f), new (1.0f, 1.0f, 1.0f, 0.000001f)}, false },
            new object[] { "Vector4 2", new List<Vector4>(){new (1.0f, 1.0f, 1.0f, 0.0f), new (1.0f, 1.0f, 1.0f, 0.0f + (2.0f * DiffingExtensions.EpsilonFloat))}, true },
            // Quaternion
            new object[] { "Quaternion 1", new List<Quaternion>(){new (1.0f, 1.0f, 1.0f, 0.0f), new (1.0f, 1.0f, 1.0f, 0.000001f)}, false },
            new object[] { "Quaternion 2", new List<Quaternion>(){new (1.0f, 1.0f, 1.0f, 0.0f), new (1.0f, 1.0f, 1.0f, 0.0f + (2.0f * DiffingExtensions.EpsilonFloat))}, true },
            // byte[]
            new object[] { "ByteArray 1", new List<byte[]>(){null, null}, false},
            new object[] { "ByteArray 2", new List<byte[]>(){new byte[]{ 0xFF, 0xA0 }, new byte[]{ 0xFF, 0xA0 }}, false},
            new object[] { "ByteArray 3", new List<byte[]>(){new byte[]{ 0xFF, 0xA0 }, null}, true},
            new object[] { "ByteArray 4", new List<byte[]>(){new byte[]{ 0xFF, 0xA0 }, new byte[]{ 0xA0, 0xDD }}, true},
        };

        [TestCaseSource(nameof(DiffersTestData))]
        public void DiffersFrom<T>(string description, List<T> values, bool differ)
        {
            Assert.That(values.Count, Is.EqualTo(2), "Test requires 2 values");

            switch (values)
            {
                case List<string> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<double> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<float> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<Vector2> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<Vector3> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<Vector4> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<Quaternion> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                case List<byte[]> l:
                    Assert.That(l[0].DiffersFrom(l[1]), Is.EqualTo(differ), description);
                    break;
                default:
                    Assert.That(true, Is.False, "Unsupported type");
                    break;
            }
        }
    }
}
