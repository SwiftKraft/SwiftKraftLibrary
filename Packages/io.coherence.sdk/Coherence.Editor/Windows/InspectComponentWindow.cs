// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public class InspectComponentWindow<T> : EditorWindow where T : Component
    {
        protected T component;

        public T Component
        {
            get => component;
            set => component = value;
        }

        protected GameObject context;

        public GameObject Context
        {
            get => context;
            set => context = value;
        }

        protected string guid; // last stored guid for the object being viewed
        protected int instanceId; // last stored instance id for the object being viewed

        protected SerializedObject serializedObject;
        protected SerializedObject serializedGameObject;
        protected SerializedProperty componentsProperty;

        protected PrefabStage stage;

        protected bool inStage;
        protected bool isInstance;
        protected bool isAsset;

        public string searchString;

        protected virtual void UpdateFlags()
        {
            if (!Component)
            {
                return;
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            inStage = stage != null && Component && stage.prefabContentsRoot == Component.gameObject;
            isInstance = PrefabUtility.IsPartOfPrefabInstance(Component) && !inStage;
            isAsset = PrefabUtility.IsPartOfPrefabAsset(Component) || inStage;
        }

        protected virtual void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.modifierKeysChanged += Repaint;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            Undo.undoRedoPerformed += OnUndoRedo;

            Refresh(forceNewSelection: true);
        }

        protected virtual void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.modifierKeysChanged -= Repaint;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (componentsProperty != null)
            {
                componentsProperty.Dispose();
            }

            if (serializedObject != null)
            {
                serializedObject.Dispose();
            }

            if (serializedGameObject != null)
            {
                serializedGameObject.Dispose();
            }
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnUndoRedo()
        {
            Repaint();
        }

        protected void TryReloadFromGUID()
        {
            if (!component)
            {
                // NOTE there is an edge case where deserializing a
                // component whose gameobject contains a missing reference
                // causes the component to be null after OnEnable.
                //
                // it happens e.g. when the MonoScript that's
                // being referenced gets deleted.
                //
                // although the reference renders invalid,
                // subsequent script reloads restore it.
                //
                // here we workaround this storing the object's guid
                // at OnEnable time, and attempt to load it back
                // from the AssetDatabase.

                var s = ReloadFromGUID();
                if (s)
                {
                    component = s;
                    Refresh();
                }
            }
        }

        private T ReloadFromGUID()
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<T>(path) : null;
            }

            return null;
        }

        protected virtual void OnGUI()
        {
            TryReloadFromGUID();
        }

        private protected void OnInspectorUpdate()
        {
            var newStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == newStage)
            {
                return;
            }

            if (stage)
            {
                OnPrefabStageClosing(stage);
            }

            if (newStage)
            {
                OnPrefabStageOpened(newStage);
            }
        }

        protected virtual void OnHierarchyChange()
        {
            Refresh();
            Repaint();
        }

        protected virtual void OnFocus()
        {
            Refresh();
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            bool inStage = StageUtility.GetCurrentStage() != StageUtility.GetMainStage();
            bool isAsset = component && (PrefabUtility.IsPartOfPrefabAsset(component) || inStage);

            if (isAsset)
            {
                return;
            }

            // when switching between play modes while selecting a scene reference,
            // try to persist the reference. Unity honors instance ids between states.

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    instanceId = component ? component.GetInstanceID() : 0;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    if (instanceId != 0)
                    {
                        Selection.activeInstanceID = instanceId;
                        OnSelectionChanged(true);
                    }

                    break;
            }
        }

        private void OnSelectionChanged() => OnSelectionChanged(true);

        private void OnSelectionChanged(bool exitGUI)
        {
            var newContext = Selection.activeGameObject;
            if (!newContext)
            {
                return;
            }

            var previousComponent = component;
            var newComponent = newContext.GetComponentInParent<T>(true);
            if (!newComponent)
            {
                return;
            }

            var changed = newComponent != previousComponent || context != newContext;

            if (newComponent)
            {
                if (changed)
                {
                    OnActiveSelectionChanging(previousComponent, newComponent);
                }

                component = newComponent;
                context = newContext;
            }

            // selection can change in a pre-deserialization state
            if (component)
            {
                Refresh();
                if (changed)
                {
                    OnActiveSelectionChanged(previousComponent, newComponent);
                }
            }

            Repaint();

            if (exitGUI && Event.current != null)
            {
                GUIUtility.ExitGUI();
            }
        }

        protected virtual void OnPrefabStageOpened(PrefabStage newStage)
        {
            stage = newStage;
            if (stage.prefabContentsRoot.TryGetComponent(out component))
            {
                context = component ? component.gameObject : null;
                Refresh();
            }
        }

        protected virtual void OnPrefabStageClosing(PrefabStage oldStage)
        {
            stage = null;
            var s = AssetDatabase.LoadAssetAtPath<T>(oldStage.assetPath);
            component = s;
            context = s ? s.gameObject : null;
            Refresh();
            Repaint();
        }

        public virtual void Refresh(T component)
        {
            this.component = component;
            Refresh();
            Repaint();
        }

        private int GetComponentsPropertyHash()
        {
            try
            {
                unchecked
                {
                    if (serializedObject == null)
                    {
                        return 0;
                    }

                    if (serializedGameObject == null)
                    {
                        return 0;
                    }

                    if (componentsProperty == null)
                    {
                        return 0;
                    }

                    int id = 17;
                    for (int i = 0; i < componentsProperty.arraySize; i++)
                    {
                        using (var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i))
                        using (var componentProperty = baseComponentProperty.FindPropertyRelative("component"))
                        {
                            id = (id * 23) + (componentProperty != null
                                ? componentProperty.objectReferenceInstanceIDValue
                                : 0);
                        }
                    }

                    return id;
                }
            }
            catch
            {
                // we can't guarantee the serialized objects/properties are still valid (i.e. not disposed),
                // so let's safeguard this
                return 0;
            }
        }

        public virtual void Refresh(bool forceNewSelection = false)
        {
            if (forceNewSelection)
            {
                OnSelectionChanged(false);
            }

            if (!component)
            {
                guid = null;

                if (forceNewSelection && Event.current != null)
                {
                    GUIUtility.ExitGUI();
                }
                return;
            }

            int componentHashBefore = GetComponentsPropertyHash();
            if (componentsProperty != null)
            {
                componentsProperty.Dispose();
            }

            if (serializedObject != null)
            {
                serializedObject.Dispose();
            }

            if (serializedGameObject != null)
            {
                serializedGameObject.Dispose();
            }

            serializedObject = new SerializedObject(component);
            guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(component));

            serializedGameObject = new SerializedObject(Context ? Context : component.gameObject);
            componentsProperty = serializedGameObject.FindProperty("m_Component");

            if (componentHashBefore != GetComponentsPropertyHash())
            {
                OnComponentsChanged();
            }
        }

        protected virtual void OnComponentsChanged()
        {
        }

        protected virtual void OnActiveSelectionChanging(T currentComponent, T nextComponent) { }

        protected virtual void OnActiveSelectionChanged(T previousComponent, T newComponent)
        {
        }

        protected void IterateComponents(Action<SerializedProperty> fn)
        {
            IterateComponents(fn, componentsProperty);
        }

        protected void IterateComponents(Action<SerializedProperty> fn, SerializedProperty componentsProperty)
        {
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                using (var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i))
                using (var componentProperty = baseComponentProperty.FindPropertyRelative("component"))
                {
                    fn(componentProperty);
                }
            }
        }

        protected void IterateComponents(Action<SerializedProperty> fn, GameObject gameObject)
        {
            using (var so = new SerializedObject(gameObject))
            using (var p = so.FindProperty("m_Component"))
            {
                IterateComponents(fn, p);
            }
        }

        protected void IterateOnChildren(Action<SerializedProperty> fn, Transform root)
        {
            foreach (Transform t in root)
            {
                IterateComponents(fn, t.gameObject);
                IterateOnChildren(fn, t);
            }
        }

        protected bool IncludedInSearchFilter(string content, bool exactMatch = false)
        {
            return string.IsNullOrEmpty(searchString) ||
                   (exactMatch
                       ? content.Equals(searchString, StringComparison.InvariantCultureIgnoreCase)
                       : content.ToLowerInvariant().Contains(searchString.ToLowerInvariant()));
        }
    }
}
