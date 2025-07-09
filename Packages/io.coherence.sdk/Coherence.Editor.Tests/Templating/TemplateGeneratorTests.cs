// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests.Templating
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Coherence.Tests;
    using Editor.Templating;
    using Microsoft.CSharp;
    using NUnit.Framework;
    using UnityEditor;

    public class TemplateGeneratorTests : CoherenceTest
    {
        private const string CustomInstantiator = nameof(CustomInstantiator);
        private const string CustomProvider = nameof(CustomProvider);

        private string networkObjectProviderFileName = "";
        private string networkObjectProviderClassPath = "";
        private string networkObjectInstantiatorFileName = "";
        private string networkObjectInstantiatorClassPath = "";

        [SetUp]
        public void Setup()
        {
            networkObjectProviderFileName = "Assets/NetworkObjectProviderTest.cs";
            networkObjectInstantiatorFileName = "Assets/NetworkObjectInstantiatorTest.cs";
        }

        [TearDown]
        public void RemoveFiles()
        {
            AssetUtils.DeleteFile(networkObjectProviderClassPath);
            AssetUtils.DeleteFile(networkObjectInstantiatorClassPath);
        }

        [Test]
        [Description("INetworkObjectProvider template created and is valid")]
        public void INetworkObjectProvider_File_Created()
        {
            var templatePath = Paths.providerTemplatePath;
            networkObjectProviderClassPath = AssetDatabase.GenerateUniqueAssetPath(networkObjectProviderFileName);

            var generator = new TemplateGenerator(templatePath, networkObjectProviderClassPath, CustomProvider);
            generator.Generate();

            var contents = File.ReadAllText(networkObjectProviderFileName);
            Assert.That(HasCorrectClassName(generator.ClassName, contents), Is.True);
            Assert.That(TryCompile(contents, out _), Is.True);
        }

        [Test]
        [Description("INetworkObjectInstantiator template created and is valid")]
        public void INetworkObjectInstantiator_File_Created()
        {
            var templatePath = Paths.instantiatorTemplatePath;
            networkObjectInstantiatorClassPath = AssetDatabase.GenerateUniqueAssetPath(networkObjectInstantiatorFileName);

            var generator = new TemplateGenerator(templatePath, networkObjectInstantiatorClassPath, CustomInstantiator);
            generator.Generate();

            var contents = File.ReadAllText(networkObjectInstantiatorFileName);
            Assert.That(HasCorrectClassName(generator.ClassName, contents), Is.True);
            Assert.That(TryCompile(contents, out _), Is.True);
        }

        private static bool TryCompile(string sourceCode, out Assembly assembly)
        {
            var assemblyPaths = GetFullPaths(new[]
            {
                "UnityEditor",
                "UnityEngine",
                "System.Core",
                "netstandard",
                "System",
                "UnityEngine.CoreModule",
                "Coherence.Toolkit",
            });

            assembly = BuildDLL(new [] { sourceCode }, assemblyPaths);
            return assembly is not null;
        }

        private static string[] GetFullPaths(string[] dllNames) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => dllNames.Contains(asm.GetName().Name))
                .Select(asm => asm.Location)
                .ToArray();

        private static Assembly BuildDLL(string[] sources, string[] assemblyReferences)
        {
            var providerOptions = new Dictionary<string, string>
            {
                { "CompilerVersion", "v3.5" }
            };
            var provider = new CSharpCodeProvider(providerOptions);
            var options = new CompilerParameters();

            foreach (var assemblyLocation in assemblyReferences)
            {
                options.ReferencedAssemblies.Add(assemblyLocation);
            }

            options.GenerateExecutable = false;
            options.GenerateInMemory = true;
            options.IncludeDebugInformation = false;

            var result = provider.CompileAssemblyFromSource(options, sources);
            return result.CompiledAssembly;
        }

        private bool HasCorrectClassName(string className, string fileContents)
        {
            var classRegex = new Regex($"class {className}");
            return classRegex.Match(fileContents).Success;
        }
    }
}
