// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils.Tests
{
    using System;
    using System.Numerics;
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Tests;
    using Log;
    using NUnit.Framework;
    using UnityEngine.TestTools;
    using System.Text.RegularExpressions;

    public class BoundsTests : CoherenceTest
    {
        private static object[][] VectorList => new[]
        {
            new object[] { "normal vector3", new Vector3(10f, 10f, 10f), new Vector3(10f, 10f, 10f), false },
            new object[] { "normal negative vector3", new Vector3(-10f, -10f, -10f), new Vector3(-10f, -10f, -10f), false },
            new object[] { "negative infinity vector3", new Vector3(float.NegativeInfinity), new Vector3(float.MinValue), true },
            new object[] { "single axis is negative infinity", new Vector3(float.NegativeInfinity, 10f, -10f), new Vector3(float.MinValue, 10f, -10f), true},
            new object[] { "positive infinity vector3", new Vector3(float.PositiveInfinity), new Vector3(float.MaxValue), true },
            new object[] { "single axis is positive infinity", new Vector3(float.PositiveInfinity, 10f, -10f), new Vector3(float.MaxValue, 10f, -10f), true},
            new object[] { "NaN vector3", new Vector3(float.NaN), new Vector3(0f), true },
            new object[] { "single axis is NaN", new Vector3(float.NaN, 10f, -10f), new Vector3(0f, 10f, -10f), true },
        };

        [TestCaseSource(nameof(VectorList))]
        public void CheckNormalVectorsForNanAndInfinityTest(string description, Vector3 inputValue, Vector3 expectedValue, bool expectingWarning)
        {
            bool observedWarning = false;
            LogLevel? actualLogLevel = null;
            const LogLevel expectedLogLevel = LogLevel.Warning;
            bool? actualLogFiltered = false;
            const bool expectedLogFiltered = false;
            Vector3? loggedInvalidPosition = null;

            try
            {
                Logger.OnLog += OnLog;

                Vector3 actualValue = inputValue;
                Bounds.CheckPositionForNanAndInfinity(ref actualValue, logger);

                Assert.That(expectingWarning, Is.EqualTo(observedWarning), description);

                if (expectingWarning)
                {
                    Assert.That(actualValue, Is.EqualTo(expectedValue), description);
                    Assert.That(actualLogLevel, Is.EqualTo(expectedLogLevel), description);
                    Assert.That(actualLogFiltered, Is.EqualTo(expectedLogFiltered), description);
                    Assert.That(loggedInvalidPosition.Value.X, Is.EqualTo(inputValue.X), description);
                    Assert.That(loggedInvalidPosition.Value.Y, Is.EqualTo(inputValue.Y), description);
                    Assert.That(loggedInvalidPosition.Value.Z, Is.EqualTo(inputValue.Z), description);

                    Assert.That(logger.GetCountForWarningID(Warning.BoundsPositionInvalid), Is.EqualTo(1));
                }
            }
            finally
            {
                Logger.OnLog -= OnLog;
            }

            void OnLog(LogLevel level, bool filtered, string log, Type source, (string key, object value)[] args)
            {
                observedWarning = true;
                loggedInvalidPosition = (Vector3)args.FirstOrDefault().value;
                actualLogLevel = level;
                actualLogFiltered = filtered;
            }
        }

        private static object[][] ClampList => new[]
        {
            new object[] { "int within range", new List<int>() { 4, 3, 10 }, 4 },
            new object[] { "int less than min", new List<int>() { -4, -3, 10 }, -3 },
            new object[] { "int more than max", new List<int>() { 20, 4, 10 }, 10 },
            new object[] { "uint within range", new List<uint>() { 400, 300, 500 }, (uint)400 },
            new object[] { "uint less than min", new List<uint>() { 2, 4, 10 }, (uint)4 },
            new object[] { "uint more than max", new List<uint>() { 20, 4, 10 }, (uint)10 },
        };

        [TestCaseSource(nameof(ClampList))]
        public void CheckClamp<T>(string description, List<T> input, T expected)
        {
            switch (input)
            {
                case List<int> v:
                    Assert.That(Bounds.Clamp(v[0], v[1], v[2]), Is.EqualTo(expected), description);
                    break;
                case List<uint> v:
                    Assert.That(Bounds.Clamp(v[0], v[1], v[2]), Is.EqualTo(expected), description);
                    break;
            }
        }
    }
}
