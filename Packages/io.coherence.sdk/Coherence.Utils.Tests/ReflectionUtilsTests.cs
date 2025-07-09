// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils.Tests
{
    using System.Reflection;
    using Coherence.Tests;
    using NUnit.Framework;

    public sealed class ReflectionUtilsTests : CoherenceTest
    {
        #region BaseType
        [Test] public void Should_Find_PublicStaticMethod_InBaseType_UsingPublicStaticFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Base), Base.PublicStaticInBaseName,  BindingFlags.Public | BindingFlags.Static));
        [Test] public void Should_NotFind_PublicStaticMethod_InBaseType_UsingNonPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PublicStaticInBaseName,  BindingFlags.NonPublic | BindingFlags.Static));
        [Test] public void Should_NotFind_PublicStaticMethod_InBaseType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PublicStaticInBaseName,  BindingFlags.Public | BindingFlags.Instance));

        [Test] public void Should_Find_PrivateStaticMethod_InBaseType_UsingNonPublicStaticFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateStaticInBaseName,  BindingFlags.NonPublic | BindingFlags.Static));
        [Test] public void Should_NotFind_PrivateStaticMethod_InBaseType_UsingPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateStaticInBaseName,  BindingFlags.Public | BindingFlags.Static));
        [Test] public void Should_NotFind_PrivateStaticMethod_InBaseType_UsingNonPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateStaticInBaseName,  BindingFlags.NonPublic | BindingFlags.Instance));

        [Test] public void Should_Find_PublicInstanceMethod_InBaseType_UsingPublicInstanceFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Base), Base.PublicInstanceInBaseName,  BindingFlags.Public | BindingFlags.Instance));
        [Test] public void Should_NotFind_PublicInstanceMethod_InBaseType_UsingNonPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PublicInstanceInBaseName,  BindingFlags.NonPublic | BindingFlags.Instance));
        [Test] public void Should_NotFind_PublicInstanceMethod_InBaseType_UsingPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PublicInstanceInBaseName,  BindingFlags.Public | BindingFlags.Static));
        [Test] public void Should_NotFind_PublicInstanceProperty_InBaseType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), nameof(Base.PublicInstancePropertyInBase),  BindingFlags.Public | BindingFlags.Instance));

        [Test] public void Should_Find_PrivateInstanceMethod_InBaseType_UsingNonPublicInstanceFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateInstanceInBaseName,  BindingFlags.NonPublic | BindingFlags.Instance));
        [Test] public void Should_NotFind_PrivateInstanceMethod_InBaseType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateInstanceInBaseName,  BindingFlags.Public | BindingFlags.Instance));
        [Test] public void Should_NotFind_PrivateInstanceMethod_InBaseType_UsingNonPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Base), Base.PrivateInstanceInBaseName,  BindingFlags.NonPublic | BindingFlags.Static));
        #endregion

        #region DerivedType
		[Test] public void Should_Find_PublicStaticMethod_InDerivedType_UsingPublicStaticFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicStaticInDerivedName,  BindingFlags.Public | BindingFlags.Static));
        [Test] public void Should_NotFind_PublicStaticMethod_InDerivedType_UsingNonPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicStaticInDerivedName,  BindingFlags.NonPublic | BindingFlags.Static));
        [Test] public void Should_NotFind_PublicStaticMethod_InDerivedType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicStaticInDerivedName,  BindingFlags.Public | BindingFlags.Instance));

        [Test] public void Should_Find_PrivateStaticMethod_InDerivedType_UsingNonPublicStaticFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateStaticInDerivedName,  BindingFlags.NonPublic | BindingFlags.Static));
        [Test] public void Should_NotFind_PrivateStaticMethod_InDerivedType_UsingPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateStaticInDerivedName,  BindingFlags.Public | BindingFlags.Static));
        [Test] public void Should_NotFind_PrivateStaticMethod_InDerivedType_UsingNonPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateStaticInDerivedName,  BindingFlags.NonPublic | BindingFlags.Instance));
        [Test] public void Should_NotFind_PublicInstanceProperty_InDerivedType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), nameof(Derived.PublicInstancePropertyInDerived),  BindingFlags.Public | BindingFlags.Instance));

        [Test] public void Should_Find_PublicInstanceMethod_InDerivedType_UsingPublicInstanceFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicInstanceInDerivedName,  BindingFlags.Public | BindingFlags.Instance));
        [Test] public void Should_NotFind_PublicInstanceMethod_InDerivedType_UsingNonPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicInstanceInDerivedName,  BindingFlags.NonPublic | BindingFlags.Instance));
        [Test] public void Should_NotFind_PublicInstanceMethod_InDerivedType_UsingPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PublicInstanceInDerivedName,  BindingFlags.Public | BindingFlags.Static));

        [Test] public void Should_Find_PrivateInstanceMethod_InDerivedType_UsingNonPublicInstanceFlags() => Assert.IsTrue(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateInstanceInDerivedName,  BindingFlags.NonPublic | BindingFlags.Instance));
        [Test] public void Should_NotFind_PrivateInstanceMethod_InDerivedType_UsingPublicInstanceFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateInstanceInDerivedName,  BindingFlags.Public | BindingFlags.Instance));
        [Test] public void Should_NotFind_PrivateInstanceMethod_InDerivedType_UsingNonPublicStaticFlags() => Assert.IsFalse(ReflectionUtils.MethodExists(typeof(Derived), Derived.PrivateInstanceInDerivedName,  BindingFlags.NonPublic | BindingFlags.Static));
        #endregion

        private abstract class Base
        {
            public const string PublicStaticInBaseName = nameof(PublicStaticInBase);
            public const string PrivateStaticInBaseName = nameof(PrivateStaticInBase);
            public const string PublicInstanceInBaseName = nameof(PublicInstanceInBase);
            public const string PrivateInstanceInBaseName = nameof(PrivateInstanceInBase);

            public bool PublicInstancePropertyInBase => false;

            public static bool PublicStaticInBase() => false;
            private static bool PrivateStaticInBase() => false;
            public bool PublicInstanceInBase() => false;
            private bool PrivateInstanceInBase() => false;
        }

        private sealed class Derived : Base
        {
            public const string PublicStaticInDerivedName = nameof(PublicStaticInDerived);
            public const string PrivateStaticInDerivedName = nameof(PrivateStaticInDerived);
            public const string PublicInstanceInDerivedName = nameof(PublicInstanceInDerived);
            public const string PrivateInstanceInDerivedName = nameof(PrivateInstanceInDerived);

            public bool PublicInstancePropertyInDerived => false;

            public static bool PublicStaticInDerived() => false;
            private static bool PrivateStaticInDerived() => false;
            public bool PublicInstanceInDerived() => false;
            private bool PrivateInstanceInDerived() => false;
        }
    }
}

