namespace Coherence.Common
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// PreserveAttribute prevents Unity's byte code stripping from removing a class, method, field, or property.
    /// <remarks>
    /// This works just like
    /// <see aref="https://docs.unity3d.com/ScriptReference/Scripting.PreserveAttribute.html">UnityEngine.Scripting.PreserveAttribute</see>,
    /// but without the dependency on UnityEngine.dll.
    /// </remarks>
    /// </summary>
    [Conditional("UNITY_5_3_OR_NEWER"), Conditional("UNITY")]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class PreserveAttribute : Attribute { }
}
