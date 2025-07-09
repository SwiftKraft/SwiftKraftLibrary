// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit.BindingProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Editor;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    [DescriptorProvider(typeof(Animator), -1)]
    internal class AnimatorDescriptorProvider : DescriptorProvider
    {
        private static readonly string[] methodsFilter = new string[]
        {
            nameof(Animator.SetTrigger),
            nameof(Animator.ResetTrigger),
            nameof(Animator.Play),
            nameof(Animator.Stop),
            nameof(Animator.PlayInFixedTime),
        };

        private List<Descriptor> cachedDescriptors = new List<Descriptor>();
        private const float CacheInvalidationTimeInSeconds = 1f;
        private double nextInvalidation = 0;

        public override List<Descriptor> Fetch()
        {
            if (!CacheHasExpired())
            {
                return cachedDescriptors;
            }

            cachedDescriptors.AddRange(GetAnimatorControllerDescriptors()); // AnimatorParameters
            cachedDescriptors.AddRange(base.Fetch()); // Built-in fields, properties and methods

            return cachedDescriptors;
        }

        private bool CacheHasExpired()
        {
            if (EditorApplication.timeSinceStartup > nextInvalidation)
            {
                cachedDescriptors.Clear();
                nextInvalidation = EditorApplication.timeSinceStartup + CacheInvalidationTimeInSeconds;

                return true;
            }

            return false;
        }

        protected override bool CustomMethodFilter(MethodInfo methodInfo)
        {
            return methodsFilter.Contains(methodInfo.Name);
        }

        private Descriptor CreateAnimationParameterDescriptor(AnimatorControllerParameter parameter)
        {
            return parameter.type switch
            {
                AnimatorControllerParameterType.Int => new AnimatorDescriptor(typeof(IntAnimatorParameterBinding), parameter),
                AnimatorControllerParameterType.Float => new AnimatorDescriptor(typeof(FloatAnimatorParameterBinding), parameter),
                AnimatorControllerParameterType.Bool => new AnimatorDescriptor(typeof(BoolAnimatorParameterBinding), parameter),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private AnimatorControllerParameter[] GetAnimatorControllerParameters()
        {
            var animator = Component as Animator;

            using var animatorSerializedObject = new SerializedObject(animator);
            using var animatorControllerProperty = animatorSerializedObject.FindProperty("m_Controller");

            if (animatorControllerProperty.objectReferenceValue is AnimatorController animController)
            {
                return animController.parameters;
            }

            return animatorControllerProperty.objectReferenceValue is AnimatorOverrideController
            {
                runtimeAnimatorController: AnimatorController animControllerFromOverride
            } ? animControllerFromOverride.parameters : Array.Empty<AnimatorControllerParameter>();
        }

        public override GUIContent GetIconContent(Descriptor descriptor)
        {
            if (descriptor is AnimatorDescriptor)
            {
                return EditorGUIUtility.TrIconContent("AnimatorStateMachine Icon");
            }
            else
            {
                return GUIContent.none;
            }
        }

        public override MenuItemData[] AdditionalMenuItemData => new MenuItemData[]
        {
            new MenuItemData
            {
                content = Icons.GetContent("Coherence.Select.AnimParams.Off", "Select All Animation Parameters"),
                contentHover = Icons.GetContent("Coherence.Select.AnimParams.On", "Select All Animation Parameters"),
                function = SelectAllParameters,
            },
        };

        private void SelectAllParameters(object userData)
        {
            var context = userData as MenuItemContext;
            var sync = context.sync;
            var component = context.component;
            if (!component)
            {
                return;
            }

            var addedAnyBinding = false;
            var descriptors = GetAnimatorControllerDescriptors();

            foreach (var descriptor in descriptors.Where(descriptor => IncludedInSearchFilter(context.searchString, descriptor.Name)))
            {
                if (sync.HasBindingForDescriptor(descriptor, component))
                {
                    continue;
                }

                if (!addedAnyBinding)
                {
                    Undo.RecordObject(sync, $"Select All {ObjectNames.GetInspectorTitle(component)} Bindings (Parameters)");

                    addedAnyBinding = true;
                }

                sync.Bindings.Add(descriptor.InstantiateBinding(component));
            }

            if (addedAnyBinding)
            {
                EditorUtility.SetDirty(sync);
                Undo.FlushUndoRecordObjects();
            }
        }

        private static bool IncludedInSearchFilter(string searchString, string content)
        {
            return string.IsNullOrEmpty(searchString) || content.ToLowerInvariant().Contains(searchString.ToLowerInvariant());
        }

        private IEnumerable<Descriptor> GetAnimatorControllerDescriptors()
        {
            return GetAnimatorControllerParameters().
                Where(parameter => parameter.type != AnimatorControllerParameterType.Trigger).
                Select(CreateAnimationParameterDescriptor);
        }
    }
}
