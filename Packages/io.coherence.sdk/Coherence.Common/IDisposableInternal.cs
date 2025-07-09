// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Common
{
    using System;
    using Log;

    /// <summary>
    /// Extends the IDisposable interface with functionality to detect and help debug resource leaks.
    /// </summary>
    internal interface IDisposableInternal : IDisposable
    {
        /// <summary>
        /// Text describing the context in which initialization is currently taking place.
        /// </summary>
        internal static string CurrentInitializationContext { get; private set; }

        /// <summary>
        /// Sets a text describing the context for <see cref="IDisposable"/> objects being created.
        /// </summary>
        /// <param name="context">
        /// Text describing the context in which initialization of objects is currently taking place.
        /// <para>
        /// For example the name of the test that is being executed.
        /// </para>
        /// </param>
        internal static void SetCurrentInitializationContext(string context) => CurrentInitializationContext = context;

        /// <summary>
        /// Text describing the context in which this object was initialized.
        /// </summary>
        /// <seealso cref="DisposableInternalExtensions.OnInitialized"/>
        string InitializationContext { get; set; }

        /// <summary>
        /// Stack trace of the initialization of this object.
        /// </summary>
        /// <seealso cref="DisposableInternalExtensions.OnInitialized"/>
        string InitializationStackTrace { get; set; }

        /// <summary>
        /// Contains a value indicating whether <see cref="IDisposable.Dispose"/> has been executed on this object.
        /// </summary>
        /// <seealso cref="DisposableInternalExtensions.OnDisposed"/>
        /// <seealso cref="DisposableInternalExtensions.OnFinalized"/>
        bool IsDisposed { get; set; }
    }

    internal static class DisposableInternalExtensions
    {
        /// <summary>
        /// Gets a warning message that can be logged to the Console if the finalizer of an <seealso cref="IDisposable"/>
        /// object is executed without <seealso cref="IDisposable.Dispose"/> having been called.
        /// </summary>
        /// <param name="instance"> The <seealso cref="IDisposable"/> object that wasn't disposed properly. </param>
        internal static string GetResourceLeakWarningMessage(this IDisposableInternal instance)
        {
            return $"Finalizer ~{instance.GetType().Name} executed without Dispose having been called. This is a potential resource leak.\n" +
                $"Initialization Context: {instance.InitializationContext}\n" +
                $"Initialization Stack Trace: {instance.InitializationStackTrace}";
        }

        /// <summary>
        /// Sets the <see cref="IDisposableInternal.InitializationContext"/> of the object to the current context.
        /// </summary>
        /// <remarks>
        /// Call this in the constructor of objects that implement <see cref="IDisposableInternal"/>.
        /// </remarks>
        internal static void OnInitialized(this IDisposableInternal instance)
        {
            instance.InitializationContext = IDisposableInternal.CurrentInitializationContext;

#if DEBUG
            instance.InitializationStackTrace = Environment.StackTrace;
#else
            instance.InitializationStackTrace = "<Stack trace available only in debug mode>";
#endif
        }

        /// <summary>
        /// If the object has not been disposed yet, then marks the object as disposed, suppresses finalization
        /// and returns <see langword="true"/>; otherwise, just returns <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// Call this in the <see cref="IDisposable.Dispose"/> method of objects that implement <see cref="IDisposableInternal"/>.
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if the object has been disposed already; otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool OnDisposed(this IDisposableInternal instance)
        {
            if (instance.IsDisposed)
            {
                return true;
            }

            instance.IsDisposed = true;
            GC.SuppressFinalize(instance);
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed, and executes <see cref="IDisposable.Dispose"/> if not.
        /// </summary>
        /// <remarks>
        /// Call this in the finalizer of objects that implement <see cref="IDisposableInternal"/>.
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if the object has been disposed; otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool OnFinalized(this IDisposableInternal instance)
        {
            if (instance.IsDisposed)
            {
                return true;
            }

            try
            {
                instance.Dispose();
            }
            catch (Exception e)
            {
                using var logger = Log.GetLogger(instance.GetType(), instance);
                logger.Error(Error.CommonIDisposibleFinalize,
                    $"An exception occurred while finalizing {instance.GetType().Name} which had not been disposed.",
                    ("InitializationContext", instance.InitializationContext),
                    ("InitializationStackTrace", instance.InitializationStackTrace),
                    ("Exception", e));
            }

            return false;
        }
    }
}
