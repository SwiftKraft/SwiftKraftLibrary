// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Entities;
    using ProtocolDef;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using Bindings;
    using UnityEngine;
    using Log;
    using Logger = Log.Logger;
    using Utils;
    using Component = UnityEngine.Component;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void GenericCommandRequestDelegate(MessageTarget target, ChannelID channelID, object[] args);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CommandsHandler
    {
        private struct CommandData
        {
            public Component Receiver;
            public GenericCommandRequestDelegate Receive;
            public GenericCommandRequestDelegate Send;
            public MessageTarget Routing;
            public bool HasSenderArg;
        }

        private struct SendOptions
        {
            public Component Receiver;
            public bool SendToAllBindings;
        }

        [Flags]
        private enum SendTo
        {
            None = 0,
            Self = 1,
            Others = 2,
            All = Self | Others
        }

        private readonly ICoherenceSync sync;
        private readonly List<Binding> bindings;

        private readonly Dictionary<string, List<CommandData>> commandRequestDataByName = new Dictionary<string, List<CommandData>>();
        private readonly Dictionary<string, List<CommandBinding>> commandNameMemoization = new Dictionary<string, List<CommandBinding>>();
        private readonly HashSet<string> registeredCommandNames = new HashSet<string>();

        private CoherenceSyncBaked BakedScript => sync.BakedScript;
        private bool HasStateAuthority => sync.EntityState?.AuthorityType.Value.ControlsState() ?? true;
        private Entity EntityID => sync.EntityState?.EntityID ?? default;
        private ICoherenceBridge CoherenceBridge => sync.CoherenceBridge;
        private CoherenceClientConnection MyConnection => CoherenceBridge != null ? CoherenceBridge.ClientConnections?.GetMine() : null;
        private string Name => sync.name;

        private bool IsConnected => CoherenceBridge != null && CoherenceBridge.IsConnected;

        public Logger logger { get; internal set; }

        public CommandsHandler(ICoherenceSync sync, List<Binding> bindings, Logger logger)
        {
            this.sync = sync;
            this.bindings = bindings;
            this.logger = logger;
        }

        public void AddBakedCommand(string name, string signatureAsString, GenericCommandRequestDelegate sendDelegate,
            GenericCommandRequestDelegate receiveDelegate, MessageTarget routing, Component receiver, bool hasSenderArg)
        {
            var fullKey = name + signatureAsString;

            if (!commandRequestDataByName.TryGetValue(fullKey, out var commandDataList))
            {
                commandDataList = new List<CommandData>();
                commandRequestDataByName.Add(fullKey, commandDataList);
            }

            commandDataList.Add(new CommandData
            {
                Receiver = receiver,
                Send = sendDelegate,
                Receive = receiveDelegate,
                Routing = routing,
                HasSenderArg = hasSenderArg
            });

            registeredCommandNames.Add(name);
        }

        public void HandleCommand(IEntityCommand command, MessageTarget target)
        {
            if (!CanRouteMessage(target, command.Routing))
            {
                logger.Warning(Warning.ToolkitSyncCommandInvalidRouting,
                    ("target", target),
                    ("routing allowed", command.Routing),
                    ("entity", EntityID));

                return;
            }

            BakedScript.ReceiveCommand(command);
        }

        // Mixed object version where args could be tuples using reflection.
        public bool SendCommand(Type targetType, string methodName, MessageTarget target, ChannelID channelID, bool sendToAllBindings, params object[] args)
        {
            if (!GetCommandNameAndValidateEntityId(targetType, methodName, out var commandName))
            {
                return false;
            }

            var options = GetSendOptions(null, sendToAllBindings);

            return SendCommandUsingBakedScript(commandName, target, channelID, options, args);
        }

        // Pure tuples version optimization skipping all reflection.
        public bool SendCommand(Type targetType, string methodName, MessageTarget target, ChannelID channelID, bool sendToAllBindings, params ValueTuple<Type, object>[] args)
        {
            if (!GetCommandNameAndValidateEntityId(targetType, methodName, out string commandName))
            {
                return false;
            }

            SendOptions options = GetSendOptions(null, sendToAllBindings);

            return SendCommandUsingBakedScript(commandName, target, channelID, options, args);
        }

        public bool SendCommand(Action method, MessageTarget target, ChannelID channelID)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, Array.Empty<(Type, object)>(), receiver, methodName);
        }

        public bool SendCommand<T>(Action<T> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2>(Action<T1, T2> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3>(Action<T1, T2, T3> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        public bool SendCommand<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> method, MessageTarget target, ChannelID channelID, ValueTuple<Type, object>[] args)
        {
            var receiver = method.Target;
            var methodName = method.Method.Name;

            return ExecuteSendCommand(target, channelID, false, args, receiver, methodName);
        }

        private SendOptions GetSendOptions(Component receiver, bool sendToAllBindings)
        {
            var options = new SendOptions()
            {
                Receiver = receiver,
                SendToAllBindings = sendToAllBindings
            };

            return options;
        }

        private bool ExecuteSendCommand(MessageTarget target, ChannelID channelID, bool sendToAllBindings, (Type, object)[] args,
            object receiver, string methodName)
        {
            if (!(receiver is Component))
            {
                logger.Error(Error.ToolkitSyncCommandNonComponent);
            }

            if (!GetCommandNameAndValidateEntityId(receiver.GetType(), methodName, out string commandName))
            {
                return false;
            }

            var options = GetSendOptions(receiver as Component, sendToAllBindings);

            return SendCommandUsingBakedScript(commandName, target, channelID, options, args);
        }

        private bool GetCommandNameAndValidateEntityId(Type targetType, string methodName, out string commandName)
        {
            var currentType = targetType;
            bool foundValidCommandName = false;
            commandName = string.Empty;

            while (currentType != null && !foundValidCommandName)
            {
                commandName = TypeUtils.CommandName(currentType, methodName);

                if (registeredCommandNames.Contains(commandName))
                {
                    foundValidCommandName = true;
                }
                else
                {
                    currentType = currentType.BaseType;
                }
            }

            if (!foundValidCommandName)
            {
                var message = GetErrorMessage(commandName);
                logger.Error(Error.ToolkitSyncCommandInvalidName, message);
                return false;

                string GetErrorMessage(string commandName)
                {
                    if (ReflectionUtils.TryGetMethod(targetType, methodName, BindingFlags.Public | BindingFlags.Instance, out var publicMethod))
                    {
                        if (publicMethod.IsDefined(typeof(CommandAttribute), true))
                        {
                            return $"Unable to send command '{commandName}' to '{Name}': command was not found. Bake again ('coherence > Bake' menu item).";
                        }

                        return $"Unable to send command '{commandName}' to '{Name}': command was not found. Make sure that the method '{methodName}' has been marked as a command on the component of type '{targetType.FullName}' using the Configuration window, or add the [Command] attribute to it. Then, Bake again (coherence > Bake menu item).";
                    }

                    if (ReflectionUtils.MethodExists(targetType, methodName, BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        return $"Unable to send command '{commandName}' to '{Name}': Method '{methodName}' on component of '{targetType.FullName}' needs to be public.";
                    }

                    if (ReflectionUtils.MethodExists(targetType, methodName, BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        return $"Unable to send command '{commandName}' to '{Name}': Method '{methodName}' on component of type '{targetType.FullName}' needs to be non-static.";
                    }

                    return $"Unable to send command '{commandName}' to '{Name}': No method by the name '{methodName}' exists on target type '{targetType.FullName}'. Check that the correct target type and method name are being used when sending the command.";
                }
            }

            // Send commands when disconnected (makes coherence code work for single player games)
            bool allowLocalOfflineCommand = !IsConnected;

            // Validate
            if (EntityID == Entity.InvalidRelative && !allowLocalOfflineCommand)
            {
                logger.Warning(Warning.ToolkitSyncCommandMissingEntity,
                    $"Trying to send command '{commandName}' to '{Name}', which has no associated entity. This message will not be sent. {sync}");
                return false;
            }

            return true;
        }

        private (object[], Type[], bool) ProcessArgs(object[] args)
        {
            if (args == null)
            {
                return (null, null, false);
            }

            object[] finalArgs = new object[args.Length];
            Type[] finalArgTypes = new Type[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                Type argType = default;
                object arg = args[i];

                if (arg == null)
                {
                    logger.Error(Error.ToolkitSyncCommandInvalidArg,
                        $"command argument {i} invalid null argument format. When passing null argument, they must be in default ValueTuple format: (typeof(argument type), (argument type)default)");

                    return (null, null, true);
                }

                // Can't find a better way to know if this is a tuple or not since we want to know if they're using a tuple, but
                // a bad one or a good one.
                argType = arg.GetType();
                if (argType.IsGenericType && argType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
                {
                    try
                    {
                        var tuple = arg;
                        arg = argType.GetField("Item2").GetValue(tuple);
                        argType = (Type)argType.GetField("Item1").GetValue(tuple);
                    }
                    catch
                    {
                        logger.Error(Error.ToolkitSyncCommandInvalidTuple,
                            $"command argument {i} invalid tuple format.  Want: (typeof(object type), object).");

                        return (null, null, true);
                    }
                }
                else
                {
                    arg = args[i];
                    argType = arg.GetType();
                }

                finalArgs[i] = arg;
                finalArgTypes[i] = argType;
            }

            return (finalArgs, finalArgTypes, false);
        }

        // Pure tuples version optimization skipping all reflection.
        private bool SendCommandUsingBakedScript(string commandName, MessageTarget target, ChannelID channelID, SendOptions options, params ValueTuple<Type, object>[] args)
        {
            var finalArgs = new object[args.Length];
            var finalArgTypes = new Type[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                finalArgs[i] = args[i].Item2;
                finalArgTypes[i] = args[i].Item1;
            }

            return SendCommandUsingBakedScript(commandName, target, channelID, options, finalArgs, finalArgTypes);
        }

        // Mixed object version where args could be tuples using reflection.
        private bool SendCommandUsingBakedScript(string commandName, MessageTarget target, ChannelID channelID, SendOptions options, params object[] args)
        {
            var (finalArgs, finalArgTypes, err) = ProcessArgs(args);
            if (err)
            {
                logger.Error(Error.ToolkitSyncCommandErrorProcessingArg,
                    ("command", commandName));

                return false;
            }

            return SendCommandUsingBakedScript(commandName, target, channelID, options, finalArgs, finalArgTypes);
        }

        private bool SendCommandUsingBakedScript(string commandName, MessageTarget target, ChannelID channelID, SendOptions options, object[] args, Type[] types)
        {
            // Validate
            if (!ValidateArgumentTypes(commandName, args, types))
            {
                return false;
            }

            if (IsConnected && !ValidateEntityArgumentsAreInitialized(commandName, args, types))
            {
                return false;
            }

            var sendTo = WhoToSendTo(target);
            if (sendTo.Equals(SendTo.None))
            {
                if (target is MessageTarget.Other && !IsConnected)
                {
                    logger.Warning(Warning.ToolkitCommandBridgeDisconnected,
                        $"Unable to send command {commandName} to other nodes when CoherenceBridge is disconnected");
                }
                return false;
            }

            bool canSendCommand = ValidateBakedCommandSending(commandName, types, target, options, out List<CommandData> commandData);
            if (!canSendCommand)
            {
                return false;
            }

            foreach (var command in commandData)
            {
                if (options.Receiver != null && command.Receiver != options.Receiver)
                {
                    continue;
                }

                // Send
                if (sendTo.HasFlag(SendTo.Self))
                {
                    command.Receive(target, channelID, UnifyNullTypes(args, types));
                }

                if (sendTo.HasFlag(SendTo.Others))
                {
                    // To catch a serialization error.
                    try
                    {
                        command.Send(target, channelID, args);
                    }
                    catch (Exception e)
                    {
                        logger.Error(Error.ToolkitSyncCommandSendException,
                            $"error sending command {commandName} to {target}: {e}");

                        return false;
                    }
                }
            }

            return true;
        }

        // When deserializing these types they are not deserialized as null,
        // so this makes them consistent values when sent locally as well.
        private object[] UnifyNullTypes(object[] args, Type[] types)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == null)
                {
                    var type = types[i];
                    if (type == typeof(string))
                    {
                        args[i] = string.Empty;
                    }
                    else if (type == typeof(Byte[]))
                    {
                        args[i] = Array.Empty<Byte>();
                    }
                }
            }

            return args;
        }

        private SendTo WhoToSendTo(MessageTarget target) => target switch
        {
            MessageTarget.All => IsConnected ? SendTo.All : SendTo.Self,
            MessageTarget.AuthorityOnly => HasStateAuthority ? SendTo.Self : IsConnected ? SendTo.Others : SendTo.None,
            MessageTarget.Other => IsConnected ? SendTo.Others : SendTo.None,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };

        private bool ValidateNumberOfCommands(string commandName, bool sendToAllBindings, List<CommandData> commands)
        {
            if (!sendToAllBindings && commands.Count > 1)
            {
                logger.Error(Error.ToolkitCommandNumCommandBindings,
                    $"Trying to send a single command, but {commandName} is bound to more than one component. " +
                    $"If it is intended, please use the SendCommandToComponentsInChildren method instead.");
                return false;
            }

            //logger.Warning($"There seems to be multiple overloads for the method '{CommandBinding.FullName}', please select a single one for syncing, or use baked script. This message will not be sent.");

            return true;
        }

        private bool ValidateBakedCommandSending(string command, Type[] argsToValidate, MessageTarget messageTarget, SendOptions options, out List<CommandData> data)
        {
            string commandSignature = TypeUtils.CommandNameWithSignatureSuffix(command, argsToValidate);
            if (!commandRequestDataByName.TryGetValue(commandSignature, out data))
            {
                logger.Error(Error.ToolkitCommandBakedSendMissing,
                    GetMessageForInvalidCommand(command, argsToValidate));
                return false;
            }

            if (options.Receiver == null && !ValidateNumberOfCommands(command, options.SendToAllBindings, data))
            {
                return false;
            }

            foreach (var commandData in data)
            {
                if (!CanRouteMessage(messageTarget, commandData.Routing))
                {
                    logger.Error(Error.ToolkitSyncCommandInvalidRouting,
                        ("target", messageTarget),
                        ("routing allowed", commandData.Routing),
                        ("entity", EntityID));

                    return false;
                }
            }

            if (!ValidateOrphan(command, messageTarget))
            {
                return false;
            }

            return true;
        }

        private bool ValidateOrphan(string commandName, MessageTarget target)
        {
            if (target == MessageTarget.AuthorityOnly && sync.IsOrphaned)
            {
                logger.Warning(Warning.ToolkitCommandValidateAuthorityOrphaned,
                    $"Trying to send command '{commandName}' on '{Name}' to {MessageTarget.AuthorityOnly}, but the entity is orphaned. " +
                    $"This command will not be sent.");

                return false;
            }

            return true;
        }

        private bool ValidateEntityArgumentsAreInitialized(string command, object[] args, Type[] argTypes)
        {
            string message = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    continue;
                }

                Entity entity;

                if (argTypes[i] == typeof(CoherenceSync))
                {
                    entity = CoherenceBridge.UnityObjectToEntityId(args[i] as CoherenceSync);
                }
                else if (argTypes[i] == typeof(GameObject))
                {
                    entity = CoherenceBridge.UnityObjectToEntityId(args[i] as GameObject);
                }
                else if (argTypes[i] == typeof(Transform))
                {
                    entity = CoherenceBridge.UnityObjectToEntityId(args[i] as Transform);
                }
                else
                {
                    continue;
                }

                if (entity == Entity.InvalidRelative || entity == default)
                {
                    message ??= $"Unable to send command '{command}' to '{Name}':";

                    message +=
                        $"{Environment.NewLine}Argument '{args[i]}' of type '{argTypes[i]}' is not synchronized with the network." +
                        $" Make sure the object is active in the hierarchy, has a {nameof(CoherenceSync)} component, and you're connected to a Room/World through the CoherenceBridge.";
                }
            }

            if (message != null)
            {
                logger.Warning(Warning.ToolkitCommandValidateArguments, message);
                return false;
            }

            return true;
        }

        private bool ValidateArgumentTypes(string command, object[] args, Type[] argTypes)
        {
            string message = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    continue;
                }

                if (!argTypes[i].IsInstanceOfType(args[i]))
                {
                    message ??= $"Unable to send command '{command}' to '{Name}':";

                    message += $"{Environment.NewLine}Argument '{args[i]}' of type '{args[i].GetType()}' is not an instance of '{argTypes[i]}'. " +
                        $"Check if types and values of given tuples are matching.";
                }
            }

            if (message != null)
            {
                logger.Warning(Warning.ToolkitCommandValidateArgumentTypes, message);
                return false;
            }

            return true;
        }

        private string GetMessageForInvalidCommand(string command, Type[] argsToValidate)
        {
            var commandSignature = TypeUtils.CommandNameWithSignatureSuffix(command, argsToValidate, true);
            var message = $"Unable to send command '{command}' to '{Name}': ";
            if (CommandNameHasBinding(command))
            {
                message += $"command {commandSignature} was not found. Check the command's name and parameters, or Bake again";
            }
            else
            {
                message += "command is not registered. This command will not be sent.";
            }

            return message;
        }

        private bool CommandNameHasBinding(string commandName)
        {
            var simpleCommandName = commandName;
            var dotIndex = commandName.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < commandName.Length - 1)
            {
                simpleCommandName = commandName.Substring(dotIndex + 1);
            }

            return bindings.Exists(b => b.Name == simpleCommandName);
        }

        private List<CommandBinding> GetCommandBindingsMemoized(string commandName)
        {
            if (!commandNameMemoization.TryGetValue(commandName, out var memoized))
            {
                foreach (var binding in bindings)
                {
                    if (binding.IsMethod &&
                        binding.FullName == commandName)
                    {
                        if (!commandNameMemoization.ContainsKey(commandName))
                        {
                            memoized = new List<CommandBinding>();
                            commandNameMemoization.Add(commandName, memoized);
                        }

                        commandNameMemoization[commandName].Add((CommandBinding)binding);
                    }
                }
            }

            if (memoized == null)
            {
                logger.Warning(Warning.ToolkitCommandMissing,
                    $"We couldn't find a method with name '{commandName}' among the bindings on '{Name}'.");
            }

            return memoized;
        }

        static bool CanRouteMessage(MessageTarget target, MessageTarget routing)
        {
            return routing switch
            {
                MessageTarget.AuthorityOnly => target == MessageTarget.AuthorityOnly,
                MessageTarget.All => true,
                MessageTarget.Other => true,
                _ => false
            };
        }
    }
}
