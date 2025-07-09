// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests.PlayerLoop
{
    using System;
    using System.Linq;
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;
    using Toolkit.PlayerLoop;
    using UnityEngine.LowLevel;

    public class CoherenceLoopTests : CoherenceTest
    {
        private static readonly Type[] LoopUpdateTypes =
        {
            typeof(UnityEngine.PlayerLoop.Update),
            typeof(UnityEngine.PlayerLoop.FixedUpdate),
            typeof(UnityEngine.PlayerLoop.PreLateUpdate),
        };

        private static readonly (Type, int)[] CallbackTypeCounts =
        {
            new(typeof(CoherenceLoop.CoherenceReceiver), 2),
            new(typeof(CoherenceLoop.CoherenceInterpolation), 3),
            new(typeof(CoherenceLoop.CoherenceSampler), 3),
            new(typeof(CoherenceLoop.CoherenceSender), 1),
        };

        [Test]
        public void PlayerLoopShouldContainValidCallbacks()
        {
            CoherenceLoop.Inject();
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            foreach (var (type, count) in CallbackTypeCounts)
            {
                Assert.That(
                    GetCoherenceUpdateSystemCount(playerLoop, type) == count,
                    Is.True,
                    $"Unexpected PlayerLoop callbacks count of type {type}"
                    );
            }
        }

        [Test]
        public void AddBridgeMultipleTimes()
        {
            var list = new CoherenceLoop.BridgeList<ICoherenceBridge>();
            var b1 = new Mock<ICoherenceBridge>();

            list.QueueAdd(b1.Object);
            list.QueueAdd(b1.Object);

            var bridges = list.Resolve();
            Assert.That(bridges, Is.Not.Empty, "Bridge not added to collection");
            Assert.That(bridges.Count(), Is.EqualTo(1), "Unexpected bridges count");
        }

        [Test]
        public void AddAndRemoveBridge()
        {
            var list = new CoherenceLoop.BridgeList<ICoherenceBridge>();
            var b1 = new Mock<ICoherenceBridge>();

            list.QueueAdd(b1.Object);
            list.QueueRemove(b1.Object);

            var bridges = list.Resolve();
            Assert.That(bridges, Is.Empty, "Collection should be empty");
        }

        [Test]
        public void AddAndRemoveMultipleBridges()
        {
            var list = new CoherenceLoop.BridgeList<ICoherenceBridge>();
            var b1 = new Mock<ICoherenceBridge>();
            var b2 = new Mock<ICoherenceBridge>();
            var b3 = new Mock<ICoherenceBridge>();

            list.QueueAdd(b1.Object);
            list.QueueAdd(b2.Object);
            list.QueueRemove(b1.Object);
            list.QueueAdd(b3.Object);

            var bridges = list.Resolve();
            Assert.That(bridges.Count(), Is.EqualTo(2), "Unexpected bridges count");
            Assert.That(bridges.Contains(b2.Object), Is.True, "Missing bridge 2");
            Assert.That(bridges.Contains(b3.Object), Is.True, "Missing bridge 3");
        }

        private int GetCoherenceUpdateSystemCount(PlayerLoopSystem playerLoop, Type type)
        {
            return playerLoop.subSystemList
                .Where(s => LoopUpdateTypes.Contains(s.type))
                .SelectMany(s => s.subSystemList)
                .Count(s => s.type == type);
        }
    }
}
