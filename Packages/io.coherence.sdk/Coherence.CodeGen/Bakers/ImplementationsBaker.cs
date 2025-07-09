namespace Coherence.CodeGen
{
    using System;
    using System.Collections.Generic;
    using Scriban.Runtime;

    public class ImplementationsBaker : IBaker
    {
        public BakeResult Bake(CodeGenData data)
        {
            if (data.NoUnityReferences || !data.BakeToolkitImplementations)
            {
                return new BakeResult
                {
                    Success = true,
                    GeneratedFiles = new HashSet<string>(),
                };
            }

            var result = true;
            var files = new HashSet<string>();

            var scribanWriter = new ScribanWriter();

            result &= GenerateImplBridge(scribanWriter, data.OutputDirectory, files);

            result &= GenerateImplLiveQuery(scribanWriter, data.OutputDirectory, files);

            result &= GenerateImplTagQuery(scribanWriter, data.OutputDirectory, files);

            result &= GenerateImplGlobalQuery(scribanWriter, data.OutputDirectory, files);

            result &= GenerateImplSync(scribanWriter, data.OutputDirectory, files);

            var interopDir = System.IO.Path.Combine(data.OutputDirectory, "Interop");

            result &= GenerateImplInterop(data, scribanWriter, interopDir, files);

            result &= GenerateInteropAsmdef(interopDir);

            return new BakeResult
            {
                Success = result,
                GeneratedFiles = files,
            };
        }

        private static bool GenerateImplBridge(ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "UnityEngine",
                    "Coherence.Toolkit",
                    "System",
                    "Coherence.ProtocolDef",
                    "ConnectionType = Coherence.Connection.ConnectionType",
                    "ClientID = Coherence.Connection.ClientID",
                    "Coherence.Entities",
                    "Coherence.SimulationFrame",
                    "Coherence.Core",
                },
                TemplateNames = new List<string>
                {
                    "impl_bridge",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplBridge");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }

        private static bool GenerateImplLiveQuery(ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            ScribanOptions scribanOptions = new()
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "UnityEngine",
                    "Coherence.Entities",
                    "Toolkit",
                    "Coherence.SimulationFrame",
                },
                TemplateNames = new List<string>
                {
                    "impl_livequery",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplLiveQuery");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }

        private static bool GenerateImplTagQuery(ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            ScribanOptions scribanOptions = new()
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "UnityEngine",
                    "Coherence.Entities",
                    "Toolkit",
                    "Coherence.SimulationFrame",
                },
                TemplateNames = new List<string>
                {
                    "impl_tagquery",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplTagQuery");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }

        private static bool GenerateImplGlobalQuery(ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            ScribanOptions scribanOptions = new()
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "UnityEngine",
                    "Coherence.Entities",
                    "Toolkit",
                    "Coherence.SimulationFrame",
                },
                TemplateNames = new List<string>
                {
                    "impl_globalquery",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplGlobalQuery");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }

        private static bool GenerateImplSync(ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            ScribanOptions scribanOptions = new()
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "UnityEngine",
                    "Coherence.Toolkit",
                    "System",
                    "Coherence.ProtocolDef",
                    "System.Collections.Generic",
                    "Log",
                    "Logger = Log.Logger",
                    "Coherence.Entities",
                    "Coherence.SimulationFrame",
                },
                TemplateNames = new List<string>
                {
                    "impl_sync",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplSync");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }

        private static bool GenerateInteropAsmdef(string savePath)
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(savePath, "Interop.asmdef"), @"
{
    ""name"": ""Coherence.Interop.Generated"",
    ""rootNamespace"": """",
    ""references"": [
      ""Coherence.Entities"",
      ""Coherence.Core"",
      ""Coherence.ProtocolDef"",
      ""Coherence.Core.Native"",
      ""Coherence.Brisk"",
      ""Coherence.Log"",
      ""Coherence.Common"",
      ""Coherence.SimulationFrame"",
      ""Coherence.Serializer"",
      ""Coherence.Toolkit"",
      ""Coherence.Utils"",
      ""Coherence.Brook"",
      ""Coherence.Interpolation""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": true,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}
");

            System.IO.File.WriteAllText(System.IO.Path.Combine(savePath, "Interop.asmdef.meta"), @"fileFormatVersion: 2
guid: 2057d415ee6eb74b698acba0678889f6
AssemblyDefinitionImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
");

            return true;
        }

        private static bool GenerateImplInterop(CodeGenData data, ScribanWriter scribanWriter, string savePath,
            HashSet<string> files)
        {
            var modelObj = new ScriptObject
            {
                ["project_schema"] = data.SchemaDefinition,
            };

            modelObj.Import("GetToInteropConversionBegin", new Func<string, string, string>(TemplateMethods.GetToInteropConversionBegin));
            modelObj.Import("GetToInteropConversionEnd", new Func<string, string>(TemplateMethods.GetToInteropConversionEnd));

            ScribanOptions scribanOptions = new()
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "System",
                    "System.Runtime.InteropServices",
                    "System.Text",
                    "Coherence",
                    "Coherence.Brisk",
                    "Coherence.ProtocolDef",
                    "Coherence.Connection",
                    "Coherence.Entities",
                    "System.Collections.Generic",
                    "Coherence.SimulationFrame",
                    "Coherence.Core"
                },
                TemplateNames = new List<string>
                {
                    "impl_interop",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject> { modelObj },
            };

            var renderResult = scribanWriter.Render(scribanOptions, savePath, "ImplInterop");

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return renderResult.Success;
        }
    }
}
