// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit.BindingValidators
{
    using System.Reflection;
    using Coherence.Toolkit;
    using Log;

    /// <summary>
    /// The <see cref="MethodBindingValidator"/> class fetches a filtered list of methods attached to the type.
    /// </summary>
    internal class MethodBindingValidator
    {
        private readonly Logger logger;

        public MethodBindingValidator()
        {
            logger = Log.GetLogger<MethodBindingValidator>();
        }


        /// <summary>
        /// Verify that the method can be used for a Command. Will write any errors or warnings out to the console.
        /// </summary>
        /// <param name="methodInfo">The method to be validated</param>
        /// <returns>True if the method is a valid command</returns>
        internal bool AssertBindingIsValid(MethodInfo methodInfo)
        {
            var bindingState = methodInfo.GetBindingState();
            if (!methodInfo.IsDefined(typeof(CommandAttribute)))
            {
                return bindingState == BindingState.Valid;
            }

            // DeclaringType can be null. Using ?? instead of putting ?. everywhere
            var type = methodInfo.DeclaringType ?? typeof(object);

            switch (bindingState)
            {
                case BindingState.Incompatible:
                    logger.Error(Error.ToolkitBindingIncompatible,
                        $"Method {type.Name}.{methodInfo.Name} cannot return a value and cannot contain unsupported parameters");
                    break;
                case BindingState.Obsolete:
                    logger.Warning(Warning.ToolkitBindingObsolete,
                        $"Method {type.Name}.{methodInfo.Name} is obsolete");
                    break;
                case BindingState.SpecialName:
                    logger.Error(Error.ToolkitBindingSpecial,
                        $"Method {type.Name}.{methodInfo.Name} is a special name. Please remove the [Command] attribute");
                    break;
                case BindingState.UnsuppotedType:
                    logger.Error(Error.ToolkitBindingUnsupported,
                        $"Command bindings cannot target members of the type {type.Name}");
                    break;
            }

            return bindingState == BindingState.Valid || bindingState == BindingState.Obsolete;
        }
    }
}
