// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using Coherence.Tests;

    public class CoherenceInputManagerTests : CoherenceTest
    {
        private const string IntegrationTestCategory = "Integration";

        private Mock<ICoherenceBridge> bridgeMock;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            bridgeMock = new Mock<ICoherenceBridge>();
        }

        [Test]
        public void CommonReceivedFrame_SetToOneLessThanCurrentFrameOnStart()
        {
            // Arrange
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(100);

            // Act
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);

            // Assert
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(99));
        }

        [Test]
        public void CommonReceivedFrame_UpdatesAfterProcessingEnabled()
        {
            // Arrange
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(100);
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(200);

            // Act
            inputManager.ProcessingEnabled = true;

            // Assert
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(199));
        }

        [Test]
        public void CommonReceivedFrame_UpdatesCurrentFrameOnTimeReset()
        {
            // Arrange
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(100);
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object)
            {
                ProcessingEnabled = true
            };

            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(200);

            // Act
            bridgeMock.Raise(m => m.OnTimeReset += null);

            // Assert
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(199));
        }

        [Test]
        public void CommonReceivedFrame_DoesntChangeOnNoInputs()
        {
            // Arrange
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(100);
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object)
            {
                ProcessingEnabled = true
            };

            // Act
            inputManager.Update();

            // Assert
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(99));
        }

        [Test]
        public void CommonReceivedFrame_UpdatesWithSingleInput()
        {
            // Arrange
            long simFrame = 100;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object)
            {
                ProcessingEnabled = true
            };

            var localClientBuffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame, true);

            Action<int> runFrames = (int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    inputManager.Update();
                    simFrame++;
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                }
            };

            // Act & Assert
            runFrames(1);

            // No one has joined yet, frames are set to the initial value
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(99));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(99));
            Assert.That(inputManager.ShouldPause, Is.False);

            inputManager.AddInput(localClientMock.Object);
            // Frame 101
            runFrames(1);

            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(101));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(101));
            Assert.That(inputManager.ShouldPause, Is.False);

            // Frame 102
            runFrames(10);

            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(111));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(111));
            Assert.That(inputManager.ShouldPause, Is.False);
        }

        [Test]
        public void CommonReceivedFrame_WorksWhenRemoteClientIsAhead()
        {
            // Arrange
            long simFrame = 100;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object)
            {
                ProcessingEnabled = true
            };

            var localClientBuffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame, true);

            var remoteClientBuffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);
            var remoteClientMock = CreateBufferBackedInputMock(remoteClientBuffer, () => simFrame);

            Action<int> runFrames = (int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    inputManager.Update();
                    simFrame++;
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                }
            };

            // Act & Assert
            inputManager.AddInput(localClientMock.Object);
            // Frame 100
            runFrames(1);

            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(100));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(100));
            Assert.That(inputManager.ShouldPause, Is.False);

            inputManager.AddInput(remoteClientMock.Object);
            // Frame 101
            runFrames(1);
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(100));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(100));
            Assert.That(inputManager.ShouldPause, Is.False);

            long recvFrame = 105;
            for (int i = 0; i < 20; i++)
            {
                remoteClientBuffer.ReceiveInput("test", recvFrame);
                runFrames(1);
                remoteClientBuffer.TryGetInput(simFrame, out _);

                Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(recvFrame));
                Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(recvFrame));
                Assert.That(inputManager.ShouldPause, Is.False);
                recvFrame++;
            }
        }

        [Test]
        public void CommonReceivedFrame_WorksWhenRemoteClientIsBehind()
        {
            // Arrange
            long simFrame = 100;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object)
            {
                ProcessingEnabled = true
            };

            var localClientBuffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame, true);

            var remoteClientBuffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);
            var remoteClientMock = CreateBufferBackedInputMock(remoteClientBuffer, () => simFrame);

            Action<int> runFrames = (int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    inputManager.Update();
                    simFrame++;
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                    bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);
                }
            };

            // Act & Assert
            inputManager.AddInput(localClientMock.Object);
            // Frame 100
            runFrames(1);

            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(100));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(100));
            Assert.That(inputManager.ShouldPause, Is.False);

            inputManager.AddInput(remoteClientMock.Object);
            // Frame 101
            runFrames(1);
            Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(100));
            Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(100));
            Assert.That(inputManager.ShouldPause, Is.False);

            // Remote client is late by 8 frames, but common receive frame is not downgraded
            // thus after
            long recvFrame = 92;
            for (int i = 0; i < 8; i++)
            {
                remoteClientBuffer.ReceiveInput("test", recvFrame);
                runFrames(1);
                remoteClientBuffer.TryGetInput(simFrame, out _);

                Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(100));
                Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(100));
                recvFrame++;
            }

            // The remote client must catch up at some point, otherwise we'll hang on a pause
            for (int i = 0; i < 8; i++)
            {
                remoteClientBuffer.ReceiveInput("test", recvFrame);
                remoteClientBuffer.TryGetInput(simFrame, out _);
                recvFrame++;
            }

            for (int i = 0; i < 15; i++)
            {
                remoteClientBuffer.ReceiveInput("test", recvFrame);
                runFrames(1);
                remoteClientBuffer.TryGetInput(simFrame, out _);

                Assert.That(inputManager.CommonReceivedFrame, Is.EqualTo(recvFrame));
                Assert.That(inputManager.AcknowledgedFrame, Is.EqualTo(recvFrame));
                Assert.That(inputManager.ShouldPause, Is.False);
                recvFrame++;
            }
        }

        [Test]
        [Category(IntegrationTestCategory)]
        public void MispredictionFrame_AlwaysLowerThanCommonReceivedFrame()
        {
            // Arrange
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);
            long simFrame = 0;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            var localClientBuffer = new InputBuffer<Str>(10);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame);

            var remoteClientABuffer = new InputBuffer<Str>(10);
            var remoteClientAMock = CreateBufferBackedInputMock(remoteClientABuffer, () => simFrame);

            var remoteClientBBuffer = new InputBuffer<Str>(10);
            var remoteClientBMock = CreateBufferBackedInputMock(remoteClientBBuffer, () => simFrame);

            inputManager.ProcessingEnabled = true;

            inputManager.AddInput(localClientMock.Object);
            inputManager.AddInput(remoteClientAMock.Object);
            inputManager.AddInput(remoteClientBMock.Object);

            // Act

            // Frames 0-30
            for (int i = 0; i < 30; i++)
            {
                remoteClientABuffer.ReceiveInput("", simFrame + 1);
                remoteClientBBuffer.ReceiveInput("", simFrame + 1);

                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientABuffer.TryGetInput(simFrame, out _);
                remoteClientBBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);
            }

            // Frame 31 & 32, no inputs received
            for (int i = 0; i < 2; i++)
            {
                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientABuffer.TryGetInput(simFrame, out _);
                remoteClientBBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);
            }

            // Client A receives input for frame 31 which was mispredicted
            remoteClientABuffer.ReceiveInput("misprediction", 31);

            // Frame 33
            inputManager.Update();
            simFrame++;

            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            localClientBuffer.TryGetInput(simFrame, out _);
            remoteClientABuffer.TryGetInput(simFrame, out _);
            remoteClientBBuffer.TryGetInput(simFrame, out _);

            bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

            localClientBuffer.AddInput("", simFrame);

            // Assert

            // There is no misprediction as we don't yet have inputs from all clients for the mispredicted frame
            Assert.That(inputManager.MispredictionFrame, Is.Null);

            // Client B receives input for frame 31 which was mispredicted
            remoteClientBBuffer.ReceiveInput("misprediction", 31);

            // Frame 34
            inputManager.Update();
            simFrame++;
            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            // Since we now have all inputs for the frame 31, misprediction should show up
            Assert.That(inputManager.MispredictionFrame, Is.EqualTo(31));
        }

        [Test]
        [Category(IntegrationTestCategory)]
        public void MispredictionFrame_LowersForMultiplemispredictions()
        {
            // Arrange
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);
            long simFrame = 0;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            var localClientBuffer = new InputBuffer<Str>(10);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame);

            var remoteClientABuffer = new InputBuffer<Str>(10);
            var remoteClientAMock = CreateBufferBackedInputMock(remoteClientABuffer, () => simFrame);

            var remoteClientBBuffer = new InputBuffer<Str>(10);
            var remoteClientBMock = CreateBufferBackedInputMock(remoteClientBBuffer, () => simFrame);

            inputManager.ProcessingEnabled = true;

            inputManager.AddInput(localClientMock.Object);
            inputManager.AddInput(remoteClientAMock.Object);
            inputManager.AddInput(remoteClientBMock.Object);

            // Act

            // Frames 0-30
            for (int i = 0; i < 30; i++)
            {
                remoteClientABuffer.ReceiveInput("", simFrame + 1);
                remoteClientBBuffer.ReceiveInput("", simFrame + 1);

                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientABuffer.TryGetInput(simFrame, out _);
                remoteClientBBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);
            }

            // Frame 31-35, no inputs received
            for (int i = 0; i < 5; i++)
            {
                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientABuffer.TryGetInput(simFrame, out _);
                remoteClientBBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);
            }

            // Client A receives inputs for frame 31-33 where 33 is mispredicted
            remoteClientABuffer.ReceiveInput("", 31);
            remoteClientABuffer.ReceiveInput("", 32);
            remoteClientABuffer.ReceiveInput("misprediction", 33);

            // Frame 36
            inputManager.Update();
            simFrame++;

            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            localClientBuffer.TryGetInput(simFrame, out _);
            remoteClientABuffer.TryGetInput(simFrame, out _);
            remoteClientBBuffer.TryGetInput(simFrame, out _);

            bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

            localClientBuffer.AddInput("", simFrame);

            // Assert

            // There is no misprediction as we don't yet have inputs from all clients for the mispredicted frame
            Assert.That(inputManager.MispredictionFrame, Is.Null);

            // Client B receives input for frame 31 which was mispredicted
            remoteClientBBuffer.ReceiveInput("misprediction", 31);

            // Frame 37
            inputManager.Update();
            simFrame++;
            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            // Since we now have all inputs for the frame 31, misprediction should show up
            Assert.That(inputManager.MispredictionFrame, Is.EqualTo(31));
        }

        [Test]
        [Category(IntegrationTestCategory)]
        public void ShouldPause_Works()
        {
            // Arrange
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);
            long simFrame = 0;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            var localClientBuffer = new InputBuffer<Str>(10, 5);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame);

            var remoteClientBuffer = new InputBuffer<Str>(10, 5);
            var remoteClientMock = CreateBufferBackedInputMock(remoteClientBuffer, () => simFrame);

            inputManager.ProcessingEnabled = true;

            inputManager.AddInput(localClientMock.Object);
            inputManager.AddInput(remoteClientMock.Object);

            // Act

            // Simulation went just fine for 100 frames
            for (int i = 0; i < 104; i++)
            {
                // We didn't receive anything for the last 4 frames
                if (i < 100)
                {
                    remoteClientBuffer.ReceiveInput("", simFrame + 1);
                }

                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);

                Assert.That(inputManager.ShouldPause, Is.False);
            }

            // Since we have a delay of 5, on the 105 frame the local client should pause to not overwrite any inputs
            // required to rollback the simulation in case a misprediction happened at any frame above 100.

            inputManager.Update();
            simFrame++;

            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            localClientBuffer.TryGetInput(simFrame, out _);
            remoteClientBuffer.TryGetInput(simFrame, out _);

            bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

            localClientBuffer.AddInput("", simFrame);

            // Assert

            Assert.That(inputManager.ShouldPause, Is.True);
            bool hasInput = ((IInputBuffer)localClientBuffer).TryPeekInput(inputManager.CommonReceivedFrame + 1, out _);
            Assert.That(hasInput, Is.True);

            // Ensure that one frame later the input would be gone
            remoteClientBuffer.ReceiveInput("misprediction", 101);

            inputManager.Update();
            simFrame++;

            bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

            localClientBuffer.TryGetInput(simFrame, out _);
            remoteClientBuffer.TryGetInput(simFrame, out _);

            bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

            localClientBuffer.AddInput("", simFrame);

            // There was a misprediction on frame 101, but since we didn't pause the input is now gone
            hasInput = ((IInputBuffer)localClientBuffer).TryPeekInput(inputManager.MispredictionFrame.Value, out _);
            Assert.That(hasInput, Is.False);
        }

        [Test]
        [Category(IntegrationTestCategory)]
        public void ShouldPause_UnpausesAfterInputIsReceived()
        {
            // Arrange
            CoherenceInputManager inputManager = new CoherenceInputManager(bridgeMock.Object);
            long simFrame = 0;
            bridgeMock.Setup(m => m.ClientFixedSimulationFrame).Returns(() => simFrame);

            var localClientBuffer = new InputBuffer<Str>(10, 3);
            var localClientMock = CreateBufferBackedInputMock(localClientBuffer, () => simFrame);

            var remoteClientBuffer = new InputBuffer<Str>(10, 3);
            var remoteClientMock = CreateBufferBackedInputMock(remoteClientBuffer, () => simFrame);

            inputManager.ProcessingEnabled = true;

            inputManager.AddInput(localClientMock.Object);
            inputManager.AddInput(remoteClientMock.Object);

            // Act

            // Simulation for 27 frames where for the last 7 frames remote client doesn't receive anything
            for (int i = 0; i < 27; i++)
            {
                if (i < 20)
                {
                    remoteClientBuffer.ReceiveInput(i.ToString(), simFrame + 1);
                }

                Assert.That(inputManager.ShouldPause, Is.False);

                inputManager.Update();
                simFrame++;

                bridgeMock.Raise(m => m.OnFixedNetworkUpdate += null);

                localClientBuffer.TryGetInput(simFrame, out _);
                remoteClientBuffer.TryGetInput(simFrame, out _);

                bridgeMock.Raise(m => m.OnLateFixedNetworkUpdate += null);

                localClientBuffer.AddInput("", simFrame);
            }

            // Assert
            Assert.That(inputManager.ShouldPause, Is.True);

            // We do another loop, but this time we paused so only standard `Update` is run
            inputManager.Update();

            // No input received, still paused
            Assert.That(inputManager.ShouldPause, Is.True);

            // We've received an input, the system should unpause now
            remoteClientBuffer.ReceiveInput("x", 21);
            inputManager.Update();

            Assert.That(inputManager.ShouldPause, Is.False);
        }

        static Mock<ICoherenceInput> CreateBufferBackedInputMock(InputBuffer<Str> buffer, Func<long> currentSimulationFrame, bool isProducer = false)
        {
            var mock = new Mock<ICoherenceInput>();

            mock.Setup(m => m.Buffer).Returns(buffer);
            mock.Setup(m => m.IsProducer).Returns(isProducer);
            mock.Setup(m => m.BufferSize).Returns(() => buffer.Size);
            mock.Setup(m => m.MispredictionFrame).Returns(() => buffer.MispredictionFrame);
            mock.Setup(m => m.LastAcknowledgedFrame).Returns(() => buffer.LastAcknowledgedFrame);
            mock.Setup(m => m.LastReceivedFrame).Returns(() => buffer.LastReceivedFrame);
            mock.Setup(m => m.ShouldPause(It.IsAny<long>()))
                .Returns((long commonReceivedFrame) => buffer.ShouldPause(currentSimulationFrame(), commonReceivedFrame)
            );

            return mock;
        }
    }
}
