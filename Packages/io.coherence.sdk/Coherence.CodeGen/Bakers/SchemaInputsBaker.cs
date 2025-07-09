// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.CodeGen
{
    using System;
    using System.Collections.Generic;
    using Scriban.Runtime;

    public class SchemaInputsBaker : IBaker
    {
        public BakeResult Bake(CodeGenData codeGenData)
        {
            var inputDefinitions = codeGenData.SchemaDefinition.InputDefinitions;
            var result = true;
            var files = new HashSet<string>();
            var scribanWriter = new ScribanWriter();

            foreach (var input in inputDefinitions)
            {
                var scribanOptions = GetOptions(codeGenData);

                scribanOptions.Model[0]["inputDefinition"] = input;

                var renderResult = scribanWriter.Render(scribanOptions, System.IO.Path.Combine(codeGenData.OutputDirectory, "Interop"), input.name);

                result &= renderResult.Success;

                if (!string.IsNullOrEmpty(renderResult.FileGenerated))
                {
                    files.Add(renderResult.FileGenerated);
                }
            }

            return new BakeResult
            {
                Success = result,
                GeneratedFiles = files,
            };
        }

        private static ScribanOptions GetOptions(CodeGenData codeGenData)
        {
            var modelObj = new ScriptObject
            {
                ["no_unity_refs"] = codeGenData.NoUnityReferences,
            };

            modelObj.Import("GetSerializeMethod", new Func<string, string>(TemplateMethods.GetSerializeMethod));
            modelObj.Import("GetSerializeParams",
                new Func<string, bool, string>(TemplateMethods.GetDefaultSerializeParameters));
            modelObj.Import("GetInteropTypeFromCSharpType", new Func<string, string>(TemplateMethods.GetInteropTypeFromCSharpType));
            modelObj.Import("GetFromInteropConversion", new Func<string, string, string>(TemplateMethods.GetFromInteropConversion));

            var usingDirectives = new List<string>
            {
                "Coherence.ProtocolDef",
                "Coherence.Serializer",
                "Coherence.Brook",
                "Coherence.Entities",
                "Coherence.Log",
                "Coherence.Core",
                "System.Collections.Generic",
                "System.Runtime.InteropServices",
                "System"
            };

            usingDirectives.Add(codeGenData.NoUnityReferences ? "System.Numerics" : "UnityEngine");

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = usingDirectives,
                TemplateNames = new List<string>
                {
                    "schema_input",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>
                {
                    modelObj,
                },
            };
            return scribanOptions;
        }
    }
}
