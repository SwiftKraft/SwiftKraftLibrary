// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Debugging
{
    using System.Diagnostics;

#if UNITY_5_3_OR_NEWER
    using AssertImpl = UnityDbgAssert;
#else
    using AssertImpl = SystemDbgAssert;
#endif

    public static class DbgAssert
    {
#if UNITY_5_3_OR_NEWER
        const string ASSERTIONS_ENABLED = "UNITY_ASSERTIONS";
#else
        const string ASSERTIONS_ENABLED = "DEBUG";
#endif

        public static bool Enabled { get; set; } = true;

        [Conditional(ASSERTIONS_ENABLED)]
        public static void That(bool condition, string message)
        {
            if (!Enabled)
            {
                return;
            }

            AssertImpl.That(condition, message);
        }

        [Conditional(ASSERTIONS_ENABLED)]
        public static void ThatFmt<T1>(bool condition, string messageToFormat, in T1 arg1)
        {
            if (!Enabled)
            {
                return;
            }

            AssertImpl.ThatFmt(condition, messageToFormat, arg1);
        }

        [Conditional(ASSERTIONS_ENABLED)]
        public static void ThatFmt<T1, T2>(bool condition, string messageToFormat, in T1 arg1, in T2 arg2)
        {
            if (!Enabled)
            {
                return;
            }

            AssertImpl.ThatFmt(condition, messageToFormat, arg1, arg2);
        }

        [Conditional(ASSERTIONS_ENABLED)]
        public static void ThatFmt<T1, T2, T3>(bool condition, string messageToFormat, in T1 arg1, in T2 arg2, in T3 arg3)
        {
            if (!Enabled)
            {
                return;
            }

            AssertImpl.ThatFmt(condition, messageToFormat, arg1, arg2, arg3);
        }

        [Conditional(ASSERTIONS_ENABLED)]
        public static void ThatFmt<T1, T2, T3, T4>(bool condition, string messageToFormat, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4)
        {
            if (!Enabled)
            {
                return;
            }

            AssertImpl.ThatFmt(condition, messageToFormat, arg1, arg2, arg3, arg4);
        }
    }
}

