// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Scriban.Runtime;

    public class SchemaComponentsBaker : IBaker
    {
        public BakeResult Bake(CodeGenData codeGenData)
        {
            var componentDefinitions = codeGenData.SchemaDefinition.ComponentDefinitions;
            var result = true;
            var files = new HashSet<string>();
            var scribanWriter = new ScribanWriter();
            var componentsDictionary = new Dictionary<string, ComponentDefinition>();

            foreach (var componentDef in componentDefinitions)
            {
                componentsDictionary[componentDef.name] = componentDef;

                var archetypes =
                    GetArchetypesForComponent(codeGenData.SchemaDefinition.ArchetypeDefinitions, componentDef);

                result &= WriteComponentDefinition(codeGenData, componentDef, archetypes, scribanWriter, files);
            }

            result &= BakeArchetypeComponents(codeGenData, componentsDictionary, scribanWriter, files);

            return new BakeResult
            {
                Success = result,
                GeneratedFiles = files,
            };
        }

        private static bool BakeArchetypeComponents(CodeGenData codeGenData,
            Dictionary<string, ComponentDefinition> componentsDictionary,
            ScribanWriter scribanWriter, HashSet<string> files)
        {
            if (!codeGenData.ExtendedDefinition)
            {
                return true;
            }

            var emptyArchetypesList = new List<ArchetypeDefinition>();
            var result = true;

            foreach (var archetype in codeGenData.SchemaDefinition.ArchetypeDefinitions)
            {
                foreach (var lod in archetype.lods)
                {
                    foreach (var component in lod.items)
                    {
                        if (!componentsDictionary.TryGetValue(component.componentName, out var def))
                        {
                            throw new InvalidDataException(
                                $"Component Definition not found for Archetype Component {component.componentName}");
                        }

                        var componentDef =
                            new ComponentDefinition(
                                $"{component.componentName}{archetype.id}_LOD{lod.level}", component.bakeConditional)
                            {
                                id = component.id,
                                generatedByArchetype = true,
                                baseComponentName = component.componentName,
                                bitMasks = def.bitMasks,
                                members = CloneMembers(def.members),
                            };

                        foreach (var member in componentDef.members)
                        {
                            var matchingField = component.fields
                                .First(field => member.variableName.Equals(field.fieldName));

                            if (matchingField == null)
                            {
                                throw new InvalidDataException("Didn't find matching field in the archetype" +
                                                               $" for {member.variableName}");
                            }

                            member.overrides = matchingField.overrides;
                        }

                        result &= WriteComponentDefinition(codeGenData, componentDef, emptyArchetypesList,
                            scribanWriter,
                            files);
                    }
                }
            }

            return result;
        }

        private static List<ComponentMemberDescription> CloneMembers(List<ComponentMemberDescription> otherMembers)
        {
            var members = new List<ComponentMemberDescription>(otherMembers.Count);

            foreach (var member in otherMembers)
            {
                members.Add(new ComponentMemberDescription(member));
            }

            return members;
        }

        private static bool WriteComponentDefinition(CodeGenData codeGenData, ComponentDefinition componentDef,
            List<ArchetypeDefinition> archetypes,
            ScribanWriter scribanWriter, HashSet<string> files)
        {
            var hasRefFields = componentDef.members.Any(b => b.typeName.Equals("Entity"));

            var scribanOptions = GetOptions(codeGenData);

            scribanOptions.Model[0]["componentDefinition"] = componentDef;
            scribanOptions.Model[0]["hasRefFields"] = hasRefFields;
            scribanOptions.Model[0]["archetypes"] = archetypes;
            scribanOptions.Model[0]["extended_def"] = codeGenData.ExtendedDefinition;
            scribanOptions.Model[0]["fieldsWithSimFrames"] =
                componentDef.members.Where(m => m.overrides.TryGetValue("sim-frames", out var val) ? val == "true" : false).ToList();

            var renderResult =
                scribanWriter.Render(scribanOptions, System.IO.Path.Combine(codeGenData.OutputDirectory, "Interop"), componentDef.name);

            var result = renderResult.Success;

            if (!string.IsNullOrEmpty(renderResult.FileGenerated))
            {
                files.Add(renderResult.FileGenerated);
            }

            return result;
        }

        private static List<ArchetypeDefinition> GetArchetypesForComponent(
            List<ArchetypeDefinition> archetypeDefinitions, ComponentDefinition componentDef)
        {
            var archetypes = new List<ArchetypeDefinition>();

            foreach (var archetype in archetypeDefinitions)
            {
                foreach (var lod in archetype.lods)
                {
                    var found = false;

                    if (lod.items.Any(item => item.componentName.Equals(componentDef.name)))
                    {
                        archetypes.Add(archetype);
                        found = true;
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }

            return archetypes;
        }

        private static ScribanOptions GetOptions(CodeGenData codeGenData)
        {
            var modelObj = new ScriptObject
            {
                ["no_unity_refs"] = codeGenData.NoUnityReferences,
                ["extended_def"] = codeGenData.ExtendedDefinition,
            };

            modelObj.Import("GetSerializeMethod", new Func<string, string>(TemplateMethods.GetSerializeMethod));
            modelObj.Import("GetSerializeParams",
                new Func<string, Dictionary<string, string>, bool, string>(TemplateMethods
                    .GetSerializeParametersFromOverrides));
            modelObj.Import("GetInteropTypeFromCSharpType", new Func<string, string>(TemplateMethods.GetInteropTypeFromCSharpType));
            modelObj.Import("GetFromInteropConversion", new Func<string, string, string>(TemplateMethods.GetFromInteropConversion));

            var usingDirectives = new List<string>
            {
                "System",
                "System.Runtime.InteropServices",
                "System.Collections.Generic",
                "Coherence.ProtocolDef",
                "Coherence.Serializer",
                "Coherence.SimulationFrame",
                "Coherence.Entities",
                "Coherence.Utils",
                "Coherence.Brook",
                "Coherence.Core",
                "Logger = Coherence.Log.Logger",
            };

            if (codeGenData.NoUnityReferences)
            {
                usingDirectives.Add("System.Numerics");
            }
            else
            {
                usingDirectives.Add("UnityEngine");
                usingDirectives.Add("Coherence.Toolkit");

                if (codeGenData.ExtendedDefinition)
                {
                    usingDirectives.Add("Coherence.Interpolation");
                }
            }

            var scribanOptions = new ScribanOptions
            {
                Namespace = "Coherence.Generated",
                UsingDirectives = usingDirectives,
                TemplateNames = new List<string>
                {
                    "schema_component",
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
