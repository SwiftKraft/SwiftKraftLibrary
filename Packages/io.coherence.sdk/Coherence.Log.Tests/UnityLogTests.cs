// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;
using Coherence.Log;
using NUnit.Framework;
using UnityEngine;

namespace Coherence.Log.Tests
{
    using Coherence.Log.Targets;
    using System.Collections.Generic;
    using Coherence.Tests;

    public class LogClassA { }
    public class LogClassB { }

    public class ExampleBehaviour : MonoBehaviour { }

    public class UnityLogTests : CoherenceTest
    {
        [Test]
        public void UnityContextArgs()
        {
            var exampleGO = new GameObject();
            exampleGO.name = "example";
            var behaviour = exampleGO.AddComponent<ExampleBehaviour>() as ExampleBehaviour;

            var parentLogger = Log.GetLogger<LogClassA>().WithArgs(("context", behaviour), ("project_id", 1234));
            var childLogger = parentLogger.With<LogClassB>().WithArgs(("child_info", "ABCD"));

            parentLogger.Debug("PARENT", ("parent_arg", "HIJK"));
            childLogger.Debug("CHILD", ("child_arg", 9876));
            Debug.Log("SAMPLE", behaviour);
        }

        [Test]
        public void LogPassesArgsWhenFiltered()
        {
            var parentLogger = Log.GetLogger<LogClassA>().WithArgs(("a", 1), ("b", 2));

            foreach (var target in parentLogger.LogTargets)
            {
                target.Level = LogLevel.Error;
            }

            var childLogger = parentLogger.With<LogClassB>().WithArgs(("c", 3), ("d", 4));

            Logger.LogDelegate onLog = (level, filtered, log, source, args) =>
            {
                Assert.That(filtered, "Filtered");
                Assert.That(args.Length, level == LogLevel.Debug
                    ? Is.EqualTo(3)
                    : Is.EqualTo(5), "Arg count");
            };

            Logger.OnLog += onLog;
            try
            {
                parentLogger.Debug("x", ("x", 10));
                childLogger.Trace("y", ("y", 20));
            }
            finally
            {
                Logger.OnLog -= onLog;
            }
        }

        [Test]
        public void UnityValueArgs()
        {
            var testArgs = new (string, object)[]
            {
                ("arg0", 123),
                ("arg1", "abc"),
                ("arg2", 0.1f),
                ("arg3", 6969),
                ("arg4", "hello"),
                ("arg5", new GameObject()),
                ("arg6", null),
            };

            var parentLogger = Log.GetLogger<LogClassA>().WithArgs(testArgs[0], testArgs[1]);
            var childLogger = parentLogger.With<LogClassB>().WithArgs(testArgs[2], testArgs[3]);

            void checkLogs(LogLevel level, bool filtered, string log, Type source, ICollection<(string, object)> args)
            {
                if (source == typeof(LogClassA))
                {
                    Assert.That(args.Count, Is.EqualTo(3));
                }
                else
                {
                    Assert.That(args.Count, Is.EqualTo(6));
                }
            };

            Logger.OnLog += checkLogs;
            try
            {
                parentLogger.Debug("PARENT", testArgs[4]);
                childLogger.Debug("CHILD", testArgs[5], testArgs[6]);
            }
            finally
            {
                Logger.OnLog -= checkLogs;
            }
        }
    }
}
