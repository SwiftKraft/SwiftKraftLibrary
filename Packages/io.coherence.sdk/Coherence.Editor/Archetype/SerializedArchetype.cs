// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Archetypes;

    internal class SerializedArchetype
    {
        internal CoherenceSync Sync { private set;  get; }
        internal ToolkitArchetype Target { private set;  get; }

        internal SerializedObject SerializedSync { private set; get; }
        internal SerializedProperty Archetype { private set; get; }
        internal SerializedProperty LodLevels { private set; get; }
        internal SerializedProperty BoundComponents { private set; get; }

        internal SerializedArchetype(CoherenceSync sync)
        {
            SharedSetup(sync);
            LodLevels = Archetype.FindPropertyRelative("lodLevels");
            BoundComponents = Archetype.FindPropertyRelative("boundComponents");
        }

        private void SharedSetup(CoherenceSync sync) {
            Sync = sync;
            Target = sync.Archetype;
            SerializedSync = new SerializedObject(sync);
            Archetype = SerializedSync.FindProperty("archetype");
        }


        internal void Dispose()
        {
            SerializedSync.Dispose();
            Archetype.Dispose();
            LodLevels.Dispose();
            BoundComponents.Dispose();
        }

        internal SerializedProperty GetDistancePropertyOfLOD(int lodStep)
        {
            return LodLevels.GetArrayElementAtIndex(lodStep).FindPropertyRelative("distance");
        }

        internal SerializedProperty GetBoundComponentLodStep(int boundComponentIndex, int lodStep)
        {
            SerializedProperty boundComponent = BoundComponents.GetArrayElementAtIndex(boundComponentIndex);
            SerializedProperty lodsteps = boundComponent.FindPropertyRelative("lodSteps");
            return lodsteps.GetArrayElementAtIndex(lodStep);
        }

        internal int GetTotalActiveBitsOfLOD(int i)
        {
            if (Target != null)
            {
                return Target.GetTotalActiveBitsOfLOD(i);
            }
            return 0;
        }

        // Helper functions

        internal static bool IsFixedField(SchemaType schemaType)
        {
            return !ArchetypeMath.CanOverride(schemaType);
        }

        internal static bool IsActiveField(SerializedProperty field)
        {
            return field.FindPropertyRelative("activeOnSync").boolValue;
        }

        internal static int GetFieldBits(SerializedProperty field, SchemaType schemaType)
        {
            return field.FindPropertyRelative("_bits").intValue * ArchetypeMath.GetBitsMultiplier(schemaType);
        }

        internal static bool FieldIsActiveAndCanBeOverriden(SerializedProperty field)
        {
            if (!IsActiveField(field))
            {
                return false;
            }

            SchemaType schemaType = (SchemaType)field.FindPropertyRelative("_type").enumValueIndex;
            return !IsFixedField(schemaType);
        }
    }

}
