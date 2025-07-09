// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using UnityEngine;
    using Coherence.Tests;

    public struct Str
    {
        public string Value;

        public static implicit operator string(Str str) => str.Value;
        public static implicit operator Str(string str) => new Str { Value = str };

        public override string ToString() => Value ?? string.Empty;
    }

    public class InputBufferTests : CoherenceTest
    {
        [Test]
        public void ReceiveInput_QueuingRespectsSubsequentFrameRequirement()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 10, true);
            for (int i = 0; i <= 100; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
            }

            // Act
            buffer.TryGetInput(100, out Str input);
            buffer.ReceiveInput("110", 110);

            // Assert
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(100));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(100));
        }

        [Test]
        public void ReceiveInput_ProgressesReceivedFrameWhenQueued()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 10, false);
            for (int i = 0; i <= 100; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
            }

            // Act & Assert
            buffer.TryGetInput(100, out Str input);

            buffer.ReceiveInput("110", 110);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(100));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(109));

            buffer.TryGetInput(109, out input);
            Assert.That(input.Value, Is.EqualTo("100"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(110));

            buffer.TryGetInput(110, out input);
            Assert.That(input.Value, Is.EqualTo("110"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(110));

            buffer.ReceiveInput("140", 140);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(110));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(119));

            buffer.TryGetInput(120, out input);
            Assert.That(input.Value, Is.EqualTo("110"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(129));

            buffer.TryGetInput(130, out input);
            Assert.That(input.Value, Is.EqualTo("110"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(139));

            buffer.TryGetInput(140, out input);
            Assert.That(input.Value, Is.EqualTo("140"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(140));
        }

        [Test]
        public void TryGetInput_ProgressesQueuedInputs()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 10, false);
            for (int i = 0; i < 100; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
            }

            // Act
            buffer.TryGetInput(99, out Str input);

            // Assert
            Assert.That(input.Value, Is.EqualTo("99"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(99));
            Assert.That(buffer.LastAcknowledgedFrame, Is.EqualTo(99));
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(99));
        }

        [Test]
        [TestCase(0, 2)]
        [TestCase(2, 0)]
        [TestCase(2, 5)]
        [TestCase(10, 5)]
        [TestCase(15, 10)]
        public void Reset_RestoresBaseDelay(int baseDelay, int changedDelay)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(30, baseDelay, true);

            // Act & Assert
            Assert.That(buffer.Delay, Is.EqualTo(baseDelay));

            buffer.Delay = changedDelay;
            Assert.That(buffer.Delay, Is.EqualTo(changedDelay));

            buffer.Reset();
            Assert.That(buffer.Delay, Is.EqualTo(baseDelay));
        }

        [Test]
        public void OrderingWorks()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, requiresSubsequentFrames: true);

            buffer.ReceiveInput("10", 10);

            // Act & Assert
            buffer.ReceiveInput("12", 12);
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(10));

            buffer.ReceiveInput("11", 11);
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(12));
        }

        [Test]
        public void DoesntOverwriteInputs()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(3, 0);

            // Act & Assert
            // -   : Nothing in the buffer
            // *   : Predicted input
            // (x) : Value at that frame
            // !   : Last consumed frame

            // #FRAME 0
            // [ -, -, -]
            buffer.TryGetInput(0, out _);
            // [ 0*, -, -]

            // #FRAME 1
            // [ 0*, -, -]
            buffer.TryGetInput(1, out _);
            // [ 0*, 1*, -]
            buffer.ReceiveInput("p", 0);
            // [ 0(p), 1(p)*, -]

            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(-1));

            // #FRAME 2
            // [ 0(p), 1(p)*, -]
            buffer.TryGetInput(2, out _);
            // [ !0(p), 1(p)*, 2(p)*]

            Assert.That(buffer.ShouldPause(2, 0), Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(0));

            // [ !0(p), 1(p)*, 2(p)*]
            buffer.ReceiveInput("x", 1);
            // [ !0(p), 1(x), 2(x)*]

            Assert.That(buffer.ShouldPause(2, 1), Is.False);

            // #FRAME 3
            // [ !0(p), 1(x), 2(x)*]
            buffer.TryGetInput(3, out Str input);
            // [ 3(x)*, !1(x), 2(x)*]

            Assert.That(buffer.ShouldPause(3, 1), Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(1));

            // [ 3(x)*, !1(x), 2(x)*]
            buffer.ReceiveInput("y", 2);
            // [ 3(y)*, !1(x), 2(y)]
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(2));

            // [ 3(y)*, !1(x), 2(y)]
            buffer.ReceiveInput("z", 3);
            // [ 3(z), !1(x), 2(y)]
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(3));

            // [ 3(z), !1(x), 2(y)]
            buffer.ReceiveInput("q", 4);
            // [ 3(z), !1(x), 2(y)] <Queue: 4(q)>
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(3));

            // [ 3(z), !1(x), 2(y)] <Queue: 4(q)>
            buffer.ReceiveInput("r", 5);
            // [ 3(z), !1(x), 2(y)] <Queue: 4(q), 5(r)>

            // #FRAME 4
            // [ 3(z), !1(x), 2(y)] <Queue: 4(q), 5(r)>
            buffer.TryGetInput(4, out input);
            // [ 3(z), !4(q), 5(r)]
            Assert.That(input.Value, Is.EqualTo("q"));
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(5));
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(4));

            // [ 3(z), !4(q), 5(r)]
            buffer.ReceiveInput("s", 6);
            // [ 6(s), !4(q), 5(r)] <Queue: 6(s)>
            Assert.That(buffer.LastReceivedFrame, Is.EqualTo(6));
        }

        [Test]
        [TestCase(0, 20, 20, 10, true)]
        [TestCase(0, 20, 20, 12, false)]
        [TestCase(0, 20, 20, 25, false)]
        [TestCase(0, 20, 20, 0, true)]
        [TestCase(3, 17, 20, 11, true)]
        [TestCase(3, 17, 20, 14, false)]
        public void ShouldPause_WorksForLocalBuffer(int inputDelay, int inputFrame,
            int currentFrame, int ackFrame, bool expectedPause)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, inputDelay);
            buffer.AddInput("i", 0);
            buffer.AddInput("i", inputFrame);

            // Act
            bool shouldPause = buffer.ShouldPause(currentFrame, ackFrame);

            // Assert
            Assert.That(shouldPause, Is.EqualTo(expectedPause));
        }

        [Test]
        public void HandlesLongQueue()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 0);

            for (int i = 0; i < 100; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
            }

            // Act
            buffer.TryGetInput(95, out Str input);

            // Assert
            Assert.That(input.Value, Is.EqualTo("95"));
        }

        [Test]
        public void ReceiveInput_AcknowledgesInput()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 0);
            Queue<long> ackedInputs = new Queue<long>();

            // Act
            buffer.ReceiveInput("hello", 10);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);
            buffer.ReceiveInput("world", 11);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);
            buffer.TryGetInput(10, out Str input);
            buffer.ReceiveInput("world", 12);
            buffer.TryGetInput(12, out input);
            buffer.TryGetInput(13, out input);
            buffer.ReceiveInput("mispredict", 13);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);
            buffer.TryGetInput(13, out input);
            buffer.ReceiveInput("predict", 14);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);
            buffer.TryGetInput(17, out input);
            buffer.ReceiveInput("predict", 15);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);
            buffer.ReceiveInput("predict", 16);
            buffer.ReceiveInput("mispredict", 17);
            ackedInputs.Enqueue(buffer.LastAcknowledgedFrame);

            // Assert
            Assert.That(ackedInputs.Count, Is.EqualTo(6));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(10));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(11));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(12));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(14));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(15));
            Assert.That(ackedInputs.Dequeue(), Is.EqualTo(16));
        }

        [Test]
        public void ReceiveInput_QueuesInputsAsNeeded()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 0);

            // Act & Assert
            buffer.ReceiveInput("hello", 10);

            bool gotInput = buffer.TryGetInput(10, out Str input);
            Assert.That(gotInput, Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(10));
            Assert.That(input.Value, Is.EqualTo("hello"));

            buffer.ReceiveInput("world", 25);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(10));

            gotInput = buffer.TryGetInput(15, out input);
            Assert.That(gotInput, Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(15));
            Assert.That(input.Value, Is.EqualTo("hello"));

            gotInput = buffer.TryGetInput(25, out input);
            Assert.That(gotInput, Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(25));
            Assert.That(input.Value, Is.EqualTo("world"));

            gotInput = buffer.TryGetInput(26, out input);
            Assert.That(gotInput, Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(25));
            Assert.That(input.Value, Is.EqualTo("world"));

            gotInput = buffer.TryGetInput(27, out input);
            Assert.That(gotInput, Is.True);
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(25));
            Assert.That(input.Value, Is.EqualTo("world"));
        }

        [Test]
        public void ReceiveInput_DoesntChangeLastConsumedOnReceive_WhenMispredictions()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 0);

            // Act
            buffer.ReceiveInput("hello", 10);
            buffer.TryGetInput(10, out Str _);
            buffer.ReceiveInput("world", 25);
            buffer.TryGetInput(20, out Str _);
            buffer.TryGetInput(25, out Str _);
            buffer.TryGetInput(26, out Str _);
            buffer.TryGetInput(27, out Str _);

            buffer.ReceiveInput("worldx", 26);

            // Assert
            Assert.That(buffer.LastConsumedFrame, Is.EqualTo(25));
        }

        [Test]
        public void InvalidPredictionFrame_WorksWithInitialInput()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);

            // Act
            buffer.ReceiveInput("hello", 95);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(default(long?)));
        }

        [Test]
        public void InvalidPredictionFrame_IgnoresInitialInputReceived()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.TryGetInput(100, out _);

            // Act
            buffer.ReceiveInput("hello", 95);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(null));
        }

        [Test]
        public void InvalidPredictionFrame_WorksWithFetchAfterInitialInput()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.ReceiveInput("hello", 95);

            // Act
            buffer.TryGetInput(100, out _);
            buffer.ReceiveInput("world", 98);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(98));
        }

        [Test]
        public void InvalidPredictionFrame_NotOverwrittenOnSubsequentPredictionFailures()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.ReceiveInput("hello", 5);
            buffer.TryGetInput(10, out _);

            // Act
            buffer.ReceiveInput("world", 7);
            buffer.ReceiveInput("foo", 8);
            buffer.ReceiveInput("bar", 10);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(7));
        }

        [Test]
        public void InvalidPredictionFrame_ResetsAfterInputFetch()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.ReceiveInput("hello", 5);
            buffer.TryGetInput(10, out _);

            // Act & Assert
            buffer.ReceiveInput("world", 7);

            buffer.TryGetInput(6, out _);
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(7));

            buffer.TryGetInput(7, out _);
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(default(long?)));
        }

        [Test]
        public void InvalidPredictionFrame_IsSetOnPredictionFailure()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.ReceiveInput("hello", 5);
            buffer.TryGetInput(10, out _);

            // Act
            buffer.ReceiveInput("world", 7);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(7));
        }

        [Test]
        public void InvalidPredictionFrame_IsNullOnNoPredictionFailure()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10);
            buffer.ReceiveInput("hello", 5);

            // Act
            buffer.TryGetInput(10, out _);

            // Assert
            Assert.That(buffer.MispredictionFrame, Is.EqualTo(default(long?)));
        }

        [Test]
        public void DequeueForSending_Works()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(5, 0);
            buffer.AddInput("hello", 10);
            buffer.AddInput("world", 12);

            // Act && Assert
            bool dequeued = buffer.DequeueForSending(10, out long inputFrame, out Str input, out bool differs);
            Assert.That(dequeued, Is.True);
            Assert.That(differs, Is.True);
            Assert.That(inputFrame, Is.EqualTo(10));
            Assert.That(input, Is.EqualTo((Str)"hello"));

            dequeued = buffer.DequeueForSending(12, out inputFrame, out input, out differs);
            Assert.That(dequeued, Is.True);
            Assert.That(differs, Is.False);
            Assert.That(inputFrame, Is.EqualTo(11));
            Assert.That(input, Is.EqualTo((Str)"hello"));

            dequeued = buffer.DequeueForSending(12, out inputFrame, out input, out differs);
            Assert.That(dequeued, Is.True);
            Assert.That(differs, Is.True);
            Assert.That(inputFrame, Is.EqualTo(12));
            Assert.That(input, Is.EqualTo((Str)"world"));
        }

        [TestCase(2, 0)]
        [TestCase(2, 2)]
        public void TryGetInput_ReturnsDefaultValueOnNoInput(int bufferSize, int frame)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(bufferSize);

            // Act
            buffer.TryGetInput(frame, out Str value);

            // Assert
            Assert.That(value, Is.EqualTo(default(Str)));
        }

        [TestCase(10, 5, 4, 4)]
        [TestCase(10, 5, 8, 4)]
        [TestCase(10, 5, 12, 4)]
        public void TryGetInput_UpdatesBufferWithLastInput(int bufferSize, int valuesAdded, int frame, int expectedValue)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(bufferSize);
            for (int i = 0; i < valuesAdded; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
            }

            // Act
            buffer.TryGetInput(frame, out Str value);

            // Assert
            Assert.That(expectedValue, Is.EqualTo(int.Parse(value)));
        }

        [TestCase(10, 20, 11, 11, true)]
        [TestCase(10, 20, 15, 15, true)]
        [TestCase(10, 20, 20, 19, true)]
        [TestCase(10, 20, 40, 19, true)]
        [TestCase(10, 20, 8, -1, false)]
        public void TryGetInput_ReturnsCorrectValues(int bufferSize, int valuesAdded, int frame, int expectedValue, bool expectedFlag)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(bufferSize);
            for (int i = 0; i < valuesAdded; i++)
            {
                buffer.ReceiveInput(i.ToString(), i);
                buffer.TryGetInput(i, out _);
            }

            // Act
            bool gotInput = buffer.TryGetInput(frame, out Str value);

            // Assert
            Assert.That(expectedFlag, Is.EqualTo(gotInput));
            if (expectedFlag)
            {
                Assert.That(expectedValue, Is.EqualTo(int.Parse(value)));
            }
        }

        private static object[] ReceiveInput_Works_TestCases = {
            new object[]
            {
                10, new[]
                {
                    ("hello", 2L, false),
                    ("hello", 4, false),
                    ("world", 6, true),
                    ("foo", 7, true)
                },
                new []
                {
                    (default, 0, 1),
                    ("hello", 2L, 5L),
                    ("world", 6, 6),
                    ("foo", 7, 7)
                }
            },
            new object[]
            {
                10, new[]
                {
                    ("world", 4L, false),
                    ("world", 5, false),
                    ("hello", 6, true),
                    ("foo", 8, true),
                },
                new []
                {
                    (default, 1, 3),
                    ("world", 4L, 5L),
                    ("hello", 6, 7),
                    ("foo", 8, 8),
                }
            },
            new object[]
            {
                10, new[]
                {
                    ("hello", long.MaxValue - 100, false),
                },
                new []
                {
                    (default, long.MaxValue - 91, long.MaxValue - 99),
                    ("hello", long.MaxValue - 100, long.MaxValue - 100),
                }
            }
        };

        [TestCaseSource(nameof(ReceiveInput_Works_TestCases))]
        public void ReceiveInput_Works(int bufferSize,
            (string value, long frame, bool expectsInvalidPrediction)[] inputs,
            (string expectedValue, long fromFrame, long toFrame)[] expectedInputs)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(bufferSize, 0);

            // Act
            foreach ((Str value, long frame, bool expectsInvalidPrediction) in inputs)
            {
                // In order to trigger invalid prediction
                buffer.TryGetInput(frame, out _);

                bool invalidPrediction = buffer.ReceiveInput(value, frame);
                Assert.That(invalidPrediction, Is.EqualTo(expectsInvalidPrediction), $"Frame: {frame}, Value: {value}");

                // In order to clear prediction flag
                buffer.TryGetInput(frame, out _);
            }

            // Assert
            foreach ((Str expectedValue, long fromFrame, long toFrame) in expectedInputs)
            {
                for (long frame = fromFrame; frame <= toFrame; frame++)
                {
                    bool gotInput = buffer.TryGetInput(frame, out Str inputValue);
                    Assert.That(gotInput, Is.True, $"Frame: {frame}, From: {fromFrame}, To: {toFrame}, Value: {expectedValue}");
                    Assert.That(inputValue, Is.EqualTo(expectedValue));
                }
            }
        }

        [Test]
        public void AddInput_RespectsDelay(
            [Values(10)] int bufferSize,
            [Values(0, 1, 5, 9)] int delay,
            [Values(0, 5, 100)] int frameOffset)
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(bufferSize, delay);
            buffer.AddInput("hello", 2 + frameOffset);
            buffer.AddInput("world", 4 + frameOffset);

            // Act && Assert
            buffer.TryGetInput(delay + frameOffset, out Str input);
            Assert.That(input, Is.EqualTo(default(Str)));

            buffer.TryGetInput(2 + delay + frameOffset, out input);
            Assert.That(input, Is.EqualTo((Str)"hello"));

            buffer.TryGetInput(3 + delay + frameOffset, out input);
            Assert.That(input, Is.EqualTo((Str)"hello"));

            buffer.TryGetInput(4 + delay + frameOffset, out input);
            Assert.That(input, Is.EqualTo((Str)"world"));
        }

        [Test]
        public void AddInput_AppendsOnLoweredDelay_IfInputDoesntMatch()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 2);
            buffer.AddInput("hello", 5);

            // Act
            buffer.Delay = 1;
            buffer.AddInput("world", 6);

            // Assert
            Assert.That(buffer.LastFrame, Is.EqualTo(8));

            buffer.TryGetInput(7, out Str input);
            Assert.That(input, Is.EqualTo((Str)"hello"));

            buffer.TryGetInput(8, out input);
            Assert.That(input, Is.EqualTo((Str)"world"));
        }

        [Test]
        public void AddInput_NoOpOnLoweredDelay_IfInputMatches()
        {
            // Arrange
            InputBuffer<Str> buffer = new InputBuffer<Str>(10, 2);
            buffer.AddInput("hello", 5);

            // Act
            buffer.Delay = 1;
            buffer.AddInput("hello", 6);

            // Assert
            Assert.That(buffer.LastFrame, Is.EqualTo(7));

            bool gotInput = buffer.TryGetInput(7, out Str input);
            Assert.That(gotInput, Is.True);
            Assert.That(input, Is.EqualTo((Str)"hello"));

            gotInput = buffer.TryGetInput(8, out input);
            Assert.That(gotInput, Is.False);
            Assert.That(buffer.LastFrame, Is.EqualTo(7), "Sending side shouldn't bump frames on getters");
            Assert.That(input, Is.EqualTo(default(Str)));
        }
    }
}
