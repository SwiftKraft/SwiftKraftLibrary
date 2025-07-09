namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Coherence.Tests;

    public class CoherenceNodeTests : CoherenceTest
    {
        private class ObjectMock
        {
            public GameObject gameObject;
            public Transform transform;
            public CoherenceNode node;
            public Mock<IConnectedEntityDriver> connectedEntityDriverMock;
            public Mock<Log.Logger> loggerMock;

            public ObjectMock(bool hasAuthority)
            {
                connectedEntityDriverMock = new Mock<IConnectedEntityDriver>();
                connectedEntityDriverMock.Setup(m => m.HasStateAuthority).Returns(hasAuthority);
                connectedEntityDriverMock.Setup(m => m.SetParent(It.IsAny<Transform>())).Callback<Transform>(p => transform.parent = p);

                loggerMock = new Mock<Log.Logger>(null, null, null);

                gameObject = new GameObject();
                transform = gameObject.transform;
                node = gameObject.AddComponent<CoherenceNode>();

                node.sync = connectedEntityDriverMock.Object;
                node.logger = loggerMock.Object;

                node.CallAwake();
            }
        }

        private ObjectMock authority;
        private ObjectMock networked;

        private GameObject parent1;
        private CoherenceSync sync1;
        private GameObject networkedParent1;
        private CoherenceSync networkedSync1;

        private GameObject parent2;
        private CoherenceSync sync2;
        private GameObject networkedParent2;
        private CoherenceSync networkedSync2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            authority = new ObjectMock(true);
            networked = new ObjectMock(false);

            parent1 = new GameObject();
            sync1 = parent1.AddComponent<CoherenceSync>();

            networkedParent1 = new GameObject();
            networkedSync1 = networkedParent1.AddComponent<CoherenceSync>();

            parent2 = new GameObject();
            sync2 = parent2.AddComponent<CoherenceSync>();

            networkedParent2 = new GameObject();
            networkedSync2 = networkedParent2.AddComponent<CoherenceSync>();
        }

        /// <summary>
        /// Parents authority game object to <paramref name="parent"/> and triggers path calculation.
        /// </summary>
        /// <param name="connectedEntity">First CohrenceSync ancestor of <paramref name="parent"/>.</param>
        /// <param name="parent">Real parent and descendant of <paramref name="connectedEntity"/>.</param>
        private void ParentAuthority(CoherenceSync connectedEntity, Transform parent)
        {
            if ((parent == null && connectedEntity != null) ||
                (connectedEntity == null && parent != null))
            {
                throw new Exception("ConnectedEntity and parent both must be null or not null!");
            }

            if (parent != null && parent != connectedEntity.transform && !parent.IsChildOf(connectedEntity.transform))
            {
                throw new Exception("Transform parent must be a child of connected entity!");
            }

            authority.transform.SetParent(parent);
            authority.connectedEntityDriverMock.Setup(m => m.ConnectedEntity).Returns(connectedEntity);
            authority.connectedEntityDriverMock.Raise(m => m.DidSendConnectedEntity += null, connectedEntity);

        }

        /// <summary>
        /// Copies path and pathDirtyCounter from authority to networked game object. Forces networked game object
        /// to update itself in the hierarchy.
        /// </summary>
        /// <param name="connectLate">If true, calls Start() instead of <see cref="IConnectedEntityDriver.ConnectedEntityChangeOverride>"/></param>
        /// <param name="parent">ConnectedEntity</param>
        private void UpdateNetworked(bool connectLate, CoherenceSync parent)
        {
            networked.connectedEntityDriverMock.Setup(m => m.ConnectedEntity).Returns(parent);
            networked.node.path = authority.node.path;
            networked.node.pathDirtyCounter = authority.node.pathDirtyCounter;

            if (!connectLate)
            {
                networked.connectedEntityDriverMock.Raise(m => m.ConnectedEntityChangeOverride += null, parent);
            }
            else
            {
                networked.node.CallStart();
            }
            networked.node.UpdateHierarchy();
        }

        private GameObject CreateChild(GameObject parent)
        {
            var gameObject = new GameObject();
            gameObject.transform.SetParent(parent.transform);
            return gameObject;
        }

        private Dictionary<string, GameObject> CreateComplexHierarchy(GameObject root)
        {
            var dictionary = new Dictionary<string, GameObject>();
            dictionary[""] = root;
            dictionary["1"] = CreateChild(root);
            dictionary["2"] = CreateChild(root);
            dictionary["3"] = CreateChild(root);
            dictionary["21"] = CreateChild(dictionary["2"]);
            dictionary["22"] = CreateChild(dictionary["2"]);
            dictionary["211"] = CreateChild(dictionary["21"]);
            dictionary["212"] = CreateChild(dictionary["21"]);

            return dictionary;
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Node_DirectParent(bool hasParent, bool connectLate)
        {
            // Tests if parenting directly to a coherenceSync works.
            // Also tests when the parent is null.

            // Arrange
            var parent = hasParent ? sync1 : null;
            var networkedParent = hasParent ? networkedSync1 : null;

            // Act
            if (!connectLate)
            {
                networked.node.CallStart();
            }

            ParentAuthority(parent, parent?.transform);
            UpdateNetworked(connectLate, networkedParent);

            // Assert
            Assert.AreEqual(networkedParent?.transform, networked.transform.parent);
        }

        // [TestCase(false, false, false)] // doesn't have much value to reparent from null to null
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        // [TestCase(false, false, true)] // doesn't have much value to reparent from null to null
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void Node_ReparentDirect(bool hasParent1, bool hasParent2, bool connectLate)
        {
            // Tests if reparenting from directly one coherenceSync to another works.
            // Also tests when first or second parents are null.

            // Arrange
            var parent1 = hasParent1 ? sync1 : null;
            var parent2 = hasParent2 ? sync2 : null;
            var networkedParent1 = hasParent1 ? networkedSync1 : null;
            var networkedParent2 = hasParent2 ? networkedSync2 : null;

            // Act
            if (!connectLate)
            {
                networked.node.CallStart();
            }

            // Act - Parent1
            ParentAuthority(parent1, parent1?.transform);
            UpdateNetworked(connectLate, networkedParent1);

            // Assert - Parent1
            Assert.AreEqual(networkedParent1?.transform, networked.transform.parent);

            // Act - Parent2
            ParentAuthority(parent2, parent2?.transform);
            UpdateNetworked(false, networkedParent2);

            // Assert - Parent2
            Assert.AreEqual(networkedParent2?.transform, networked.transform.parent);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Node_ComplexParent(bool connectLate)
        {
            // Tests if parenting in complex hierarchy works.

            // Arrange
            var hierarchy = CreateComplexHierarchy(parent1);
            var networkedHierarchy = CreateComplexHierarchy(networkedParent1);
            var path = "212";

            // Act
            if (!connectLate)
            {
                networked.node.CallStart();
            }

            ParentAuthority(sync1, hierarchy[path].transform);
            UpdateNetworked(connectLate, networkedSync1);

            // Assert
            Assert.AreEqual(networkedHierarchy[path].transform, networked.transform.parent);
        }

        [TestCase("212", "21", true)]
        [TestCase("212", "21", false)]
        [TestCase("", "21", true)]
        [TestCase("", "21", false)]
        [TestCase("1", "", true)]
        [TestCase("1", "", false)]
        public void Node_ComplexParent_ReparentSameRoot(string path1, string path2, bool connectLate)
        {
            // Tests if reparenting inside of complex hierarchy of the same root coherenceSync works.

            // Arrange
            var hierarchy = CreateComplexHierarchy(parent1);
            var networkedHierarchy = CreateComplexHierarchy(networkedParent1);

            // Act
            if (!connectLate)
            {
                networked.node.CallStart();
            }

            // Act - Parent 212
            ParentAuthority(sync1, hierarchy[path1].transform);
            UpdateNetworked(connectLate, networkedSync1);

            // Assert - Parent 212
            Assert.AreEqual(networkedHierarchy[path1].transform, networked.transform.parent);

            // Act - Parent 21
            ParentAuthority(sync1, hierarchy[path2].transform);
            UpdateNetworked(false, networkedSync1);

            // Assert - Parent 21
            Assert.AreEqual(networkedHierarchy[path2].transform, networked.transform.parent);
        }

        [TestCase("212", "212", true)]
        [TestCase("212", "212", false)]
        [TestCase("212", "22", true)]
        [TestCase("212", "22", false)]
        [TestCase("", "22", true)]
        [TestCase("", "22", false)]
        [TestCase("212", "", true)]
        [TestCase("212", "", false)]
        public void Node_ComplexParent_ReparentDifferentRoot(string path1, string path2, bool connectLate)
        {
            // Tests if reparenting from one complex hirerachy to different complex hierarchy works.

            // Arrange
            var hierarchy1 = CreateComplexHierarchy(parent1);
            var hierarchy2 = CreateComplexHierarchy(parent2);
            var networkedHierarchy1 = CreateComplexHierarchy(networkedParent1);
            var networkedHierarchy2 = CreateComplexHierarchy(networkedParent2);

            // Act
            if (!connectLate)
            {
                networked.node.CallStart();
            }

            // Act - Parent1
            ParentAuthority(sync1, hierarchy1[path1].transform);
            UpdateNetworked(connectLate, networkedSync1);

            // Assert - Parent1
            Assert.AreEqual(networkedHierarchy1[path1].transform, networked.transform.parent);

            // Act - Parent2
            ParentAuthority(sync2, hierarchy2[path2].transform);
            UpdateNetworked(false, networkedSync2);

            // Assert - Parent2
            Assert.AreEqual(networkedHierarchy2[path2].transform, networked.transform.parent);
        }



        // Tests below are not passing because of our known limitations which are correctly documented:
        // `The one important constraint is that the hierarchies have to be identical on all Clients.`
        //
        //[TestCase(true)]
        //[TestCase(false)]
        //public void Node_ComplexParent_DifferentHierarchies_MissingTransform(bool connectLate)
        //{
        //    // Tests if parenting in complex hierarchy works if there is a missing (destoryed) game object inside of the hirearchy
        //
        //    // Arrange
        //    var hierarchy = CreateComplexHierarchy(parent1);
        //    var networkedHierarchy = CreateComplexHierarchy(networkedParent1);
        //    GameObject.DestroyImmediate(networkedHierarchy["21"]);
        //    var path = "22";

        //    // Act
        //    if (!connectLate)
        //    {
        //        networked.node.CallStart();
        //    }

        //    ParentAuthority(sync1, hierarchy[path].transform);
        //    UpdateNetworked(connectLate, networkedSync1);

        //    // Assert
        //    Assert.AreEqual(networkedHierarchy[path].transform, networked.transform.parent);
        //}

        //[TestCase(true)]
        //[TestCase(false)]
        //public void Node_ComplexParent_DifferentHierarchies_DifferentOrder(bool connectLate)
        //{
        //    // Tests if parenting in complex hierarchy works if the order of game objects in the hierarchy is changed
        //
        //    // Arrange
        //    var hierarchy = CreateComplexHierarchy(parent1);
        //    var networkedHierarchy = CreateComplexHierarchy(networkedParent1);
        //    networkedHierarchy["21"].transform.SetAsLastSibling();
        //    var path = "22";


        //    // Act
        //    if (!connectLate)
        //    {
        //        networked.node.CallStart();
        //    }

        //    ParentAuthority(sync1, hierarchy[path].transform);
        //    UpdateNetworked(connectLate, networkedSync1);

        //    // Assert
        //    Assert.AreEqual(networkedHierarchy[path].transform, networked.transform.parent);
        //}
    }
}
