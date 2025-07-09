// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;

    public partial class CoherenceSync
    {
        /// <summary>
        ///     Send a command to another client to call a method on one of its components.
        /// </summary>
        /// <typeparam name="TTarget">The type of the Unity component that contains the method to call.</typeparam>
        /// <param name="methodName">The name of the method to call. Tip: use C#'s nameof() operator to avoid name mismatches.</param>
        /// <param name="target">To which clients should this command be sent.</param>
        /// <returns>true if the sending was successful.</returns>
        public bool SendCommand<TTarget>(string methodName, MessageTarget target) where TTarget : Component
        {
            return commandsHandler.SendCommand(typeof(TTarget), methodName, target, ChannelID.Default, false, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)"/>
        /// <param name="targetType">
        ///     <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)" path="/typeparam[@name='TTarget']"/>
        /// </param>
        public bool SendCommand(Type targetType, string methodName, MessageTarget target)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, false, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)"/>
        /// <param name="args">The arguments to send to the method, make sure they match its signature.</param>
        public bool SendCommand<TTarget>(string methodName, MessageTarget target, params object[] args) where TTarget : Component
        {
            return SendCommand(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, object[])"/>
        /// <param name="targetType">
        ///     <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, object[])" path="/typeparam[@name='TTarget']"/>
        /// </param>
        public bool SendCommand(Type targetType, string methodName, MessageTarget target, params object[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, false, args);
        }

        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)"/>
        /// <param name="args">
        ///     The arguments to send to the method, make sure they match its signature.
        ///     This version of SendCommand uses tuples with the type and the value of each argument,
        ///     this enables sending of null values.
        /// </param>
        public bool SendCommand<TTarget>(string methodName, MessageTarget target, params ValueTuple<Type, object>[] args) where TTarget : Component
        {
            return SendCommand(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])"/>
        /// <param name="targetType">
        ///     <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])" path="/typeparam[@name='TTarget']"/>
        /// </param>
        public bool SendCommand(Type targetType, string methodName, MessageTarget target, params ValueTuple<Type, object>[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, false, args);
        }

        /// <summary>
        ///     Send a command to another client to call a method to every component in the hierarchy that has methodName bound.
        /// </summary>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)"/>
        public bool SendCommandToChildren<TTarget>(string methodName, MessageTarget target) where TTarget : Component
        {
            return SendCommandToChildren(typeof(TTarget), methodName, target, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget)"/>
        public bool SendCommandToChildren(Type targetType, string methodName, MessageTarget target)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, true, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, object[])"/>
        public bool SendCommandToChildren<TTarget>(string methodName, MessageTarget target, params object[] args) where TTarget : Component
        {
            return SendCommandToChildren(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget, object[])"/>
        public bool SendCommandToChildren(Type targetType, string methodName, MessageTarget target, params object[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, true, args);
        }

        /// <inheritdoc cref="SendCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendCommandToChildren<TTarget>(string methodName, MessageTarget target, params ValueTuple<Type, object>[] args) where TTarget : Component
        {
            return SendCommandToChildren(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendCommandToChildren(Type targetType, string methodName, MessageTarget target, params ValueTuple<Type, object>[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Default, true, args);
        }

        /// <summary>
        ///     Send a command to another client to call a method in a specific component, if you have more than one component method bound in the same hierarchy.
        /// </summary>
        /// <param name="action">Action instance of the component and method command that you wish to send</param>
        /// <param name="target">To which clients should this command be sent.</param>
        /// <returns>true if the sending was successful.</returns>
        public bool SendCommand(Action action, MessageTarget target)
        {
            return commandsHandler.SendCommand(action, target, ChannelID.Default);
        }

        /// <inheritdoc cref="SendCommand(Action, MessageTarget)"/>
        /// <param name="param1">Argument of type T1 to send to the method</param>
        public bool SendCommand<T1>(Action<T1> action, MessageTarget target, T1 param1)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1}(Action{T1}, MessageTarget, T1)"/>
        /// <param name="param2">Argument of type T2 to send to the method</param>
        public bool SendCommand<T1, T2>(Action<T1, T2> action, MessageTarget target, T1 param1, T2 param2)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2}(Action{T1, T2}, MessageTarget, T1, T2)"/>
        /// <param name="param3">Argument of type T3 to send to the method</param>
        public bool SendCommand<T1, T2, T3>(Action<T1, T2, T3> action, MessageTarget target, T1 param1, T2 param2, T3 param3)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2, T3}(Action{T1, T2, T3}, MessageTarget, T1, T2, T3)"/>
        /// <param name="param4">Argument of type T4 to send to the method</param>
        public bool SendCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2, T3, T4}(Action{T1, T2, T3, T4}, MessageTarget, T1, T2, T3, T4)"/>
        /// <param name="param5">Argument of type T5 to send to the method</param>
        public bool SendCommand<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2, T3, T4, T5}(Action{T1, T2, T3, T4, T5}, MessageTarget, T1, T2, T3, T4, T5)"/>
        /// <param name="param6">Argument of type T6 to send to the method</param>
        public bool SendCommand<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2, T3, T4, T5, T6}(Action{T1, T2, T3, T4, T5, T6}, MessageTarget, T1, T2, T3, T4, T5, T6)"/>
        /// <param name="param7">Argument of type T7 to send to the method</param>
        public bool SendCommand<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 },
                new ValueTuple<Type, object>() { Item1 = typeof(T7), Item2 = param7 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <inheritdoc cref="SendCommand{T1, T2, T3, T4, T5, T6, T7}(Action{T1, T2, T3, T4, T5, T6, T7}, MessageTarget, T1, T2, T3, T4, T5, T6, T7)"/>
        /// <param name="param8">Argument of type T8 to send to the method</param>
        public bool SendCommand<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 },
                new ValueTuple<Type, object>() { Item1 = typeof(T7), Item2 = param7 },
                new ValueTuple<Type, object>() { Item1 = typeof(T8), Item2 = param8 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Default, args);
        }

        /// <summary>
        ///     Send an ordered command to another client to call a method on one of its components.
        ///     Command being ordered means that it will arrive on other clients in the same order relative to other
        ///     sent ordered commands (and other ordered changes) from this client.
        /// </summary>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget)"/>
        public bool SendOrderedCommand<TTarget>(string methodName, MessageTarget target) where TTarget : Component
        {
            return commandsHandler.SendCommand(typeof(TTarget), methodName, target, ChannelID.Ordered, false, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendOrderedCommand{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget)"/>
        public bool SendOrderedCommand(Type targetType, string methodName, MessageTarget target)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, false, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendOrderedCommand{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, object[])"/>
        public bool SendOrderedCommand<TTarget>(string methodName, MessageTarget target, params object[] args) where TTarget : Component
        {
            return SendOrderedCommand(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget, object[])"/>
        public bool SendOrderedCommand(Type targetType, string methodName, MessageTarget target, params object[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, false, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendOrderedCommand<TTarget>(string methodName, MessageTarget target, params ValueTuple<Type, object>[] args) where TTarget : Component
        {
            return SendOrderedCommand(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendOrderedCommand(Type targetType, string methodName, MessageTarget target, params ValueTuple<Type, object>[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, false, args);
        }

        /// <summary>
        ///     Send an ordered command to another client to call a method to every component in the hierarchy that has methodName bound.
        ///     Command being ordered means that it will arrive on other clients in the same order relative to other
        ///     sent ordered commands (and other ordered changes) from this client.
        /// </summary>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget)"/>
        public bool SendOrderedCommandToChildren<TTarget>(string methodName, MessageTarget target) where TTarget : Component
        {
            return SendOrderedCommandToChildren(typeof(TTarget), methodName, target, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendOrderedCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget)"/>
        public bool SendOrderedCommandToChildren(Type targetType, string methodName, MessageTarget target)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, true, Array.Empty<object>());
        }

        /// <inheritdoc cref="SendOrderedCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, object[])"/>
        public bool SendOrderedCommandToChildren<TTarget>(string methodName, MessageTarget target, params object[] args) where TTarget : Component
        {
            return SendOrderedCommandToChildren(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendOrderedCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget, object[])"/>
        public bool SendOrderedCommandToChildren(Type targetType, string methodName, MessageTarget target, params object[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, true, args);
        }

        /// <inheritdoc cref="SendOrderedCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand{TTarget}(string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendOrderedCommandToChildren<TTarget>(string methodName, MessageTarget target, params ValueTuple<Type, object>[] args) where TTarget : Component
        {
            return SendOrderedCommandToChildren(typeof(TTarget), methodName, target, args);
        }

        /// <inheritdoc cref="SendOrderedCommandToChildren{TTarget}(string, MessageTarget)" path="/summary"/>
        /// <inheritdoc cref="SendCommand(Type, string, MessageTarget, ValueTuple{Type, object}[])"/>
        public bool SendOrderedCommandToChildren(Type targetType, string methodName, MessageTarget target, params ValueTuple<Type, object>[] args)
        {
            return commandsHandler.SendCommand(targetType, methodName, target, ChannelID.Ordered, true, args);
        }

        /// <summary>
        ///     Send an ordered command to another client to call a method in a specific component, if you have more than one component method bound in the same hierarchy.
        ///     Command being ordered means that it will arrive on other clients in the same order relative to other
        ///     sent ordered commands (and other ordered changes) from this client.
        /// </summary>
        /// <inheritdoc cref="SendCommand(Action, MessageTarget)"/>
        public bool SendOrderedCommand(Action action, MessageTarget target)
        {
            return commandsHandler.SendCommand(action, target, ChannelID.Ordered);
        }

        /// <inheritdoc cref="SendOrderedCommand(Action, MessageTarget)"/>
        /// <param name="param1">Argument of type T1 to send to the method</param>
        public bool SendOrderedCommand<T1>(Action<T1> action, MessageTarget target, T1 param1)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1}(Action{T1}, MessageTarget, T1)"/>
        /// <param name="param2">Argument of type T2 to send to the method</param>
        public bool SendOrderedCommand<T1, T2>(Action<T1, T2> action, MessageTarget target, T1 param1, T2 param2)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2}(Action{T1, T2}, MessageTarget, T1, T2)"/>
        /// <param name="param3">Argument of type T3 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3>(Action<T1, T2, T3> action, MessageTarget target, T1 param1, T2 param2, T3 param3)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2, T3}(Action{T1, T2, T3}, MessageTarget, T1, T2, T3)"/>
        /// <param name="param4">Argument of type T4 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2, T3, T4}(Action{T1, T2, T3, T4}, MessageTarget, T1, T2, T3, T4)"/>
        /// <param name="param5">Argument of type T5 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2, T3, T4, T5}(Action{T1, T2, T3, T4, T5}, MessageTarget, T1, T2, T3, T4, T5)"/>
        /// <param name="param6">Argument of type T6 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2, T3, T4, T5, T6}(Action{T1, T2, T3, T4, T5, T6}, MessageTarget, T1, T2, T3, T4, T5, T6)"/>
        /// <param name="param7">Argument of type T7 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 },
                new ValueTuple<Type, object>() { Item1 = typeof(T7), Item2 = param7 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }

        /// <inheritdoc cref="SendOrderedCommand{T1, T2, T3, T4, T5, T6, T7}(Action{T1, T2, T3, T4, T5, T6, T7}, MessageTarget, T1, T2, T3, T4, T5, T6, T7)"/>
        /// <param name="param8">Argument of type T8 to send to the method</param>
        public bool SendOrderedCommand<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, MessageTarget target, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8)
        {
            var args = new ValueTuple<Type, object>[]
            {
                new ValueTuple<Type, object>() { Item1 = typeof(T1), Item2 = param1 },
                new ValueTuple<Type, object>() { Item1 = typeof(T2), Item2 = param2 },
                new ValueTuple<Type, object>() { Item1 = typeof(T3), Item2 = param3 },
                new ValueTuple<Type, object>() { Item1 = typeof(T4), Item2 = param4 },
                new ValueTuple<Type, object>() { Item1 = typeof(T5), Item2 = param5 },
                new ValueTuple<Type, object>() { Item1 = typeof(T6), Item2 = param6 },
                new ValueTuple<Type, object>() { Item1 = typeof(T7), Item2 = param7 },
                new ValueTuple<Type, object>() { Item1 = typeof(T8), Item2 = param8 }
            };

            return commandsHandler.SendCommand(action, target, ChannelID.Ordered, args);
        }
    }
}
