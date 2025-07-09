namespace Coherence.CodeGen
{
    using System.Collections.Generic;
    using Scriban.Runtime;

    public class DefinitionBaker : IBaker
    {
        public BakeResult Bake(CodeGenData data)
        {
            var modelObj = new ScriptObject
            {
                ["project_schema"] = data.SchemaDefinition,
                ["extended_def"] = data.ExtendedDefinition,
                ["schema_id"] = data.SchemaDefinition.SchemaId,
                ["no_unity_refs"] = data.NoUnityReferences,
            };

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "System.Collections.Generic",
                    "Coherence.ProtocolDef",
                    "Coherence.Brook",
                    "Coherence.Connection",
                    "Coherence.Entities",
                    "Coherence.Serializer",
                    "Coherence.Log",
                    "Coherence.SimulationFrame"
                },
                TemplateNames = new List<string>
                {
                    "definition",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>
                {
                    modelObj,
                },
            };

            var scribanWriter = new ScribanWriter();
            var renderResult = scribanWriter.Render(scribanOptions, data.OutputDirectory, "Definition");
            var files = new HashSet<string>();

            if (renderResult.Success)
            {
                files.Add(renderResult.FileGenerated);
            }

            return new BakeResult
            {
                Success = renderResult.Success,
                GeneratedFiles = files,
            };
        }
    }
}
