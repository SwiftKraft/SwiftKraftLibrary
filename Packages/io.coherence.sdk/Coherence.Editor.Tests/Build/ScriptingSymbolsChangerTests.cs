// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests.Build
{
    using System;
    using Coherence.Build;
    using Coherence.Tests;
    using Log;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Build;

    public sealed class ScriptingSymbolsChangerTests : CoherenceTest
    {
        private string[] previousSettings;
        private LogLevel previousLogLevel;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out previousSettings);
            var logSettings = Log.GetSettings();
            previousLogLevel = logSettings.LogLevel;
        }

        [TearDown]
        public override void TearDown()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, previousSettings);
            var logSettings = Log.GetSettings();
            logSettings.LogLevel = previousLogLevel;
            logSettings.Save();
            base.TearDown();
        }

        [Test]
        [Description("Add the COHERENCE_SIMULATOR symbol to the collection of scripting symbols")]
        public void ChangeScriptingSymbols_Adds_Coherence_Simulator()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, Array.Empty<string>());

            ScriptingSymbolsChanger.ChangeScriptingSymbols(NamedBuildTarget.Server, false);

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var settings);
            Assert.That(settings, Contains.Item("COHERENCE_SIMULATOR"));
        }

        [Test]
        [Description("Add a custom symbol to the collection and ensure that it is restored when the simulator build finishes")]
        public void ChangeScriptingSymbols_AddCoherenceSimulator_Reset()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, new string[]
            {
                "TEST_SYMBOL",
            });

            ScriptingSymbolsChanger.ChangeScriptingSymbols(NamedBuildTarget.Server, false);

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var settings);
            Assert.That(settings, Contains.Item("COHERENCE_SIMULATOR"));
            Assert.That(settings, Contains.Item("TEST_SYMBOL"));

            ScriptingSymbolsChanger.RestoreScriptingSymbols(NamedBuildTarget.Server);
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var restoredSettings);
            Assert.That(restoredSettings, Does.Not.Contain("COHERENCE_SIMULATOR"));
            Assert.That(restoredSettings, Contains.Item("TEST_SYMBOL"));
        }

        [Test]
        [Description("Include the debug log symbol in the collection when the logging level has been set to Debug")]
        public void ChangeScriptingSymbols_Adds_Coherence_Simulator_And_DebugLog()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, Array.Empty<string>());
            var logSettings = Log.GetSettings();
            var previousLevel = logSettings.LogLevel;
            logSettings.LogLevel = LogLevel.Debug;
            logSettings.Save();

            ScriptingSymbolsChanger.ChangeScriptingSymbols(NamedBuildTarget.Server, false);

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var settings);
            Assert.That(settings, Contains.Item("COHERENCE_SIMULATOR"));
            Assert.That(settings, Contains.Item(LogConditionals.Debug));

            logSettings.LogLevel = previousLevel;
            logSettings.Save();
        }

        [Test]
        [Description("Include the debug log symbol and ensure it is removed from the collection after the build")]
        public void ChangeScriptingSymbols_AddsCoherence_SimulatorAndDebugLog_Reset()
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, new string[]
            {
                "TEST_SYMBOL",
            });

            var logSettings = Log.GetSettings();
            var previousLevel = logSettings.LogLevel;
            logSettings.LogLevel = LogLevel.Debug;
            logSettings.Save();

            ScriptingSymbolsChanger.ChangeScriptingSymbols(NamedBuildTarget.Server, false);

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var settings);
            Assert.That(settings, Contains.Item("COHERENCE_SIMULATOR"));
            Assert.That(settings, Contains.Item(LogConditionals.Debug));

            logSettings.LogLevel = previousLevel;
            logSettings.Save();

            ScriptingSymbolsChanger.RestoreScriptingSymbols(NamedBuildTarget.Server);
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out var restoredSettings);
            Assert.That(restoredSettings, Does.Not.Contain("COHERENCE_SIMULATOR"));
            Assert.That(restoredSettings, Does.Not.Contain(LogConditionals.Debug));
            Assert.That(restoredSettings, Contains.Item("TEST_SYMBOL"));
        }
    }
}
