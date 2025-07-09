namespace Coherence.CodeGen
{
    using System.Collections.Generic;
    using Scriban.Runtime;

    public class SchemaArchetypesBaker : IBaker
    {
        private ScribanWriter scribanWriter;

        public BakeResult Bake(CodeGenData data)
        {
            var modelObj = new ScriptObject
            {
                ["archetypes"] = data.SchemaDefinition.ArchetypeDefinitions,
            };

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "System.Collections.Generic",
                    "Coherence.ProtocolDef",
                },
                TemplateNames = new List<string>
                {
                    "schema_archetype",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>
                {
                    modelObj,
                },
            };

            var scribanWriter = new ScribanWriter();
            var renderResult = scribanWriter.Render(scribanOptions, data.OutputDirectory, "Archetypes");
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
