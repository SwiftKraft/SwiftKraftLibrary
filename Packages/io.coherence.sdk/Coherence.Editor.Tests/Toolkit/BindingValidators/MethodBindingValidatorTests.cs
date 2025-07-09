// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests.Toolkit.BindingValidators
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using Editor.Toolkit.BindingValidators;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public class MethodBindingValidatorTests : CoherenceTest
    {
        private AppDomain currentDomain;
        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder modBuilder;

        [Test]
        public void MethodWithVoidReturnShouldBeValid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard,
                typeof(void), null, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.True, "Method should be valid");
        }

        [Test]
        public void MethodWithNonVoidReturnShouldBeInvalid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard,
                typeof(int), null, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Method should be invalid");
        }

        [Test]
        [TestCase(typeof(Transform))]
        [TestCase(typeof(CoherenceSync))]
        [TestCase(typeof(GameObject))]
        [TestCase(typeof(RectTransform))]
        public void MethodWithValidParametersShouldBeValid(Type paramType)
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard, typeof(void),
                new[]
                {
                    paramType,
                }, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.True, "Method should be valid");
        }

        [Test]
        public void MethodWithMultipleValidParametersShouldBeValid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard, typeof(void),
                new[]
                {
                    typeof(int),
                    typeof(float),
                    typeof(GameObject)
                }, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.True, "Method should be valid");
        }

        [Test]
        public void MethodWithInvalidParameterShouldBeInvalid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard,
                typeof(int), new[]
                {
                    typeof(MethodBindingValidatorTests),
                }, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Method should be invalid");
        }

        [Test]
        public void MethodWithInvalidThirdParameterShouldBeInvalid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("Test", MethodAttributes.Public, CallingConventions.Standard,
                typeof(int), new[]
                {
                    typeof(int),
                    typeof(float),
                    typeof(MethodBindingValidatorTests),
                    typeof(bool)
                }, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Method should be invalid");
        }

        [Test]
        public void SpecialNamedMethodsShouldBeInvalid()
        {
            var validator = new MethodBindingValidator();
            var mi = new DynamicMethod("set_Value", MethodAttributes.SpecialName, CallingConventions.Standard,
                typeof(void), null, typeof(MethodBindingValidatorTests), false);
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Special name method should be invalid");
        }

        [Test]
        [TestCase(typeof(object))]
        [TestCase(typeof(Component))]
        [TestCase(typeof(MonoBehaviour))]
        [TestCase(typeof(Object))]
        public void ObscuredTypeMethodOwnersShouldBeInvalid(Type parentType)
        {
            var validator = new MethodBindingValidator();
            var mi = parentType.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Obscured types should be invalid");
        }

        [Test]
        public void MethodMarkedObsoleteWithReturnValueShouldBeInvalid()
        {
            const string ObsoleteWithReturnMethod = nameof(ObsoleteWithReturnMethod);
            var typeBuilder = CreateTypeBuilder("MethodMarkedObsoleteWithReturnValueShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, ObsoleteWithReturnMethod, MethodAttributes.Public,
                typeof(int));
            AddCustomAttribute(methodBuilder, typeof(ObsoleteAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(ObsoleteWithReturnMethod, BindingFlags.Public | BindingFlags.Instance);

            var validator = new MethodBindingValidator();
            Assert.That(validator.AssertBindingIsValid(mi), Is.False, "Obsolete method command with return value should be invalid");
        }

        [Test]
        public void MethodWithCommandShouldBeValid()
        {
            const string MyMethod = nameof(MyMethod);
            var typeBuilder = CreateTypeBuilder("MethodWithCommandShouldBeValid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, MyMethod, MethodAttributes.Public, typeof(void));
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(MyMethod, BindingFlags.Public | BindingFlags.Instance);

            var validator = new MethodBindingValidator();
            Assert.That(validator.AssertBindingIsValid(mi), Is.True, "Method with command should be valid");
        }

        [Test]
        public void MethodMarkedObsoleteWithCommandShouldBeValid()
        {
            const string ObsoleteMethod = nameof(ObsoleteMethod);
            var typeBuilder = CreateTypeBuilder("MethodMarkedObsoleteWithCommandShouldBeValid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, ObsoleteMethod, MethodAttributes.Public, typeof(void));
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));
            AddCustomAttribute(methodBuilder, typeof(ObsoleteAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(ObsoleteMethod, BindingFlags.Public | BindingFlags.Instance);

            var validator = new MethodBindingValidator();
            Assert.That(validator.AssertBindingIsValid(mi), Is.True, "Obsolete method with command should be valid");
        }

        [Test]
        public void MethodWithCommandWithReturnValueShouldBeInvalid()
        {
            const string CommandInvalidReturnValue = nameof(CommandInvalidReturnValue);
            var typeBuilder = CreateTypeBuilder("MethodWithCommandWithReturnValueShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, CommandInvalidReturnValue, MethodAttributes.Public,
                typeof(int));
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(CommandInvalidReturnValue, BindingFlags.Public | BindingFlags.Instance);

            ExpectErrorFromInvalidBinding(mi);
        }

        [Test]
        public void MethodMarkedObsoleteWithCommandWithReturnValueShouldBeInvalid()
        {
            const string ObsoleteWithReturnMethod = nameof(ObsoleteWithReturnMethod);
            var typeBuilder = CreateTypeBuilder("MethodMarkedObsoleteWithCommandWithReturnValueShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, ObsoleteWithReturnMethod, MethodAttributes.Public,
                typeof(int));
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));
            AddCustomAttribute(methodBuilder, typeof(ObsoleteAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(ObsoleteWithReturnMethod, BindingFlags.Public | BindingFlags.Instance);

            ExpectErrorFromInvalidBinding(mi);
        }

        [Test]
        public void MethodMarkedObsoleteWithCommandWithInvalidParameterShouldBeInvalid()
        {
            const string ObsoleteInvalidParamMethod = nameof(ObsoleteInvalidParamMethod);
            var typeBuilder = CreateTypeBuilder("MethodMarkedObsoleteWithCommandWithInvalidParameterShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, ObsoleteInvalidParamMethod, MethodAttributes.Public,
                typeof(void), new []{typeof(MethodBindingValidatorTests)});
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));
            AddCustomAttribute(methodBuilder, typeof(ObsoleteAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(ObsoleteInvalidParamMethod, BindingFlags.Public | BindingFlags.Instance);

            ExpectErrorFromInvalidBinding(mi);
        }

        [Test]
        public void MethodMarkedObsoleteWithCommandWithInvalidSecondParameterShouldBeInvalid()
        {
            const string ObsoleteInvalidSecondParamMethod = nameof(ObsoleteInvalidSecondParamMethod);
            var typeBuilder = CreateTypeBuilder("MethodMarkedObsoleteWithCommandWithInvalidSecondParameterShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, ObsoleteInvalidSecondParamMethod, MethodAttributes.Public,
                typeof(void), new[]
                {
                    typeof(int),
                    typeof(MethodBindingValidatorTests)
                });
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));
            AddCustomAttribute(methodBuilder, typeof(ObsoleteAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(ObsoleteInvalidSecondParamMethod, BindingFlags.Public | BindingFlags.Instance);

            ExpectErrorFromInvalidBinding(mi);
        }

        [Test]
        public void MethodWithCommandWithInvalidArgumentsShouldBeInvalid()
        {
            const string CommandInvalidArgs = nameof(CommandInvalidArgs);
            var typeBuilder = CreateTypeBuilder("MethodWithCommandWithInvalidArgumentsShouldBeInvalid");
            var methodBuilder = CreateMethodBuilder(typeBuilder, CommandInvalidArgs, MethodAttributes.Public,
                typeof(void), new[]
                {
                    typeof(MethodBindingValidatorTests),
                });
            AddCustomAttribute(methodBuilder, typeof(CommandAttribute));

            var type = typeBuilder.CreateType();
            var mi = type.GetMethod(CommandInvalidArgs, BindingFlags.Public | BindingFlags.Instance);

            ExpectErrorFromInvalidBinding(mi);
        }

        private TypeBuilder CreateTypeBuilder(string typeName) =>
            GetModuleBuilder().DefineType(typeName + GetRandomPostFix(), TypeAttributes.Public);

        private string GetRandomPostFix(int length = 10)
            => new string(Enumerable.Repeat(0, length)
                .Select(_ => (char)UnityEngine.Random.Range('A', 'Z' + 1))
                .ToArray());

        private MethodBuilder CreateMethodBuilder(TypeBuilder typeBuilder, string methodName,
            MethodAttributes attributes, Type returnType = null,
            Type[] args = null)
        {
            var retType = returnType ?? typeof(void);
            var methodBuilder =
                typeBuilder.DefineMethod(methodName, attributes, CallingConventions.Standard, retType, args);

            // Method body can't be empty
            var myIL = methodBuilder.GetILGenerator();
            myIL.EmitWriteLine("Hello, world!");
            myIL.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        private void AddCustomAttribute(MethodBuilder methodBuilder, Type attributeType, string param = "")
        {
            var ctorParams = string.IsNullOrEmpty(param)
                ? new Type[]
                {
                }
                : new[]
                {
                    typeof(string),
                };
            var classCtorInfo = attributeType.GetConstructor(ctorParams);
            var parameters = string.IsNullOrEmpty(param)
                ? new object[]
                {
                }
                : new object[]
                {
                    param,
                };

            var customAttributeBuilder = new CustomAttributeBuilder(classCtorInfo, parameters);
            methodBuilder.SetCustomAttribute(customAttributeBuilder);
        }

        private ModuleBuilder GetModuleBuilder()
        {
            if (currentDomain == null)
            {
                currentDomain = Thread.GetDomain();
                var myAsmName = new AssemblyName
                {
                    Name = "MethodValidationTestsAssembly",
                };

                assemblyBuilder = currentDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
                modBuilder = assemblyBuilder.DefineDynamicModule("MethodValidationTestsModule");
            }

            return modBuilder;
        }

        private void ExpectErrorFromInvalidBinding(MethodInfo mi)
        {
            var validator = new MethodBindingValidator();
            Assert.That(validator.AssertBindingIsValid(mi), Is.False);
            LogAssert.Expect(LogType.Error, new Regex(".*"));
        }
    }
}
