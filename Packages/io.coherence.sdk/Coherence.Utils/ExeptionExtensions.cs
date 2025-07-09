// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Extension methods for <see cref="Exception"/>.
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Tries to extract an exception of type <typeparamref name="TException"/> from the provided <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception"> The exception to extract from. </param>
        /// <param name="result">
        /// When this method returns, contains the extracted exception of type <typeparamref name="TException"/>.
        /// <para>
        /// This will be the provided <paramref name="exception"/> itself, if it is of type <typeparamref name="TException"/>,
        /// an inner exception of the provided <paramref name="exception"/>, if any them are of type <typeparamref name="TException"/>;
        /// otherwise, <see langword="null"/>.
        /// </para>
        /// </param>
        /// <returns> <see langword="true"/> if a result was found; otherwise, <see langword="false"/>. </returns>
        public static bool TryExtract<TException>([AllowNull] this Exception exception, [NotNullWhen(true), MaybeNullWhen(false)] out TException result) where TException : Exception
        {
            if (exception is null)
            {
                result = null;
                return false;
            }

            if (exception is TException converted)
            {
                result = converted;
                return true;
            }

            if (exception is AggregateException aggregateException)
            {
                foreach (var item in aggregateException.InnerExceptions)
                {
                    if (TryExtract(item, out result))
                    {
                        return true;
                    }
                }

                result = null;
                return false;
            }

            if (exception.InnerException is { } innerException)
            {
                return TryExtract(innerException, out result);
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Is the exception an <see cref="OperationCanceledException"/>, an <see cref="AggregateException"/> containing one,
        /// or an exception whose inner exception is an an <see cref="OperationCanceledException"/>?
        /// </summary>
        public static bool WasCanceled([AllowNull] this Exception exception) => exception.TryExtract<OperationCanceledException>(out _);
    }
}
