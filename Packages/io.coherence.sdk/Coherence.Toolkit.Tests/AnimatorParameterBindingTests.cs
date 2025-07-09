namespace Coherence.Toolkit.Tests
{
    using Bindings;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class AnimatorParameterBindingTests : CoherenceTest
    {
        private static readonly int _floatParameter = Animator.StringToHash("FloatParameter");
        private static readonly int _intParameter = Animator.StringToHash("IntParameter");
        private static readonly int _boolParameter = Animator.StringToHash("BoolParameter");

        private const double Epsilon = 1E-9f;

        private static Animator CreateAnimator()
        {
            var animator = new GameObject().AddComponent<Animator>();
            var controller = (RuntimeAnimatorController)Object.Instantiate(Resources.Load("TestAnimatorController", typeof(RuntimeAnimatorController)));
            animator.runtimeAnimatorController = controller;
            return animator;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(float.MaxValue)]
        [TestCase(float.MinValue)]
        public void TestFloatAnimatorParameterBinding(float value)
        {
            // Arrange
            var animator = CreateAnimator();
            var parameter = animator.GetParameter(0);
            var descriptor = new AnimatorDescriptor(typeof(FloatAnimatorParameterBinding), parameter);
            var binding = (FloatAnimatorParameterBinding)descriptor.InstantiateBinding(animator);

            // Act
            binding.Value = value;

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value).Within(Epsilon));
            Assert.That(animator.GetFloat(_floatParameter), Is.EqualTo(value).Within(Epsilon));

            // Act
            binding.Value = default;
            animator.SetFloat(_floatParameter, value);

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value).Within(Epsilon));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void TestIntAnimatorParameterBinding(int value)
        {
            // Arrange
            var animator = CreateAnimator();
            var parameter = animator.GetParameter(1);
            var descriptor = new AnimatorDescriptor(typeof(IntAnimatorParameterBinding), parameter);
            var binding = (IntAnimatorParameterBinding)descriptor.InstantiateBinding(animator);

            // Act
            binding.Value = value;

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value));
            Assert.That(animator.GetInteger(_intParameter), Is.EqualTo(value));

            // Act
            binding.Value = default;
            animator.SetInteger(_intParameter, value);

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value));
        }


        [TestCase(true)]
        [TestCase(false)]
        public void TestBoolAnimatorParameterBinding(bool value)
        {
            // Arrange
            var animator = CreateAnimator();
            var parameter = animator.GetParameter(2);
            var descriptor = new AnimatorDescriptor(typeof(BoolAnimatorParameterBinding), parameter);
            var binding = (BoolAnimatorParameterBinding)descriptor.InstantiateBinding(animator);

            // Act
            binding.Value = value;

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value));
            Assert.That(animator.GetBool(_boolParameter), Is.EqualTo(value));

            // Act
            binding.Value = default;
            animator.SetBool(_boolParameter, value);

            // Assert
            Assert.That(binding.Value, Is.EqualTo(value));
        }
    }
}
