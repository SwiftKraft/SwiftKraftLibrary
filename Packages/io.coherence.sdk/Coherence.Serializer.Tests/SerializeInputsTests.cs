// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

// If these tests fail, then it's likely the code gen is broken.
// Take a look at README_BAKING in Coherence.Common.Tests to regenerate the code.

namespace Coherence.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Brook;
    using Brook.Octet;
    using Entities;
    using Generated;
    using Log;
    using NUnit.Framework;
    using ProtocolDef;
    using Serializer;
    using UnityEngine;
    using UnityEngine.TestTools.Utils;
    using Logger = Log.Logger;
    using Coherence.Tests;

    public class SerializeInputsTest : CoherenceTest
    {
        private bool useDebugStreams = false;
        private readonly Entity entityID = new Entity(1, 0, Entity.Relative);
        private readonly IDefinition definition = new Definition();

        new private Logger logger = new UnityLogger();

        private (uint, uint) SerializeMessageBuffer(Queue<SerializedEntityMessage> inputs, int packetSize)
        {
            (int _, uint expectedBitCount) =
                MessageQueueSerializer.GetCountFromBudget(inputs, (uint)packetSize, useDebugStreams);

            var packetStream = new OutOctetStream(packetSize);
            var outBitStream = useDebugStreams
                ? new DebugOutBitStream(new OutBitStream(packetStream))
                : (IOutBitStream)new OutBitStream(packetStream);

            Serialize.WriteMessages(new List<SerializedEntityMessage>(), MessageType.Input, inputs, new SerializerContext<IOutBitStream>(outBitStream, useDebugStreams, logger));

            var actualBitCount = outBitStream.Position;

            return (actualBitCount, expectedBitCount);
        }

        private IEntityInput[] ProcessInputs(Queue<SerializedEntityMessage> inputs, int packetSize,
            out int numSerialized)
        {
            var packetStream = new OutOctetStream(packetSize);
            var outBitStream = useDebugStreams
                ? new DebugOutBitStream(new OutBitStream(packetStream))
                : (IOutBitStream)new OutBitStream(packetStream);

            var inputsSent = new List<SerializedEntityMessage>();
            Serialize.WriteMessages(inputsSent, MessageType.Input, inputs, new SerializerContext<IOutBitStream>(outBitStream, useDebugStreams, logger));
            numSerialized = inputsSent.Count();

            outBitStream.Flush();

            var octetReader = new InOctetStream(packetStream.Close().ToArray());
            var inBitStream = useDebugStreams
                ? new DebugInBitStream(new InBitStream(octetReader, (int)outBitStream.Position))
                : (IInBitStream)new InBitStream(octetReader, (int)outBitStream.Position);

            DeserializeCommands.DeserializeCommand(inBitStream, out _);

            return definition.ReadInputs(inBitStream, logger);
        }

        private void CreateInput<T>(ref List<IEntityInput> inputs, ref Queue<SerializedEntityMessage> messageQueue,
            T inputValue)
        {
            var inputData = new InputData();
            inputData.Frame = 0;

            switch (inputValue)
            {
                case bool b:
                    var boolInputMessage = new BoolInput(entityID, 0, b, false);
                    inputData.Input = boolInputMessage;
                    break;
                case int i:
                    var intInputMessage = new IntInput(entityID, 0, i, false);
                    inputData.Input = intInputMessage;
                    break;
                case float f:
                    var floatInputMessage = new FloatInput(entityID, 0, f, false);
                    inputData.Input = floatInputMessage;
                    break;
                case Quaternion q:
                    var quaternionInputMessage = new QuaternionInput(entityID, 0, q, false);
                    inputData.Input = quaternionInputMessage;
                    break;
                case float[] fa:
                    var length = fa.Length;
                    if (length == 2)
                    {
                        var float2InputMessage = new Vector2Input(entityID, 0, new Vector2(fa[0], fa[1]), false);
                        inputData.Input = float2InputMessage;
                    }
                    else if (length == 3)
                    {
                        var float3InputMessage = new Vector3Input(entityID, 0, new Vector3(fa[0], fa[1], fa[2]), false);
                        inputData.Input = float3InputMessage;
                    }
                    else if (length == 4)
                    {
                        var float4InputMessage = new QuaternionInput(entityID, 0,
                            new Quaternion(fa[0], fa[1], fa[2], fa[3]), false);
                        inputData.Input = float4InputMessage;
                    }
                    else
                    {
                        Assert.Fail($"Unsupported float array length: {length}");
                    }

                    break;
                case Vector2 v:
                    var vector2fInputMessage = new Vector2Input(entityID, 0, v, false);
                    inputData.Input = vector2fInputMessage;
                    break;
                case Vector3 v:
                    var vector3fInputMessage = new Vector3Input(entityID, 0, v, false);
                    inputData.Input = vector3fInputMessage;
                    break;
                case string s:
                    var stringInputMessage = new StringInput(entityID, 0, s, false);
                    inputData.Input = stringInputMessage;
                    break;
                case object[] o:
                    var inputTypeName = (string)o[0];
                    if (inputTypeName == nameof(MultiInput))
                    {
                        var multiInputMessage = new MultiInput();
                        multiInputMessage.intField = (int)o[1];
                        multiInputMessage.floatField = (float)o[2];
                        var fa = (float[])o[3];
                        multiInputMessage.axisField = new Vector2(fa[0], fa[1]);
                        multiInputMessage.stringField = (string)o[4];
                        inputData.Input = multiInputMessage;
                    }
                    else if (inputTypeName == nameof(CompressedInput))
                    {
                        var compressedInputMessage = new CompressedInput();
                        compressedInputMessage.intField = (int)o[1];
                        compressedInputMessage.floatField = (float)o[2];
                        var fa = (float[])o[3];
                        compressedInputMessage.axisField = new Vector2(fa[0], fa[1]);
                        compressedInputMessage.stringField = (string)o[4];
                        inputData.Input = compressedInputMessage;
                    }
                    else
                    {
                        Assert.Fail($"Unsupported input type: {inputTypeName}");
                    }

                    break;
            }

            var serializedMessage = Serialize.SerializeMessage(MessageType.Input, MessageTarget.AuthorityOnly,
                inputData, entityID, definition, useDebugStreams, logger);
            messageQueue.Enqueue(serializedMessage);

            inputs.Add(inputData.Input);
        }

        private void CompareInput(IEntityInput input, IEntityInput original)
        {
            const float
                epsilon = 0.001f; // This should be somehow computed based on the bit range and scale of the floats compression.

            var inputType = input.GetComponentType();
            var originalType = original.GetComponentType();

            Assert.AreEqual(inputType, originalType);

            switch (inputType)
            {
                case Definition.InternalBoolInput:
                    var originalBool = (BoolInput)original;
                    var inputBool = (BoolInput)input;

                    Assert.AreEqual(originalBool.boolField, inputBool.boolField);
                    break;

                case Definition.InternalIntInput:
                    var originalInt = (IntInput)original;
                    var inputInt = (IntInput)input;

                    Assert.AreEqual(originalInt.intField, inputInt.intField);
                    break;
                case Definition.InternalFloatInput:
                    var originalFloat = (FloatInput)original;
                    var inputFloat = (FloatInput)input;

                    Assert.That(Utils.AreFloatsEqual(originalFloat.floatField, inputFloat.floatField, epsilon),
                        Is.True);
                    break;
                case Definition.InternalQuaternionInput:
                    var originalQuaternion = (QuaternionInput)original;
                    var inputQuaternion = (QuaternionInput)input;

                    Assert.That(
                        Utils.AreFloatsEqual(originalQuaternion.quaternionField.x, inputQuaternion.quaternionField.x,
                            epsilon), Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalQuaternion.quaternionField.y, inputQuaternion.quaternionField.y,
                            epsilon), Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalQuaternion.quaternionField.z, inputQuaternion.quaternionField.z,
                            epsilon), Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalQuaternion.quaternionField.w, inputQuaternion.quaternionField.w,
                            epsilon), Is.True);
                    break;
                case Definition.InternalVector2Input:
                    var originalVector2 = (Vector2Input)original;
                    var inputVector2 = (Vector2Input)input;

                    Assert.That(
                        Utils.AreFloatsEqual(originalVector2.vector2Field.x, inputVector2.vector2Field.x, epsilon),
                        Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalVector2.vector2Field.y, inputVector2.vector2Field.y, epsilon),
                        Is.True);
                    break;
                case Definition.InternalVector3Input:
                    var originalVector3 = (Vector3Input)original;
                    var inputVector3 = (Vector3Input)input;

                    Assert.That(
                        Utils.AreFloatsEqual(originalVector3.vector3Field.x, inputVector3.vector3Field.x, epsilon),
                        Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalVector3.vector3Field.y, inputVector3.vector3Field.y, epsilon),
                        Is.True);
                    Assert.That(
                        Utils.AreFloatsEqual(originalVector3.vector3Field.z, inputVector3.vector3Field.z, epsilon),
                        Is.True);
                    break;
                case Definition.InternalStringInput:
                    var originalString = (StringInput)original;
                    var inputString = (StringInput)input;

                    Assert.AreEqual(originalString.stringField, inputString.stringField);
                    for (int i = 0; i < originalString.stringField.Length; i++)
                    {
                        Assert.AreEqual(originalString.stringField[i], inputString.stringField[i]);
                    }

                    break;
            }
        }

        [Test]
        public void TestBasicInputMessages()
        {
            // Arrange
            var originalInputs = new List<IEntityInput>();
            var messageBuffer = new Queue<SerializedEntityMessage>();

            CreateInput(ref originalInputs, ref messageBuffer, true);
            CreateInput(ref originalInputs, ref messageBuffer, 123);
            CreateInput(ref originalInputs, ref messageBuffer, 123.456f);
            CreateInput(ref originalInputs, ref messageBuffer, Quaternion.Euler(1.0f, 2.0f, 3.0f));
            CreateInput(ref originalInputs, ref messageBuffer, new Vector2(1.0f, 2.0f));
            CreateInput(ref originalInputs, ref messageBuffer, new Vector3(1.0f, 2.0f, 3.0f));
            CreateInput(ref originalInputs, ref messageBuffer, "Hello World");

            var numInputs = messageBuffer.Count();

            // Act
            var numSerialized = 0;
            var inputsRes = ProcessInputs(messageBuffer, 1024, out numSerialized);

            // Assert
            Assert.AreEqual(numInputs, numSerialized);
            Assert.AreEqual(numInputs, inputsRes.Length);

            for (int i = 0; i < inputsRes.Count(); i++)
            {
                var input = inputsRes[i];
                var original = originalInputs[i];

                CompareInput(input, original);
            }
        }

        private void ProcessTooManyInputs(List<IEntityInput> originalInputs,
            Queue<SerializedEntityMessage> messageBuffer, int packetSize)
        {
            // Act
            var numSerialized = 0;
            var inputsRes = ProcessInputs(messageBuffer, packetSize, out numSerialized);

            Debug.Log($"inputs processed: {inputsRes.Count()}");

            Assert.NotZero(numSerialized);
            Assert.Less(inputsRes.Count(), originalInputs.Count());

            // Assert
            for (int i = 0; i < inputsRes.Count(); i++)
            {
                var input = inputsRes[i];
                var original = originalInputs[i];

                CompareInput(input, original);
            }
        }

        [Test]
        [TestCase("TooManyBool", 128, new object[]
        {
            true,
            false,
            true,
            false,
            true,
            false,
            true,
            false,
            true,
            false,
            true,
            false
        })]
        [TestCase("TooManyInts", 128, new object[]
        {
            0,
            -9999,
            9999,
            1234,
            -1234,
            1212,
            -1212,
            777,
            -777,
            -123,
            123,
            0
        })]
        [TestCase("TooManyFloats", 128, new object[]
        {
            0.0f,
            -2300.0f,
            2300.0f,
            1234.0f,
            -1234.0f,
            1212.0f,
            -1212.0f,
            777.0f,
            -777.0f,
            -123.0f,
            123.0f,
            0.0f
        })]
        [TestCase("TooManyStrings", 128, new object[]
        {
            "Hello",
            "World",
            "Hello",
            "World",
            "Hello",
            "World",
            "Hello",
            "World",
            "Hello",
            "World",
            "Hello",
            "World"
        })]
        [TestCase("TooManyVector2s", 128, new object[]
        {
            new[]
            {
                0.0f,
                1.0f
            },
            new[]
            {
                2.0f,
                3.0f
            },
            new[]
            {
                4.0f,
                5.0f
            },
            new[]
            {
                6.0f,
                7.0f
            },
            new[]
            {
                8.0f,
                9.0f
            },
            new[]
            {
                10.0f,
                11.0f
            },
            new[]
            {
                123.45f,
                123.45f
            },
            new[]
            {
                -123.45f,
                -123.45f
            },
            new[]
            {
                -2300.0f,
                2300.0f
            },
            new[]
            {
                111.0f,
                222.0f
            },
            new[]
            {
                333.0f,
                444.0f
            },
            new[]
            {
                555.0f,
                666.0f
            },
            new[]
            {
                0.0f,
                -1.0f
            },
            new[]
            {
                -2.0f,
                -3.0f
            },
            new[]
            {
                -4.0f,
                -5.0f
            },
            new[]
            {
                -6.0f,
                -7.0f
            },
            new[]
            {
                -8.0f,
                -9.0f
            },
            new[]
            {
                -10.0f,
                -11.0f
            },
        })]
        [TestCase("TooManyVector3s", 128, new object[]
        {
            new[]
            {
                0.0f,
                1.0f,
                2.0f
            },
            new[]
            {
                3.0f,
                4.0f,
                5.0f
            },
            new[]
            {
                6.0f,
                7.0f,
                8.0f
            },
            new[]
            {
                9.0f,
                10.0f,
                11.0f
            },
            new[]
            {
                12.0f,
                13.0f,
                14.0f
            },
            new[]
            {
                15.0f,
                16.0f,
                17.0f
            },
            new[]
            {
                123.45f,
                123.45f,
                123.45f
            },
            new[]
            {
                -123.45f,
                -123.45f,
                -123.45f
            },
            new[]
            {
                -2300.0f,
                2300.0f,
                2300.0f
            },
            new[]
            {
                111.0f,
                222.0f,
                333.0f
            },
            new[]
            {
                444.0f,
                555.0f,
                666.0f
            },
            new[]
            {
                777.0f,
                888.0f,
                999.0f
            },
            new[]
            {
                0.0f,
                -1.0f,
                -2.0f
            },
            new[]
            {
                -3.0f,
                -4.0f,
                -5.0f
            },
            new[]
            {
                -6.0f,
                -7.0f,
                -8.0f
            },
            new[]
            {
                -9.0f,
                -10.0f,
                -11.0f
            },
            new[]
            {
                -12.0f,
                -13.0f,
                -14.0f
            },
            new[]
            {
                -15.0f,
                -16.0f,
                -17.0f
            },
        })]
        [TestCase("TooManyQuaternions", 128, new object[]
        {
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
        })]
        public void TestTooManyInputs(string testName, int packetSize, object[] inputs)
        {
            // Arrange
            var originalInputs = new List<IEntityInput>();
            var messageBuffer = new Queue<SerializedEntityMessage>();
            var numInputs = inputs.Length;

            for (int i = 0; i < numInputs; i++)
            {
                CreateInput(ref originalInputs, ref messageBuffer, inputs[i]);
            }

            ProcessTooManyInputs(originalInputs, messageBuffer, packetSize);
        }

        [TestCase("Empty buffer", new object[]
        {
        })]
        [TestCase("Bool inputs", new object[]
        {
            true,
            false
        })]
        [TestCase("Integer inputs", new object[]
        {
            1,
            -781,
            0
        })]
        [TestCase("Float inputs", new object[]
        {
            2.3f,
            0.0f,
            -543.0123f
        })]
        [TestCase("Quaternion inputs", new object[]
        {
            new[]
            {
                0.0f,
                0.0f,
                0.0f,
                1.0f
            },
        })]
        [TestCase("Vector2 inputs", new object[]
        {
            new[]
            {
                0.0f,
                0.0f
            },
            new[]
            {
                -2300.0f,
                2300.0f
            },
            new[]
            {
                123.45f,
                -123.45f
            },
        })]
        [TestCase("Vector3 inputs", new object[]
        {
            new[]
            {
                0.0f,
                0.0f,
                0.0f
            },
            new[]
            {
                -2300.0f,
                2300.0f,
                2300.0f
            },
            new[]
            {
                123.45f,
                -123.45f,
                123.45f
            },
        })]
        [TestCase("Multiple fields", new object[]
        {
            new object[]
            {
                "MultiInput",
                2,
                1.3f,
                new float[]
                {
                    123,
                    -4
                },
                "string"
            },
            new object[]
            {
                "MultiInput",
                0,
                748.5f,
                new[]
                {
                    0,
                    13.5f
                },
                ""
            },
        })]
        [TestCase("Compressed fields", new object[]
        {
            new object[]
            {
                "CompressedInput",
                2,
                1.3f,
                new float[]
                {
                    123,
                    -4
                },
                "string"
            },
            new object[]
            {
                "CompressedInput",
                0,
                748.5f,
                new[]
                {
                    0,
                    13.5f
                },
                ""
            },
        })]
        [TestCase("Mixed input types", new object[]
        {
            145,
            200.0f,
            new object[]
            {
                "MultiInput",
                0,
                748.5f,
                new[]
                {
                    0,
                    13.5f
                },
                "string"
            },
            new object[]
            {
                "CompressedInput",
                0,
                748.5f,
                new[]
                {
                    0,
                    13.5f
                },
                "string"
            },
        })]
        public void TestInputMessageBitCount(string testName, object[] data)
        {
            // Arrange
            const int packetSize = 8 * 1280;

            var originalInputs = new List<IEntityInput>();
            var messageBuffer = new Queue<SerializedEntityMessage>();

            foreach (var input in data)
            {
                CreateInput(ref originalInputs, ref messageBuffer, input);
            }

            (uint actualBitCount, uint expectedBitCount) = SerializeMessageBuffer(messageBuffer, packetSize);

            Assert.That(actualBitCount, Is.EqualTo(expectedBitCount));
        }

        [TestCase("Zero char", 0)]
        [TestCase("One char", 1)]
        [TestCase("23 char", 23)]
        [TestCase("Max char", OutProtocolBitStream.SHORT_STRING_MAX_SIZE)]
        public void TestInputStringMessages(string testName, int numChars)
        {
            var chars = new char[numChars];
            for (int i = 0; i < numChars; i++)
            {
                chars[i] = (char)(i % 256);
            }

            TestInputMessageBitCount(testName, new object[]
            {
                new string(chars)
            });
        }
    }
}
