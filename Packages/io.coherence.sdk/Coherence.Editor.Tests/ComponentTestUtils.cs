namespace Coherence.Editor.Tests
{
    using Coherence.Toolkit;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public delegate CoherenceSync AddComponentDelegate(GameObject gameObject);

    public delegate void DestroyImmediateDelegate(Object obj);

    public static class ComponentTestUtils
    {
        private static CoherenceSync AddComponent(GameObject gameObject)
        {
            return gameObject.AddComponent<CoherenceSync>();
        }

        private static void DestroyImmediate(Object obj)
        {
            Object.DestroyImmediate(obj, true);
        }

        private static CoherenceSync UndoAddComponent(GameObject gameObject)
        {
            return Undo.AddComponent<CoherenceSync>(gameObject);
        }

        private static void UndoDestroyImmediate(Object obj)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        public static readonly TestCaseData[] TestCaseData =
        {
            new TestCaseData(
                    new AddComponentDelegate(AddComponent),
                    new DestroyImmediateDelegate(DestroyImmediate))
                .SetName("Runtime API"),
            new TestCaseData(
                    new AddComponentDelegate(UndoAddComponent),
                    new DestroyImmediateDelegate(UndoDestroyImmediate))
                .SetName("Undo API"),
        };
    }
}
