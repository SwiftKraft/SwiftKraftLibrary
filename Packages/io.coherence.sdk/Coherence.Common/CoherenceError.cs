namespace Coherence.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Log;

    /// <summary>
    /// Represents an error describing an operation that has failed.
    /// <para>
    /// If the error is never observed by the user, it will get automatically logged to the Console.
    /// </para>
    /// </summary>
    /// <typeparam name="TId"> The id of the error. </typeparam>
    public abstract class CoherenceError<TId> : CoherenceError, IEquatable<CoherenceError<TId>> where TId : struct
    {
        private readonly TId type;

        public TId Type
        {
            get
            {
                Ignore();
                return type;
            }
        }

        private protected CoherenceError(TId type, Error error = Error.UnobservedError, bool hasBeenObserved = false) : base(error, hasBeenObserved) => this.type = type;
        private protected CoherenceError(TId type, string message, Error error = Error.UnobservedError, bool hasBeenObserved = false) : base(message, error, hasBeenObserved) => this.type = type;

        public override string ToString() => Message is { Length: > 0 } ? type + ": " + Message : type.ToString();

        public static bool operator ==(CoherenceError<TId> left, TId right) => left is not null && left.Equals(right);
        public static bool operator !=(CoherenceError<TId> left, TId right) => left is null || !left.Equals(right);
        public override int GetHashCode() => type.GetHashCode();

        public bool Equals(CoherenceError<TId> other)
            => other is not null && EqualityComparer<TId>.Default.Equals(Type, other.Type);

        public override bool Equals(object obj)
            => obj is CoherenceError<TId> coherenceError && Equals(coherenceError);
        public bool Equals(TId id) => EqualityComparer<TId>.Default.Equals(Type, id);
    }

    /// <summary>
    /// Represents an error describing an operation that has failed.
    /// <para>
    /// If the error is never observed by the user, it will get automatically logged to the Console.
    /// </para>
    /// </summary>
    public abstract class CoherenceError
    {
        private readonly string message;
        private readonly Error error;
        private bool hasBeenObserved;
        protected DateTime timestamp;

        /// <summary>
        /// Message describing the error.
        /// </summary>
        public string Message
        {
            get
            {
                OnObserved();
                return message;
            }
        }

        private protected CoherenceError(Error error = Error.UnobservedError, bool hasBeenObserved = false) : this(error.GetText(), error, hasBeenObserved) { }
        private protected CoherenceError(string message, Error error = Error.UnobservedError, bool hasBeenObserved = false)
        {
            this.message = message;
            this.error = error;
            timestamp = DateTime.Now;
            if (hasBeenObserved)
            {
                OnObserved();
            }
        }

        /// <summary>
        /// Ignore this error.
        /// <remarks>
        /// If this method is called, the error will not get logged to the Console automatically, even if its
        /// <see cref="Message"/> is never observed.
        /// </remarks>
        /// </summary>
        public void Ignore() => OnObserved();

        /// <summary>
        /// Converts the error into an <see cref="Exception"/> and <see langword="throw">throws</see> it.
        /// </summary>
        /// <exception cref="Exception"> Always throws an exception. </exception>
        [DoesNotReturn]
        public void Throw() => throw ToException();

        /// <summary>
        /// Logs the error into the Console.
        /// </summary>
        public void Log() => Log(ToString());
        private void Log(string message) => Coherence.Log.Log.GetLogger<CoherenceError>().Error(error, message);

        public override string ToString()
        {
            OnObserved();
            return Message is { Length: > 0 } ? GetType().Name + ": " + Message : GetType().Name;
        }

        public static implicit operator Exception(CoherenceError error) => error.ToException();

        private protected virtual Exception ToException() => new(ToString());

        private void OnObserved()
        {
            hasBeenObserved = true;
            GC.SuppressFinalize(this);
        }

        ~CoherenceError()
        {
            if (!hasBeenObserved)
            {
                var secondsAgo = (DateTime.Now - timestamp).TotalSeconds;
                string messagePrefix;
                if (secondsAgo < 3d)
                {
                    messagePrefix = "An unhandled error occurred:\n";
                }
                else
                {
                    messagePrefix = "An unhandled error occurred ";
                    if (secondsAgo < 60d)
                    {
                        messagePrefix += secondsAgo.ToString("F0") + " seconds";
                    }
                    else
                    {
                        messagePrefix += (secondsAgo / 60d).ToString("F0") + " minutes";
                    }

                    messagePrefix += " ago (at " + timestamp.ToString("HH:mm:ss") + "):\n";
                }

                Log(messagePrefix + ToString());
            }
        }
    }
}
