namespace Coherence.CodeGen
{
    using System.Collections.Generic;
    using System.Linq;

    public class SchemaDefinitionBaker : IBaker
    {
        private readonly List<IBaker> definitionBakers = new()
        {
            new SchemaComponentsBaker(),
            new SchemaInputsBaker(),
            new SchemaCommandsBaker(),
            new SchemaArchetypesBaker(),
            new ImplementationsBaker(),
        };

        public BakeResult Bake(CodeGenData codeGenData)
        {
            var results = new List<BakeResult>();

            foreach (var baker in definitionBakers)
            {
                results.Add(baker.Bake(codeGenData));
            }

            var success = !TasksHaveErrors(results);

            var files = new HashSet<string>();

            if (!success)
            {
                return new BakeResult
                {
                    Success = false,
                    GeneratedFiles = files,
                };
            }

            foreach (var task in results)
            {
                files.UnionWith(task.GeneratedFiles);
            }

            return new BakeResult
            {
                Success = true,
                GeneratedFiles = files,
            };
        }

        private static bool TasksHaveErrors(List<BakeResult> results)
        {
            return results.Any(r => !r.Success);
        }
    }
}
