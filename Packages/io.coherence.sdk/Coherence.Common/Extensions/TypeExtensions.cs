using System;
using System.Linq;

namespace Coherence.Common.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Type"/>.
    /// </summary>
    internal static class TypeExtensions
    {
        public static string ToStringWithGenericArguments(this Type type)
        {
            var typeName = type.Name;
            if (!type.IsGenericType)
            {
                return typeName;
            }

            return typeName.Substring(0, typeName.Length - 2) + "<" + string.Join(", ", type.GetGenericArguments().Select(ToStringWithGenericArguments)) + ">";
        }
    }
}
