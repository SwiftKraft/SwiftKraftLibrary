namespace Coherence.Editor.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Coherence.Toolkit.Bindings.TransformBindings;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    [TestFixture(typeof(Transform))]
    [TestFixture(typeof(BoxCollider))]
    [TestFixture(typeof(AudioSource))]
    [TestFixture(typeof(CoherenceNode))]
    [TestFixture(typeof(CoherenceSync))]
    [TestFixture(typeof(Animator))]
    public class CoherenceSyncUtilsTests<T> : CoherenceTest where T : Component
    {
        private static readonly MemberInfo[] members;
        private static readonly IEnumerable<MemberInfo> bindableMembers;
        private static readonly IEnumerable<MemberInfo> nonBindableMembers;

        private static readonly IEnumerable<string> bindableMemberNames;
        private static readonly IEnumerable<string> nonBindableMemberNames;

        private static int onBeforeBindingAddedInvokeCount;
        private static int onBeforeBindingRemovedInvokeCount;

        private GameObject gameObject;
        private CoherenceSync sync;
        private T component;
        private List<Descriptor> descriptors;
        private IEnumerable<Descriptor> requiredDescriptors;
        private IEnumerable<Descriptor> availableDescriptors;
        private IEnumerable<Descriptor> nonAvailableDescriptors;

        static CoherenceSyncUtilsTests()
        {
            members = typeof(T).GetMembers();
            bindableMembers = members.Where(m => m.IsValidBinding()).ToList();
            nonBindableMembers = members.Except(bindableMembers);

            bindableMemberNames = bindableMembers.Select(m => m.Name).Distinct();
            nonBindableMemberNames = nonBindableMembers.Select(m => m.Name).Distinct();
        }

        private void UpdateDescriptorCollections()
        {
            descriptors = EditorCache.GetComponentDescriptors(component);
            requiredDescriptors = descriptors.Where(d => d.Required);
            availableDescriptors = descriptors.Where(d => bindableMemberNames.Contains(d.Name));
            nonAvailableDescriptors = descriptors.Except(availableDescriptors);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            onBeforeBindingAddedInvokeCount = 0;
            onBeforeBindingRemovedInvokeCount = 0;
            CoherenceSyncUtils.OnBeforeBindingAdded += OnBeforeBindingAdded;
            CoherenceSyncUtils.OnBeforeBindingRemoved += OnBeforeBindingRemoved;

            gameObject = new GameObject();
            sync = gameObject.AddComponent<CoherenceSync>();
            if (!gameObject.TryGetComponent(out component))
            {
                component = gameObject.AddComponent<T>();
            }

            UpdateDescriptorCollections();
        }

        [TearDown]
        public override void TearDown()
        {
            CoherenceSyncUtils.OnBeforeBindingAdded -= OnBeforeBindingAdded;
            CoherenceSyncUtils.OnBeforeBindingRemoved -= OnBeforeBindingRemoved;

            Object.DestroyImmediate(gameObject, true);

            base.TearDown();
        }

        private static void OnBeforeBindingAdded(CoherenceSync sync, Binding binding)
        {
            onBeforeBindingAddedInvokeCount++;
        }

        private void OnBeforeBindingRemoved(CoherenceSync arg1, Binding arg2)
        {
            onBeforeBindingRemovedInvokeCount++;
        }

        [Test, Description("UpdateBindings with forceUpdate set to true should add all missing required bindings to non-prefab CoherenceSync.")]
        public void UpdateBindings_CreatesRequiredBindings()
        {
            var requiredCount = requiredDescriptors.Count(d => d.OwnerType == typeof(T));
            if (requiredCount == 0)
            {
                Assert.Ignore("Component doesn't define any required bindings.");
            }

            sync.Bindings.Clear();
            CoherenceSyncUtils.UpdateBindings(sync, forceUpdate:true);
            Assert.AreEqual(requiredCount, sync.Bindings.Count(b => b.unityComponent == component),
                string.Join(",", sync.Bindings.Select(b => b.Name)));
            Assert.IsTrue(EditorUtility.IsDirty(sync));
        }

        [Test, Description("UpdateBindings should not remove any bindings from a CoherenceSync that is the child of another CoherenceSync in a prefab asset.")]
        public void UpdateBindings_DoesNotRemoveBindingsFromNestedCoherenceSyncObject()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var prefab = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!prefab)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }
            var nestedSync = prefab.transform.GetChild(0).GetComponent<CoherenceSync>();
            var bindingsBeforeUpdate = nestedSync.Bindings.ToArray();

            var bindingsWereChanged = CoherenceSyncUtils.UpdateBindings(nestedSync, forceUpdate:false);

            Assert.That(bindingsWereChanged, Is.False);
            var bindingsAfterUpdate = nestedSync.Bindings;
            Assert.That(bindingsAfterUpdate, Is.EquivalentTo(bindingsBeforeUpdate));
            Assert.That(bindingsAfterUpdate.FirstOrDefault(), Is.TypeOf<PositionBinding>());
        }
               
        [Test, Description("Invalid bindings whose Descriptor is Required should be removed automatically as part of the UpdateBindings process.")]
        public void UpdateBindings_RemovesInvalidBindings_With_RequiredDescriptors()
        {
            if (component is Transform)
            {
                Assert.Ignore("Transform cannot be removed.");
            }

            var requiredCount = requiredDescriptors.Count(d => d.OwnerType == typeof(T));
            if (requiredCount == 0)
            {
                Assert.Ignore("Component doesn't define any required bindings.");
            }
            
            sync.Bindings.Clear();
            CoherenceSyncUtils.UpdateBindings(sync, forceUpdate:true);
            
            // We don't assert that the required bindings were added since that is covered by UpdateBindings_CreatesRequiredBindings
            Object.DestroyImmediate(component);
            var bindingsWereChanged = CoherenceSyncUtils.UpdateBindings(sync, forceUpdate:true);
            Assert.IsTrue(bindingsWereChanged);
            var count = sync.Bindings.Count(b => b.unityComponent == component);
            Assert.AreEqual(0, count, "Required bindings were not removed.");
            Assert.IsTrue(EditorUtility.IsDirty(sync));
        }

        [Test, Description("IsEditingBindingsAllowed(sync, InteractionMode.UserAction) should return True when CloneMode.Enabled is False, even if CloneMode.AllowEdits is false.")]
        public void IsEditingBindingsAllowed_With_InteractionMode_UserAction_Returns_True_When_CloneMode_Is_Disabled()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var sync = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!sync)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }

            var wasEnabled = CloneMode.Enabled;
            var editsWereAllowed = CloneMode.AllowEdits;
            bool isEditingBindingsAllowed;
            CloneMode.Enabled = false;
            CloneMode.AllowEdits = false;

            try
            {
                isEditingBindingsAllowed = CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.UserAction);
            }
            finally
            {
                CloneMode.Enabled = wasEnabled;
                CloneMode.AllowEdits = editsWereAllowed;
            }

            Assert.That(isEditingBindingsAllowed, Is.True);
        }

        [Test, Description("IsEditingBindingsAllowed(sync, InteractionMode.AutomatedAction) should return True when CloneMode.Enabled is False, even if CloneMode.AllowEdits is False.")]
        public void IsEditingBindingsAllowed_With_InteractionMode_AutomatedAction_Returns_False_When_CloneMode_Is_Disabled()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var sync = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!sync)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }

            var wasEnabled = CloneMode.Enabled;
            var editsWereAllowed = CloneMode.AllowEdits;
            bool isEditingBindingsAllowed;
            CloneMode.Enabled = false;
            CloneMode.AllowEdits = false;

            try
            {
                isEditingBindingsAllowed = CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.AutomatedAction);
            }
            finally
            {
                CloneMode.Enabled = wasEnabled;
                CloneMode.AllowEdits = editsWereAllowed;
            }

            Assert.That(isEditingBindingsAllowed, Is.True);
        }

        [Test, Description("IsEditingBindingsAllowed(sync, InteractionMode.UserAction) should return True when CloneMode.Enabled is True and CloneMode.AllowEdits is True.")]
        public void IsEditingBindingsAllowed_With_InteractionMode_UserAction_Returns_True_When_CloneMode_Is_Enabled_And_AllowEdits_Is_Enabled()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var sync = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!sync)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }

            var wasEnabled = CloneMode.Enabled;
            var editsWereAllowed = CloneMode.AllowEdits;
            bool isEditingBindingsAllowed;
            CloneMode.Enabled = true;
            CloneMode.AllowEdits = true;

            try
            {
                isEditingBindingsAllowed = CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.UserAction);
            }
            finally
            {
                CloneMode.Enabled = wasEnabled;
                CloneMode.AllowEdits = editsWereAllowed;
            }

            Assert.That(isEditingBindingsAllowed, Is.True);
        }

        [Test, Description("IsEditingBindingsAllowed(sync, InteractionMode.UserAction) should return False when CloneMode.Enabled is True and CloneMode.AllowEdits is False.")]
        public void IsEditingBindingsAllowed_With_InteractionMode_UserAction_Returns_False_When_CloneMode_Is_Enabled_And_AllowEdits_Is_Disabled()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var sync = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!sync)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }

            var wasEnabled = CloneMode.Enabled;
            var editsWereAllowed = CloneMode.AllowEdits;
            bool isEditingBindingsAllowed;
            CloneMode.Enabled = true;
            CloneMode.AllowEdits = false;

            try
            {
                isEditingBindingsAllowed = CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.UserAction);
            }
            finally
            {
                CloneMode.Enabled = wasEnabled;
                CloneMode.AllowEdits = editsWereAllowed;
            }

            Assert.That(isEditingBindingsAllowed, Is.False);
        }

        [Test, Description("IsEditingBindingsAllowed(sync, InteractionMode.AutomatedAction) should return False when CloneMode.Enabled is false, even if CloneMode.AllowEdits is True.")]
        public void IsEditingBindingsAllowed_With_InteractionMode_AutomatedAction_Returns_False_When_CloneMode_Is_Enabled()
        {
            const string prefabResourcePath = "CoherenceSync/Parent";
            var sync = Resources.Load<CoherenceSync>(prefabResourcePath);
            if(!sync)
            {
                Assert.Ignore($"Test disabled because failed to find CoherenceSync test prefab at '{prefabResourcePath}'.");
            }

            var wasEnabled = CloneMode.Enabled;
            var editsWereAllowed = CloneMode.AllowEdits;
            bool isEditingBindingsAllowed;
            CloneMode.Enabled = true;
            CloneMode.AllowEdits = true;

            try
            {
                isEditingBindingsAllowed = CoherenceSyncUtils.IsEditingBindingsAllowed(sync, InteractionMode.AutomatedAction);
            }
            finally
            {
                CloneMode.Enabled = wasEnabled;
                CloneMode.AllowEdits = editsWereAllowed;
            }

            Assert.That(isEditingBindingsAllowed, Is.False);
        }

        [Test, Description(
        "AddBinding<T> throws NonBindableException if member type is non-bindable; otherwise, adds a valid binding and sets the CoherenceSync dirty.\n" +
        "RemoveBinding<T> throws DescriptorRequiredException for required bindings; otherwise, removes the binding.")]
        public void AddAndRemoveBindings_ThroughGenericType()
        {
            CoherenceSyncUtils.UpdateBindings(sync, forceUpdate:true);

            foreach (var descriptor in availableDescriptors)
            {
                var memberName = descriptor.Name;
                var count = sync.Bindings.Count;
                if (TypeUtils.IsNonBindableType(typeof(T)))
                {
                    Assert.Throws<NonBindableException>(
                        () => CoherenceSyncUtils.AddBinding<T>(gameObject, memberName));
                    Assert.AreEqual(sync.Bindings.Count, count);

                    Assert.Throws<NonBindableException>(
                        () => CoherenceSyncUtils.RemoveBinding<T>(gameObject, memberName));
                    Assert.AreEqual(sync.Bindings.Count, count);
                }
                else
                {
                    var binding = CoherenceSyncUtils.AddBinding<T>(gameObject, memberName);
                    Assert.IsNotNull(binding, memberName);
                    Assert.IsTrue(binding.IsValid);
                    var checkValid = binding.IsBindingValid();
                    Assert.IsTrue(checkValid.IsValid, checkValid.Reason);
                    Assert.IsTrue(sync.Bindings.Contains(binding));
                    Assert.IsTrue(EditorUtility.IsDirty(sync));

                    CoherenceSyncUtils.AddBinding<T>(gameObject, memberName);
                    Assert.LessOrEqual(sync.Bindings.Count, count + 1);

                    if (requiredDescriptors.Contains(descriptor))
                    {
                        Assert.Throws<DescriptorRequiredException>(
                            () => CoherenceSyncUtils.RemoveBinding<T>(gameObject, memberName));
                    }
                    else
                    {
                        CoherenceSyncUtils.RemoveBinding<T>(gameObject, memberName);
                        Assert.GreaterOrEqual(sync.Bindings.Count, count - 1);
                    }
                }
            }

        }

        [Test, Description("AddBinding<T> throws DescriptorNotFoundException if no member by the given name exists on the component of type T attached to the GameObject.")]
        public void AddBinding_OnNonExistingMember()
        {
            // Using a member name that doesn't exist
            Assert.Throws<DescriptorNotFoundException>(
                () => CoherenceSyncUtils.AddBinding<T>(gameObject, "0"));
        }

        [Test, Description("AddBinding<T> throws DescriptorNotFoundException if member type is non-bindable.")]
        public void AddBinding_OnExistingMemberButNotBindable_Throws()
        {
            // Members existing on the type, that coherence doesn't support

            if (!nonBindableMemberNames.Any())
            {
                Assert.Ignore("All members of the given type can be bound to.");
            }

            foreach (var memberName in nonBindableMemberNames)
            {
                Assert.Throws<DescriptorNotFoundException>(
                    () => CoherenceSyncUtils.AddBinding<T>(gameObject, memberName), memberName);
            }
        }

        [Test, Description("AddBinding<T> throws DescriptorNotFoundException if member type is bindable, but DescriptorProvider has a custom filter excluding the member from the descriptors lists.")]
        public void AddBinding_OnFilteredOutDescriptor_Throws()
        {
            // Members existing on the type, that are bindable, but that the DescriptorProvider decides to not expose

            if (!nonAvailableDescriptors.Any())
            {
                Assert.Ignore("The DescriptorProvider associated with given component exposes all bindable members.");
            }

            foreach (var descriptor in nonAvailableDescriptors)
            {
                var memberName = descriptor.Name;
                Assert.Throws<DescriptorNotFoundException>(
                    () => CoherenceSyncUtils.AddBinding<T>(gameObject, memberName), memberName);
            }
        }

        [Test, Description(
        "The OnBeforeBindingAdded event is raised once, if AddBinding<T> succeeds in adding a binding; otherwise, the event is not raised at all.\n" +
        "The onBeforeBindingRemovedInvokeCount event is raised once, if RemoveBinding<T> succeeds in removing a binding; otherwise, the event is not raised at all.")]
        public void AddBinding_RaisesEvents()
        {
            if (TypeUtils.IsNonBindableType(typeof(T)))
            {
                Assert.Ignore("Component doesn't allow bindings.");
            }

            var descriptorCount = availableDescriptors.Count();
            if (descriptorCount == 0)
            {
                Assert.Ignore("Component doesn't expose any valid descriptor.");
            }

            var descriptor = descriptors.FirstOrDefault(descriptor => !descriptor.Required);
            if (descriptor == default)
            {
                Assert.Ignore("Component doesn't expose non-required descriptors.");
            }

            var memberName = descriptor.Name;

            CoherenceSyncUtils.AddBinding<T>(gameObject, memberName);
            Assert.AreEqual(1, onBeforeBindingAddedInvokeCount);

            CoherenceSyncUtils.AddBinding<T>(gameObject, memberName);
            Assert.AreEqual(onBeforeBindingAddedInvokeCount, 1, "Adding an existing binding triggered OnBeforeBindingAdded.");

            CoherenceSyncUtils.RemoveBinding<T>(gameObject, memberName);
            Assert.AreEqual(1, onBeforeBindingRemovedInvokeCount);

            CoherenceSyncUtils.RemoveBinding<T>(gameObject, memberName);
            Assert.AreEqual(1, onBeforeBindingRemovedInvokeCount, "Removing a non-existing binding triggered OnBeforeBindingRemoved.");
        }

        [Test, Description("Removing a binding whose component is missing should not fail.")]
        public void RemoveInvalidBinding_DoesNotThrow()
        {
            if (component is Transform)
            {
                Assert.Ignore("Transform cannot be removed.");
            }

            if (TypeUtils.IsNonBindableType(typeof(T)))
            {
                Assert.Ignore("Component doesn't allow bindings.");
            }

            var descriptorCount = availableDescriptors.Count();
            if (descriptorCount == 0)
            {
                Assert.Ignore("Component doesn't expose any valid descriptor.");
            }

            var descriptor = descriptors.FirstOrDefault(descriptor => !descriptor.Required);
            if (descriptor == default)
            {
                Assert.Ignore("Component doesn't expose non-required descriptors.");
            }

            var memberName = descriptor.Name;
            CoherenceSyncUtils.AddBinding<T>(gameObject, memberName);

            Object.DestroyImmediate(component);

            CoherenceSyncUtils.RemoveBinding(gameObject, typeof(T), memberName);
        }
    }
}
