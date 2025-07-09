// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using UnityEngine;
    using Coherence.Tests;

    public class FixedUpdateInputTests : CoherenceTest
    {
        private static object[] buttonsTestSource => new object[]
        {
            KeyCode.A,
            new FixedUpdateInput.MouseButton(){Button = 0},
            new FixedUpdateInput.CustomButton(){Name = "foo"}
        };

        [Test]
        [TestCase(KeyCode.DownArrow, "downarrow", true)]
        [TestCase(KeyCode.DownArrow, "dOwnaRrow", true)]
        [TestCase(KeyCode.DownArrow, "DOWNARROW", true)]
        [TestCase(KeyCode.DownArrow, "down arrow", false)]
        [TestCase(KeyCode.DownArrow, "dOwna_Rrow", false)]
        [TestCase(KeyCode.DownArrow, "DownArro", false)]
        public void FixedUpdateInput_KeyNameToKeyCode_ValidatesName(KeyCode keyCode, string keyName, bool isValid)
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var fixedUpdateInput = new FixedUpdateInput(inputSource.Object);

            if (isValid)
            {
                Assert.That(() => { fixedUpdateInput.GetKey(keyName); },
                    Throws.Nothing);
            }
            else
            {
                Assert.That(() => { fixedUpdateInput.GetKey(keyName); },
                    Throws.InstanceOf<ArgumentException>());
            }
        }

        [Test]
        [TestCase(KeyCode.DownArrow, "downarrow")]
        [TestCase(KeyCode.DownArrow, "dOwnaRrow")]
        [TestCase(KeyCode.DownArrow, "DOWNARROW")]
        public void FixedUpdateInput_KeyNameToKeyCode_Works(KeyCode keyCode, string keyName)
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var fixedUpdateInput = new FixedUpdateInput(inputSource.Object);

            fixedUpdateInput.GetKey(keyCode);

            fixedUpdateInput.PressDown(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);

            // Act & Assert
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.None);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.Down);
            fixedUpdateInput.Update(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.Down);

            // The "pressed" state has been eaten by sampling
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.Up);
            fixedUpdateInput.Update(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.Up);

            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
            AssertKeyState(fixedUpdateInput, keyName, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        public void FixedUpdateInput_SinglePress_Works()
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var fixedUpdateInput = new FixedUpdateInput(inputSource.Object);
            var keyCode = KeyCode.A;

            fixedUpdateInput.GetKey(keyCode);

            fixedUpdateInput.PressDown(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);

            // Act & Assert
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            fixedUpdateInput.Update(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);

            // The "pressed" state has been eaten by sampling
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);
            fixedUpdateInput.Update(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);

            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        [TestCaseSource(nameof(buttonsTestSource))]
        public void ButtonState_SinglePress_Works<TButton>(TButton button)
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var buttonState = new FixedUpdateInput.ButtonState<TButton>(button,
                (FixedUpdateInput.IInputSource<TButton>)inputSource.Object);

            buttonState.PressDown(inputSource);
            buttonState.Release(inputSource);
            buttonState.Release(inputSource);

            // Act & Assert
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.None);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);
            buttonState.Update();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);

            // The "pressed" state has been eaten by sampling
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);
            buttonState.Update();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);

            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        public void FixedUpdateInput_DoublePress_Works()
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var fixedUpdateInput = new FixedUpdateInput(inputSource.Object);
            var keyCode = KeyCode.A;

            fixedUpdateInput.GetKey(keyCode);

            fixedUpdateInput.PressDown(keyCode, inputSource);
            fixedUpdateInput.KeepDown(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);

            fixedUpdateInput.PressDown(keyCode, inputSource);
            fixedUpdateInput.KeepDown(keyCode, inputSource);
            fixedUpdateInput.Release(keyCode, inputSource);

            // Act & Assert
            // First press
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);

            // Second press
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);

            // No more inputs
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        [TestCaseSource(nameof(buttonsTestSource))]
        public void ButtonState_DoublePress_Works<TButton>(TButton button)
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var buttonState = new FixedUpdateInput.ButtonState<TButton>(button,
                (FixedUpdateInput.IInputSource<TButton>)inputSource.Object);

            buttonState.PressDown(inputSource);
            buttonState.KeepDown(inputSource);
            buttonState.Release(inputSource);

            buttonState.PressDown(inputSource);
            buttonState.KeepDown(inputSource);
            buttonState.Release(inputSource);

            // Act & Assert
            // First press
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);

            // Second press
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);

            // No more inputs
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        public void FixedUpdateInput_PressedState_Works()
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var fixedUpdateInput = new FixedUpdateInput(inputSource.Object);
            var keyCode = KeyCode.A;

            fixedUpdateInput.GetKey(keyCode);

            fixedUpdateInput.PressDown(keyCode, inputSource);

            // Act & Assert
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);
            fixedUpdateInput.Update(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Down);

            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Pressed);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Pressed);

            // No change even after update
            fixedUpdateInput.Update(true);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Pressed);

            fixedUpdateInput.Release(keyCode, inputSource);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Pressed);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);

            fixedUpdateInput.Release(keyCode, inputSource);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.Up);
            fixedUpdateInput.FixedUpdate(true);
            AssertKeyState(fixedUpdateInput, keyCode, FixedUpdateInput.ButtonStatus.None);
        }

        [Test]
        [TestCaseSource(nameof(buttonsTestSource))]
        public void ButtonState_PressedState_Works<TButton>(TButton button)
        {
            // Arrange
            var inputSource = new Mock<FixedUpdateInput.IInput>();
            var buttonState = new FixedUpdateInput.ButtonState<TButton>(button,
                (FixedUpdateInput.IInputSource<TButton>)inputSource.Object);

            buttonState.PressDown(inputSource);

            // Act & Assert
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);
            buttonState.Update();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Down);

            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Pressed);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Pressed);

            // No change even after update
            buttonState.Update();
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Pressed);

            buttonState.Release(inputSource);
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Pressed);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);

            buttonState.Release(inputSource);
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.Up);
            buttonState.FixedUpdate();
            AssertStatus(buttonState, FixedUpdateInput.ButtonStatus.None);
        }

        private static void AssertStatus<TButton>(FixedUpdateInput.ButtonState<TButton> buttonState, FixedUpdateInput.ButtonStatus status)
        {
            Assert.That(buttonState.Status, Is.EqualTo(status));
        }

        private static void AssertKeyState(FixedUpdateInput fixedUpdateInput, KeyCode keyCode, FixedUpdateInput.ButtonStatus status)
        {
            (bool isDown, bool isPressed, bool isUp) = ButtonStatusToFlags(status);
            Assert.That(fixedUpdateInput.GetKeyDown(keyCode), Is.EqualTo(isDown), "IsDown");
            Assert.That(fixedUpdateInput.GetKey(keyCode), Is.EqualTo(isPressed), "IsPressed");
            Assert.That(fixedUpdateInput.GetKeyUp(keyCode), Is.EqualTo(isUp), "IsUp");
        }

        private static void AssertKeyState(FixedUpdateInput fixedUpdateInput, string key, FixedUpdateInput.ButtonStatus status)
        {
            (bool isDown, bool isPressed, bool isUp) = ButtonStatusToFlags(status);
            Assert.That(fixedUpdateInput.GetKeyDown(key), Is.EqualTo(isDown), "IsDown");
            Assert.That(fixedUpdateInput.GetKey(key), Is.EqualTo(isPressed), "IsPressed");
            Assert.That(fixedUpdateInput.GetKeyUp(key), Is.EqualTo(isUp), "IsUp");
        }

        private static (bool isDown, bool isPressed, bool isUp) ButtonStatusToFlags(FixedUpdateInput.ButtonStatus status)
        {
            return status switch
            {
                FixedUpdateInput.ButtonStatus.None => (false, false, false),
                FixedUpdateInput.ButtonStatus.Down => (true, true, false),
                FixedUpdateInput.ButtonStatus.Pressed => (false, true, false),
                FixedUpdateInput.ButtonStatus.Up => (false, false, true),
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }

    internal static class ButtonStateExtensions
    {
        public static void PressDown<TButton>(this FixedUpdateInput.ButtonState<TButton> buttonState, Mock<FixedUpdateInput.IInput> inputSource)
        {
            PressDown(buttonState.Button, inputSource);
            buttonState.Update();
        }

        public static void PressDown<TButton>(this FixedUpdateInput fixedUpdateInput, TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            PressDown(button, inputSource);
            fixedUpdateInput.Update(true);
        }

        private static void PressDown<TButton>(TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            inputSource.Reset();
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButton(button)).Returns(true);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonDown(button)).Returns(true);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonUp(button)).Returns(false);
        }

        public static void KeepDown<TButton>(this FixedUpdateInput.ButtonState<TButton> buttonState, Mock<FixedUpdateInput.IInput> inputSource)
        {
            KeepDown(buttonState.Button, inputSource);
            buttonState.Update();
        }

        public static void KeepDown<TButton>(this FixedUpdateInput fixedUpdateInput, TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            KeepDown(button, inputSource);
            fixedUpdateInput.Update(true);
        }

        public static void KeepDown<TButton>(TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            inputSource.Reset();
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButton(button)).Returns(false);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonDown(button)).Returns(true);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonUp(button)).Returns(false);
        }

        public static void Release<TButton>(this FixedUpdateInput.ButtonState<TButton> buttonState, Mock<FixedUpdateInput.IInput> inputSource)
        {
            Release(buttonState.Button, inputSource);
            buttonState.Update();
        }

        public static void Release<TButton>(this FixedUpdateInput fixedUpdateInput, TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            Release(button, inputSource);
            fixedUpdateInput.Update(true);
        }

        public static void Release<TButton>(TButton button, Mock<FixedUpdateInput.IInput> inputSource)
        {
            bool wasPressed = inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Object.GetButton(button);

            inputSource.Reset();
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButton(button)).Returns(false);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonDown(button)).Returns(false);
            inputSource.As<FixedUpdateInput.IInputSource<TButton>>().Setup(m => m.GetButtonUp(button)).Returns(wasPressed);
        }
    }
}
