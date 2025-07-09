// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using Coherence.Toolkit;
    using Entities;
    using Portal;
    using Utils;
    using Schema;

#if !UNITY
    using DictionaryOfStringString = Dictionary<string, string>;
#endif

    public static class SchemaReader
    {
        public static SchemaDefinition Read(string schemaText)
        {
            AntlrInputStream antlerStream = new AntlrInputStream(schemaText);
            var lexer = new SchemaLexer(antlerStream);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            SchemaParser parser = new SchemaParser(tokenStream);

            var listener = new SchemaListener();
            ParseTreeWalker.Default.Walk(listener, parser.schema());

            listener.schemaDefinition.SchemaId = HashCalc.SHA1Hash(schemaText);

            return listener.schemaDefinition;
        }
    }

    public class SchemaListener : SchemaBaseListener
    {
        public SchemaDefinition schemaDefinition = new SchemaDefinition();

        public override void EnterArchetype(SchemaParser.ArchetypeContext context)
        {
            ArchetypeDefinition archetype = new ArchetypeDefinition(context.name().GetText(), new List<ArchetypeLOD>());

            int id = 0;
            if (context.@override() != null)
            {
                foreach (var overrContext in context.@override().pair())
                {
                    if (overrContext.name().GetText().Equals("id"))
                    {
                        id = int.Parse(overrContext.TEXT().GetText());
                    }
                }
            }

            archetype.id = id;
            ArchetypeLOD highestLod = null;

            foreach (var lodContext in context.lod())
            {
                var level = lodContext.TEXT().GetText();
                var distance = lodContext.lod_distance() != null
                    ? lodContext.lod_distance().TEXT().GetText()
                    : "0";

                ArchetypeLOD lod = new ArchetypeLOD(int.Parse(level), float.Parse(distance));

                foreach (var componentContext in lodContext.component_override())
                {
                    ArchetypeItem item = new ArchetypeItem(componentContext.name().GetText(),
                        new List<ArchetypeItemField>());

                    int componentOverrideId = 0;
                    int baseComponentId = 0;

                    if (componentContext.@override() != null)
                    {
                        foreach (var overrContext in componentContext.@override().pair())
                        {
                            if (overrContext.name().GetText().Equals("id"))
                            {
                                componentOverrideId = int.Parse(overrContext.TEXT().GetText());
                            }
                            else if (overrContext.name().GetText().Equals("base-id"))
                            {
                                baseComponentId = int.Parse(overrContext.TEXT().GetText());
                            }
                        }
                    }

                    item.id = componentOverrideId;
                    item.baseComponentId = baseComponentId;

                    foreach (var fieldContext in componentContext.field_override())
                    {
                        DictionaryOfStringString overrides = new DictionaryOfStringString();

                        foreach (var overrContext in fieldContext.@override().pair())
                        {
                            overrides.Add(overrContext.name().GetText(), overrContext.TEXT().GetText());
                        }

                        ArchetypeItemField field = new ArchetypeItemField(fieldContext.name().GetText(), overrides);
                        item.fields.Add(field);
                    }

                    lod.items.Add(item);
                }

                FindExcludedComponents(lod, ref highestLod);

                archetype.lods.Add(lod);
            }

            schemaDefinition.ArchetypeDefinitions.Add(archetype);

            base.EnterArchetype(context);
        }

        private void FindExcludedComponents(ArchetypeLOD lod, ref ArchetypeLOD highestLod)
        {
            if (lod.level == 0)
            {
                highestLod = lod;
            }
            else
            {
                foreach (var component in highestLod.items)
                {
                    bool foundInCurrentLod = lod.items.Any(componentInCurrent =>
                        component.componentName.Equals(componentInCurrent.componentName));

                    if (!foundInCurrentLod)
                    {
                        lod.excludedComponentNames.Add(component.componentName);
                    }
                }
            }
        }

        public override void EnterComponent(SchemaParser.ComponentContext context)
        {
            ComponentDefinition component = new ComponentDefinition(context.name().GetText());
            var componentOverrides = new DictionaryOfStringString();

            int id = 0;
            if (context.@override() != null)
            {
                foreach (var overrContext in context.@override().pair())
                {
                    if (overrContext.name().GetText().Equals("id"))
                    {
                        id = int.Parse(overrContext.TEXT().GetText());
                    }
                    else
                    {
                        componentOverrides.Add(overrContext.name().GetText(), overrContext.TEXT().GetText());
                    }
                }
            }

            component.id = id;
            component.overrides = componentOverrides;

            int i = 0;
            int fieldOffset = 0;
            foreach (var field in context.field())
            {
                var bitMask = 1 << i;
                var name = field.name().GetText();
                var type = field.field_type().GetText();
                var cSharpType = TypeUtils.GetCSharpTypeForSchemaType(type);
                var schemaType = TypeUtils.GetSchemaType(cSharpType);
                var offsetSize = TypeUtils.GetFieldOffsetForSchemaType(schemaType);
                var overrides = new DictionaryOfStringString();

                if (field.@override() != null)
                {
                    foreach (var overr in field.@override().pair())
                    {
                        overrides.Add(overr.name().GetText(), overr.TEXT().GetText());
                    }
                }

                var member = new ComponentMemberDescription(
                    NameMangler.MangleSchemaIdentifier(name),
                    NameMangler.MangleCSharpIdentifier(name),
                    type,
                    cSharpType == typeof(Entity) || cSharpType.FullName.Contains("UnityEngine")
                        ? cSharpType.Name
                        : cSharpType.FullName, TypeUtils.GetStringifiedBitMask(bitMask), fieldOffset, overrides);
                component.members.Add(member);

                i++;
                fieldOffset += offsetSize;
            }

            component.bitMasks = TypeUtils.GetStringifiedBitMask((1 << component.members.Count) - 1);
            component.totalSize = fieldOffset;

            schemaDefinition.ComponentDefinitions.Add(component);
            base.EnterComponent(context);
        }

        public override void EnterCommand(SchemaParser.CommandContext context)
        {
            CommandDefinition command = new CommandDefinition(context.name().GetText());

            string routing = "All";
            int id = 0;
            if (context.@override() != null)
            {
                foreach (var overrContext in context.@override().pair())
                {
                    if (overrContext.name().GetText().Equals("routing"))
                    {
                        routing = overrContext.TEXT().GetText();
                    }

                    if (overrContext.name().GetText().Equals("id"))
                    {
                        id = int.Parse(overrContext.TEXT().GetText());
                    }
                }
            }

            command.id = id;
            command.routing = Enum.Parse<MessageTarget>(routing);

            int fieldOffset = 0;
            foreach (var fieldContext in context.field())
            {
                var name = fieldContext.name().GetText();
                var type = fieldContext.field_type().GetText();
                var cSharpType = TypeUtils.GetCSharpTypeForSchemaType(type);
                var schemaType = TypeUtils.GetSchemaType(cSharpType);
                var fieldSize = TypeUtils.GetFieldOffsetForSchemaType(schemaType);
                var overrides = new DictionaryOfStringString();

                if (fieldContext.@override() != null)
                {
                    foreach (var overr in fieldContext.@override().pair())
                    {
                        overrides.Add(overr.name().GetText(), overr.TEXT().GetText());
                    }
                }

                var member = new ComponentMemberDescription(
                    NameMangler.MangleSchemaIdentifier(name),
                    NameMangler.MangleCSharpIdentifier(name),
                    type,
                    cSharpType == typeof(Entity) || cSharpType.FullName.Contains("UnityEngine")
                        ? cSharpType.Name
                        : cSharpType.FullName, string.Empty, fieldOffset, overrides);
                command.members.Add(member);

                fieldOffset += fieldSize;
            }

            command.totalSize = fieldOffset;
            schemaDefinition.CommandDefinitions.Add(command);

            base.EnterCommand(context);
        }

        public override void EnterInput(SchemaParser.InputContext context)
        {
            InputDefinition input = new InputDefinition(context.name().GetText(), new List<ComponentMemberDescription>(), 0);

            int id = 0;
            if (context.@override() != null)
            {
                foreach (var overrContext in context.@override().pair())
                {
                    if (overrContext.name().GetText().Equals("id"))
                    {
                        id = int.Parse(overrContext.TEXT().GetText());
                    }
                }
            }

            input.id = id;

            int fieldOffset = 0;
            foreach (var fieldContext in context.field())
            {
                var name = fieldContext.name().GetText();
                var type = fieldContext.field_type().GetText();
                var cSharpType = TypeUtils.GetCSharpTypeForSchemaType(type);
                var fieldSize = TypeUtils.GetFieldOffsetForSchemaType(Enum.Parse<SchemaType>(type));
                var overrides = new DictionaryOfStringString();

                if (fieldContext.@override() != null)
                {
                    foreach (var overr in fieldContext.@override().pair())
                    {
                        overrides.Add(overr.name().GetText(), overr.TEXT().GetText());
                    }
                }

                var member = new ComponentMemberDescription(
                    NameMangler.MangleSchemaIdentifier(name),
                    NameMangler.MangleCSharpIdentifier(name),
                    type,
                    cSharpType == typeof(Entity) || cSharpType.FullName.Contains("UnityEngine")
                        ? cSharpType.Name
                        : cSharpType.FullName, string.Empty, fieldOffset, overrides);
                input.members.Add(member);

                fieldOffset += fieldSize;
            }

            input.totalSize = fieldOffset;
            schemaDefinition.InputDefinitions.Add(input);

            base.EnterInput(context);
        }
    }
}
