// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Unit tests for <see cref="CoherenceSyncUtils.HasUndesiredOverrides"/>.
    /// </summary>
    public sealed class CoherenceSyncUtils_UndesiredOverrides_Tests : CoherenceTest
    {
        private const string TestPrefabName = "CoherenceSyncUtils_HasUndesiredOverrides_TestPrefab";
        private const string TestPrefabVariantName = "CoherenceSyncUtils_HasUndesiredOverrides_TestPrefabVariant";
        private const string EmptyPrefabName = "EmptyPrefab";

        [Test]
        public void HasUndesiredOverrides_Returns_False_If_No_Undesired_Properties_Have_Been_Modified()
        {
            var sync = GetPrefabInstance<CoherenceSync>(TestPrefabName);

            var result = CoherenceSyncUtils.HasUndesiredOverrides(sync);

            Assert.That(result, Is.False, $"{nameof(CoherenceSyncUtils.HasUndesiredOverrides)} returned True even though no properties were modified in the prefab instance. It has the following property modifications:\n{GetPropertyModifications(sync)}");
        }

        [Test]
        public void HasUndesiredOverrides_Returns_True_If_Any_Undesired_Property_Has_Been_Modified()
        {
            var prefab = GetPrefab<CoherenceSync>(TestPrefabName);

            foreach (var propertyPath in CoherenceSyncUtils.UndesiredOverrides)
            {
                var sync = GetPrefabInstance(prefab);
                if(TryClearPropertyValue(sync, propertyPath))
                {
                    var result = CoherenceSyncUtils.HasUndesiredOverrides(sync);
                    Assert.That(result, Is.True, $"{nameof(CoherenceSyncUtils.HasUndesiredOverrides)} returned False after value of '{nameof(CoherenceSync)}.{propertyPath}' was cleared. It has the following property modifications:\n{GetPropertyModifications(sync)}");
                }
            }
        }

        [Test]
        public void HasUndesiredOverrides_Returns_False_If_Prefab_Variant_In_Prefab_Mode_Has_Modifications()
        {
            var prefab = GetPrefab<CoherenceSync>(TestPrefabVariantName);
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var prefabStage = PrefabStageUtility.OpenPrefab(prefabPath);
            var sync = prefabStage.prefabContentsRoot.GetComponent<CoherenceSync>();

            foreach (var propertyPath in CoherenceSyncUtils.UndesiredOverrides)
            {
                TryClearPropertyValue(sync, propertyPath);
            }

            var result = CoherenceSyncUtils.HasUndesiredOverrides(sync);

            Assert.That(result, Is.False, $"{nameof(CoherenceSyncUtils.HasUndesiredOverrides)} returned True for a Prefab Variant Open In Prefab Mode. It has the following property modifications:\n{GetPropertyModifications(sync)}");

            StageUtility.GoBackToPreviousStage();
        }

        [Test]
        public void HasUndesiredOverrides_Returns_False_For_CoherenceSync_Attached_To_An_Empty_Prefab()
        {
            var instance = GetPrefabInstance<GameObject>(EmptyPrefabName);
            var sync = instance.AddComponent<CoherenceSync>();

            var result = CoherenceSyncUtils.HasUndesiredOverrides(sync);

            Assert.That(result, Is.False, $"{nameof(CoherenceSyncUtils.HasUndesiredOverrides)} returned True for a CoherenceSync component that was just attached to an empty prefab instance. It has the following property modifications:\n{GetPropertyModifications(sync)}");
        }

        [Test]
        public void RemoveUndesiredOverrides_Returns_Zero_For_CoherenceSync_Attached_To_An_Empty_Prefab()
        {
            var instance = GetPrefabInstance<GameObject>(EmptyPrefabName);
            var sync = instance.AddComponent<CoherenceSync>();

            var result = CoherenceSyncUtils.RemoveUndesiredOverrides(sync);

            Assert.That(result, Is.Zero, $"{nameof(CoherenceSyncUtils.RemoveUndesiredOverrides)} returned {result} for a CoherenceSync component that was just attached to an empty prefab instance. It has the following property modifications:\n{GetPropertyModifications(sync)}");
        }

        private static T GetPrefab<T>(string resourcePath) where T : Object
        {
            var prefab = Resources.Load<T>(resourcePath);
            if (!prefab)
            {
                if(typeof(T) != typeof(GameObject))
                {
                    Assert.Ignore($"No prefab containing a {typeof(T)} component was found at 'Resources/{resourcePath}.prefab'.");
                }
                else
                {
                    Assert.Ignore($"No prefab was found at 'Resources/{resourcePath}.prefab'.");
                }
            }

            return prefab;
        }

        private static T GetPrefabInstance<T>(string resourcePath) where T : Object => (T)PrefabUtility.InstantiatePrefab(GetPrefab<T>(resourcePath));
        private static CoherenceSync GetPrefabInstance(CoherenceSync prefab) => (CoherenceSync)PrefabUtility.InstantiatePrefab(prefab);

        private static bool TryClearPropertyValue(CoherenceSync sync, string propertyPath)
        {
            using var serializedObject = new SerializedObject(sync);
            if(serializedObject.FindProperty(propertyPath) is not { } property)
            {
                return false;
            }

            if (property.isArray)
            {
                property.ClearArray();
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }

            if (property.propertyType is SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = null;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }

            var field = typeof(CoherenceSync).GetField(propertyPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is null)
            {
                return false;
            }

            var defaultValue = field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
            field.SetValue(sync, defaultValue);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sync);
            return true;
        }

        private static string GetPropertyModifications(CoherenceSync sync)
            => string.Join("\n", PrefabUtility.GetPropertyModifications(sync) is { } modifications
            ? modifications.Select(m => m.propertyPath)
            : Array.Empty<string>());
    }
}
