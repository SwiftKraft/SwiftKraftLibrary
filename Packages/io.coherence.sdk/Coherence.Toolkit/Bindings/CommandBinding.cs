namespace Coherence.Toolkit.Bindings
{
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using System.Linq;
    using SimulationFrame;

    public class CommandBinding : Binding
    {
        public override string CoherenceComponentName => "GenericCommand";
        public override string SignatureRichText => $"{Name}<color=grey>({string.Join(", ", GetParameterAssemblyRuntimeTypes().Select(TypeUtils.GetNiceTypeString))})</color>";
        public override string SignaturePlainText => $"{Name}({string.Join(", ", GetParameterAssemblyRuntimeTypes().Select(TypeUtils.GetNiceTypeString))})";

        protected CommandBinding() { }
        public CommandBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        // TODO https://github.com/coherence/unity/issues/7837
        internal override bool Activate()
        {
            return true;
        }

        public override MemberInfo GetMemberInfo()
        {
            return runtimeMethodInfo;
        }

        public override void IsDirty(AbsoluteSimulationFrame simulationFrame, out bool dirty, out bool justStopped)
        {
            dirty = false;
            justStopped = false;
        }

        public override void MarkAsReadyToSend()
        {
            // No internals to reset.
        }

        public List<string> ParameterAssemblyTypes => descriptor.ParameterAssemblyTypes;

        private MethodInfo runtimeMethodInfo;
        private List<Type> parameterAssemblyRuntimeTypes;

        private List<Type> GetParameterAssemblyRuntimeTypes()
        {
            if (parameterAssemblyRuntimeTypes == null)
            {
                parameterAssemblyRuntimeTypes = new List<Type>();
                foreach (var parameterAssemblyType in descriptor.ParameterAssemblyTypes)
                {
                    parameterAssemblyRuntimeTypes.Add(Type.GetType(parameterAssemblyType));
                }
            }

            return parameterAssemblyRuntimeTypes;
        }

        public MethodInfo GetMethodInfo()
        {
            if (runtimeMethodInfo == null)
            {
                runtimeMethodInfo = GetUnityComponentType()?.GetMethod(Name, BindingFlags.Public | BindingFlags.Instance, null, GetParameterAssemblyRuntimeTypes().ToArray(), null);
                if (runtimeMethodInfo == null)
                {
                    Logger.Warning(Warning.ToolkitBindingFailedToReloadMethodInfo,
                        $"{GetUnityComponentType()}.{Signature} failed to reload MethodInfo.");
                }
            }

            return runtimeMethodInfo;
        }

        public override bool Equals(object other)
        {
            if (other is CommandBinding otherBinding)
            {
                return descriptor == otherBinding.descriptor && FullName == otherBinding.FullName && unityComponent == otherBinding.unityComponent;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = descriptor?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ FullName.GetHashCode();
                hashCode = (hashCode * 397) ^ unityComponent.GetHashCode();

                return hashCode;
            }
        }
    }
}
