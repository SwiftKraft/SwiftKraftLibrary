// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using Connection;
    using Coherence.Tests;

    public class SimulatorEditorUtilityTests : CoherenceTest
    {
        [SetUp]
        public void Setup()
        {
            var endPointData = new EndpointData();
            SimulatorEditorUtility.SaveEndPoint(endPointData);
        }

        [Test]
        public void EndpointData_IsInvalid_OnFirstLoad()
        {
            var endPoint = SimulatorEditorUtility.LastUsedEndpoint;
            Assert.That(endPoint.Validate().isValid, Is.False);
        }

        [Test]
        public void SimulatorEditorUtility_Updates_EndPointData_OnSave()
        {
            var endPoint = SimulatorEditorUtility.LastUsedEndpoint;
            Assert.That(endPoint.Validate().isValid, Is.False);

            var newEndPoint = new EndpointData
            {
                host = "127.0.0.1",
                port = 42000,
                schemaId = "abcdef",
                region = EndpointData.LocalRegion,
                roomId = 1,
                worldId = 2
            };

            SimulatorEditorUtility.SaveEndPoint(newEndPoint);
            var lastUsed = SimulatorEditorUtility.LastUsedEndpoint;
            Assert.That(lastUsed.host, Is.EqualTo(newEndPoint.host));
            Assert.That(lastUsed.port, Is.EqualTo(newEndPoint.port));
            Assert.That(lastUsed.schemaId, Is.EqualTo(newEndPoint.schemaId));
            Assert.That(lastUsed.region, Is.EqualTo(newEndPoint.region));
            Assert.That(lastUsed.roomId, Is.EqualTo(newEndPoint.roomId));
            Assert.That(lastUsed.worldId, Is.EqualTo(newEndPoint.worldId));
        }

        [Test]
        public void SimulatorEditorUtility_Updates_EndPointData_KeepsStoredValue()
        {
            var endPoint = SimulatorEditorUtility.LastUsedEndpoint;
            Assert.That(endPoint.Validate().isValid, Is.False);

            var newEndPoint = new EndpointData
            {
                host = "127.0.0.1",
                port = 42000,
                schemaId = "abcdef",
                region = EndpointData.LocalRegion,
                roomId = 1,
                worldId = 2
            };

            SimulatorEditorUtility.SaveEndPoint(newEndPoint);
            ResetInternalEndPointData();

            var lastUsed = SimulatorEditorUtility.LastUsedEndpoint;
            Assert.That(lastUsed.host, Is.EqualTo(newEndPoint.host));
            Assert.That(lastUsed.port, Is.EqualTo(newEndPoint.port));
            Assert.That(lastUsed.schemaId, Is.EqualTo(newEndPoint.schemaId));
            Assert.That(lastUsed.region, Is.EqualTo(newEndPoint.region));
            Assert.That(lastUsed.roomId, Is.EqualTo(newEndPoint.roomId));
            Assert.That(lastUsed.worldId, Is.EqualTo(newEndPoint.worldId));
        }

        private void ResetInternalEndPointData()
        {
            var fieldInfo =
                typeof(SimulatorEditorUtility).GetField("endpoint", BindingFlags.Static | BindingFlags.NonPublic);
            fieldInfo.SetValue(null, new EndpointData());
        }
    }
}
