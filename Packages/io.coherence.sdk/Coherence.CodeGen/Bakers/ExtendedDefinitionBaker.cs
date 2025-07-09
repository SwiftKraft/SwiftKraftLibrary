namespace Coherence.CodeGen
{
    using System.Collections.Generic;
    using Scriban.Runtime;

    public class ExtendedDefinitionBaker : IBaker
    {
        public BakeResult Bake(CodeGenData data)
        {
            if (!data.ExtendedDefinition)
            {
                return new BakeResult
                {
                    Success = true,
                    GeneratedFiles = new HashSet<string>(),
                };
            }

            var usingDirectives = new List<string>
            {
                "System.Collections.Generic",
                "System.Linq",
                "Coherence",
                "Coherence.ProtocolDef",
                "Coherence.Connection",
                "Coherence.Entities",
                "Coherence.SimulationFrame",
            };

            if (data.NoUnityReferences)
            {
                usingDirectives.Add("System.Numerics");
            }
            else
            {
                usingDirectives.Add("UnityEngine");
                usingDirectives.Add("Coherence.Interpolation");
            }

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = usingDirectives,
                TemplateNames = new List<string>
                {
                    "extended_definition",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>(),
            };

            var scribanWriter = new ScribanWriter();
            var renderResult =
                scribanWriter.Render(scribanOptions, data.OutputDirectory, "ExtendedDefinition");
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
