// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER

namespace Coherence.Log
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Linq;
    using Object = UnityEngine.Object;

    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Coherence.Log.Targets;

    public class UnityLogger : Logger
    {
        public delegate void OnLogEvent(object context, string message, params (string key, object value)[] args);
        public static OnLogEvent OnLogTraceEvent;
        public static OnLogEvent OnLogDebugEvent;
        public static OnLogEvent OnLogInfoEvent;
        public static OnLogEvent OnLogWarningEvent;

        public static OnLogEvent OnLogErrorEvent;

        private Object unityLogContext;

        public UnityLogger(Type source = null, IEnumerable<ILogTarget> logTargets = null) : base(source, null, logTargets)
        {
        }

        public override Logger With<TSource>()
        {
            return With(typeof(TSource));
        }

        public override Logger With(Type source)
        {
            var newLogger = new UnityLogger(source, LogTargets);
            newLogger.WithLogger = this;
            newLogger.Context = Context;
            newLogger.UseWatermark = UseWatermark;

            return newLogger;
        }

        public override Logger WithArgs(params (string key, object value)[] args)
        {
            if (args.Length > 0 && args[0].value is Object unityObject)
            {
                unityLogContext = unityObject;
                return base.WithArgs(args.Skip(1).ToArray());
            }

            return base.WithArgs(args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Trace(string log, params (string key, object value)[] args)
        {
            base.Trace(log, args);
            OnLogTraceEvent?.Invoke(Context, log, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Debug(string log, params (string key, object value)[] args)
        {
            base.Debug(log, args);
            OnLogDebugEvent?.Invoke(Context, log, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Info(string log, params (string key, object value)[] args)
        {
            base.Info(log, args);
            OnLogInfoEvent?.Invoke(Context, log, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Warning(Warning id, params (string key, object value)[] args)
        {
            args = AppendLogID(args, id);

            base.Warning(id, args);

            var log = id.GetText();

            OnLogWarningEvent?.Invoke(Context, log, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Warning(Warning id, string msg, params (string key, object value)[] args)
        {
            args = AppendLogID(args, id);

            base.Warning(id, msg, args);

            OnLogWarningEvent?.Invoke(Context, msg, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Error(Error id, params (string key, object value)[] args)
        {
            args = AppendLogID(args, id);

            base.Error(id, args);

            var log = id.GetText();

            OnLogErrorEvent?.Invoke(Context, log, args);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public override void Error(Error id, string msg, params (string key, object value)[] args)
        {
            args = AppendLogID(args, id);

            base.Error(id, msg, args);

            OnLogErrorEvent?.Invoke(Context, msg, args);
        }

        internal Object GetUnityLogContext()
        {
            if (unityLogContext != null)
            {
                return unityLogContext;
            }

            if (WithLogger == null || !(WithLogger is UnityLogger))
            {
                return null;
            }

            return ((UnityLogger)WithLogger).GetUnityLogContext();
        }
    }
}

#endif // UNITY_5_3_OR_NEWER
