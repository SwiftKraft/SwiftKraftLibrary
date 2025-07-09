// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Scriban.Runtime;

    public class SchemaCommandsBaker : IBaker
    {
        public BakeResult Bake(CodeGenData codeGenData)
        {
            var commandDefinitions = codeGenData.SchemaDefinition.CommandDefinitions;
            var result = true;
            var files = new HashSet<string>();
            var scribanWriter = new ScribanWriter();

            foreach (var commandDef in commandDefinitions)
            {
                var hasRefFields = commandDef.members.Any(b => b.typeName.Equals("Entity"));

                var scribanOptions = GetOptions(codeGenData);

                scribanOptions.Model[0]["commandDefinition"] = commandDef;
                scribanOptions.Model[0]["hasRefFields"] = hasRefFields;

                var renderResult =
                    scribanWriter.Render(scribanOptions, System.IO.Path.Combine(codeGenData.OutputDirectory, "Interop"), commandDef.name);

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
                new Func<string, Dictionary<string, string>, bool, string>(TemplateMethods
                    .GetSerializeParametersFromOverrides));
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
                codeGenData.NoUnityReferences ? "System.Numerics" : "UnityEngine"
            };

            return new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = usingDirectives,
                TemplateNames = new List<string>
                {
                    "schema_command",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>
                {
                    modelObj,
                },
            };
        }
    }
}
