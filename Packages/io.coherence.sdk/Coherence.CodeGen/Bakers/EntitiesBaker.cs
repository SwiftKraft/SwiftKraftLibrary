namespace Coherence.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Scriban.Runtime;

    public class EntitiesBaker : IBaker
    {
        public BakeResult Bake(CodeGenData codeGenData)
        {
            if (codeGenData.NoUnityReferences)
            {
                return new BakeResult
                {
                    Success = true,
                    GeneratedFiles = new HashSet<string>(),
                };
            }

            var entitiesData = codeGenData.EntitiesData;
            var result = true;
            var files = new HashSet<string>();
            var scribanWriter = new ScribanWriter();

            foreach (var syncedBehaviour in entitiesData.BehaviourData)
            {
                var scribanOptions = GetOptions();

                if (codeGenData.EntitiesData.InputData.TryGetValue(syncedBehaviour.BehaviourName, out var inputName))
                {
                    foreach (var input in codeGenData.SchemaDefinition.InputDefinitions.Where(input =>
                                 input.name.Equals(inputName)))
                    {
                        scribanOptions.Model[0]["inputDefinition"] = input;
                        break;
                    }
                }

                scribanOptions.Model[0]["entityData"] = syncedBehaviour;

                var renderResult = scribanWriter.Render(scribanOptions, codeGenData.OutputDirectory,
                    syncedBehaviour.BehaviourName);
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

        private static ScribanOptions GetOptions()
        {
            var componentsObject = new ScriptObject();
            componentsObject.Import("resolveCsharpType", new Func<string, string, string>(ResolveCsharpType));
            componentsObject.Import("ResolveCommandParams", new Func<object, string>(ResolveCommandParams));

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = new List<string>
                {
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "UnityEngine",
                    "Coherence.Toolkit",
                    "Coherence.Toolkit.Bindings",
                    "Coherence.Entities",
                    "Coherence.ProtocolDef",
                    "Coherence.Brook",
                    "Coherence.Toolkit.Bindings.ValueBindings",
                    "Coherence.Toolkit.Bindings.TransformBindings",
                    "Coherence.Connection",
                    "Coherence.SimulationFrame",
                    "Coherence.Interpolation",
                    "Coherence.Log",
                    "Logger = Coherence.Log.Logger",
                    "UnityEngine.Scripting",
                },
                TemplateNames = new List<string>
                {
                    "baked_binding",
                    "baked_entity",
                },
                TemplateLoader = new TemplateLoaderFromDisk(),
                Model = new List<ScriptObject>
                {
                    componentsObject,
                },
            };

            return scribanOptions;
        }

        private static string ResolveCsharpType(string bindingClassName, string cSharpType)
        {
            return bindingClassName.Equals("ReferenceBinding") ? "Entity" : cSharpType;
        }

        private static string ResolveCommandParams(object field)
        {
            if (field is not List<CommandParameterInfo> list)
            {
                return null;
            }

            var parameters = new StringBuilder();

            for (var i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    parameters.Append(",");
                }

                var member = list[i];

                var set = member.Type switch
                {
                    "UnityEngine.Transform" => $"bridge.EntityIdToTransform(command.{member.Name})",
                    "Coherence.Toolkit.CoherenceSync" => $"bridge.EntityIdToCoherenceSync(command.{member.Name})",
                    "UnityEngine.GameObject" => $"bridge.EntityIdToGameObject(command.{member.Name})",
                    _ => $"({member.Type})(command.{member.Name})",
                };

                parameters.Append(set);
            }

            return parameters.ToString();
        }
    }
}
