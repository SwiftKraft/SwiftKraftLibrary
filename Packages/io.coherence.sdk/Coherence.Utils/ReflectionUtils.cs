// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Utility methods that make use of reflection.
    /// </summary>
    internal static class ReflectionUtils
    {
        /// <summary>
        /// Checks if a method by the given name exists on the given type matching the specified binding flags.
        /// </summary>
        /// <param name="targetType"> The type to examine. </param>
        /// <param name="methodName"> The exact name of the method to search for. </param>
        /// <param name="bindingFlags">
        /// Flags that specify how the search is conducted.
        /// <para>
        /// If <see cref="BindingFlags.NonPublic"/> is specified, then <paramref name="targetType"/>
        /// and all the types it derives from will be examined recursively.
        /// </para>
        /// </param>
        /// <returns> <see langword="true"/> if method was found; otherwise, <see langword="false"/>. </returns>
        public static bool MethodExists(Type targetType, string methodName, BindingFlags bindingFlags) => TryGetMethod(targetType, methodName, bindingFlags, out _);

        /// <summary>
        /// Gets <see cref="MethodInfo"/> for the first method by the given name found on the given type and matching the specified binding flags.
        /// </summary>
        /// <param name="targetType"> The type to examine. </param>
        /// <param name="methodName"> The exact name of the method to search for. </param>
        /// <param name="bindingFlags">
        /// Flags that specify how the search is conducted.
        /// <para>
        /// If <see cref="BindingFlags.NonPublic"/> is specified, then <paramref name="targetType"/>
        /// and all the types it derives from will be examined recursively.
        /// </para>
        /// </param>
        /// <param name="method">
        /// When this method returns, contains the <see cref="MethodInfo"/> for the first method that was found, if any;
        /// otherwise, <see langword="null"/>. This parameter is passed uninitialized.
        /// </param>
        /// <returns> <see langword="true"/> if method was found; otherwise, <see langword="false"/>. </returns>
        public static bool TryGetMethod(Type targetType, string methodName, BindingFlags bindingFlags, out MethodInfo method)
        {
            if(!bindingFlags.HasFlag(BindingFlags.NonPublic))
            {
                method = targetType.GetMember(methodName, bindingFlags).Select(m => m as MethodInfo).FirstOrDefault();
                return method is not null;
            }

            bindingFlags |= BindingFlags.DeclaredOnly;

            for(var type = targetType; type != null; type = type.BaseType)
            {
                method = type.GetMember(methodName, bindingFlags).Select(m => m as MethodInfo).FirstOrDefault();
                if(method is not null)
                {
                    return true;
                }
            }

            method = null;
            return false;
        }
    }
}
