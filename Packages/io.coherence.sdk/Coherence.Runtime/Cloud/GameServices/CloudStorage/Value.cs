// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Utils;

    /// <summary>
    /// Represents the value of single <see cref="StorageItem"/> that can be saved in <see cref="CloudStorage"/>
    /// as part of a <see cref="StorageObject">storage object</see>.
    /// </summary>
    /// <example>
    /// <code source="Runtime/CloudStorage/ValueExample.cs" language="csharp"/>
    /// </example>
    internal readonly struct Value : IEquatable<Value>
    {
        /// <summary>
        /// Represents no value.
        /// </summary>
        public static readonly Value None = default;

        /// <summary>
        /// The maximum number of characters that the value's json representation can contain.
        /// </summary>
        internal const int MaxJsonLength = 4096;

        internal string Json { get; }
        private (bool hasValue, object value) ValueThatWasSerialized { get; }

        /// <summary>
        /// Converts the serialized value into an object of type <see typeparamref="T"/>.
        /// </summary>
        /// <typeparam name="T"> Type of the result. </typeparam>
        /// <returns> A new object of type <see typeparamref="T"/>. </returns>
        /// <exception cref="StorageException">
        /// Thrown if the value cannot be deserialized as <see typeparamref="T"/>.
        /// </exception>
        public T As<T>() => As(out T result) ? result : throw new StorageException(StorageErrorType.InvalidValue, $"Failed to deserialize value '{Json}' as {typeof(T).Name}.");

        /// <summary>
        /// Converts the serialized value into an object of type <see typeparamref="T"/>.
        /// </summary>
        /// <param name="value">
        /// The deserialized value, if the value could be deserialized as <see typeparamref="T"/>;
        /// otherwise, the default value of <see typeparamref="T"/>.
        /// </param>
        /// <typeparam name="T">
        /// Type of the result.
        /// </typeparam>
        /// <returns>
        /// <see langword="true"/> if the value could be deserialized into an object of type <see typeparamref="T"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool As<T>(out T value)
        {
            if (ValueThatWasSerialized.hasValue)
            {
                if (ValueThatWasSerialized.value is T t)
                {
                    value = t;
                    return true;
                }

                if (ValueThatWasSerialized.value is null && !typeof(T).IsValueType)
                {
                    value = default;
                    return true;
                }

                value = default;
                return false;
            }

            if (typeof(T) == typeof(string))
            {
                value = (T)(object)Json;
                return true;
            }

            try
            {
                value = FromJson<T>(Json);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="string"/> value that the created instance represents. </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see langword="string"/> is contains more than <see cref="MaxJsonLength"/> characters.
        /// </exception>
        public Value([AllowNull] string value) : this(value, value ?? "null") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="bool"/> value that the created instance represents. </param>
        public Value(bool value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="int"/> value that the created instance represents. </param>
        public Value(int value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="float"/> value that the created instance represents. </param>
        public Value(float value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="double"/> value that the created instance represents. </param>
        public Value(double value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="byte"/> value that the created instance represents. </param>
        public Value(byte value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="short"/> value that the created instance represents. </param>
        public Value(short value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="enum"/> value that the created instance represents. </param>
        public Value(Enum value) : this(value, ToJson(value)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> struct.
        /// </summary>
        /// <param name="value"> The <see langword="object"/> value that the created instance represents. </param>
        public Value(object value) : this(value, Serialize(value)) { }

        internal Value([AllowNull] object value, [DisallowNull] string json)
        {
            ValueThatWasSerialized = new(true, value);
            Json = json;

            #if UNITY_ASSERTIONS
            if (Json is null)
            {
                throw GetException("Value's string representation cannot be null.");
            }

            if (Json.Length > MaxJsonLength)
            {
                throw GetException($"Value cannot be longer than {MaxJsonLength} characters long.\nInvalid Value:\"{Json}\"");
            }
            #endif
        }

        private Value([DisallowNull] string json, bool hasValue)
        {
            ValueThatWasSerialized = new(hasValue, null);
            Json = json;

            #if UNITY_ASSERTIONS
            if (Json is null)
            {
                throw GetException("Value's string representation cannot be null.");
            }

            if (Json.Length > MaxJsonLength)
            {
                throw GetException($"Value cannot be longer than {MaxJsonLength} characters long.\nInvalid Value:\"{Json}\"");
            }
            #endif
        }

        internal static Value FromJson(string json) => new(json, false);

        private static string Serialize(object content)
        {
            if (content is string text)
            {
                return text;
            }

            try
            {
                return ToJson(content);
            }
            catch (Exception ex)
            {
                throw GetException($"Failed to serialize value of type {content?.GetType().Name ?? "null"}. Make sure that the type has the [Serializable] attribute and is supported by Unity's JsonUtility.", ex);
            }
        }

        private static string ToJson(object content) => CoherenceJson.SerializeObject(content, StorageObject.jsonConverters);
        private static T FromJson<T>(string json) => CoherenceJson.DeserializeObject<T>(json, StorageObject.jsonConverters);
        private static StorageException GetException(string message, Exception innerException = null) => new(StorageErrorType.InvalidValue, message, innerException);

        public bool ValueEquals<T>(T other) => As(out T thisValue) && EqualityComparer<T>.Default.Equals(thisValue, other);
        public bool Equals(Value other) => string.Equals(Json, other.Json);
        public override bool Equals(object obj) => obj is Value other && Equals(other);
        public override int GetHashCode() => (Json is not null ? Json.GetHashCode() : 0);
        public override string ToString() => Json;

        public static bool operator ==(Value left, Value right) => left.Equals(right);
        public static bool operator !=(Value left, Value right) => !left.Equals(right);
        public static bool operator ==(Value left, string right) => string.Equals(left.Json, right);
        public static bool operator !=(Value left, string right) => !string.Equals(left.Json, right);
        public static bool operator ==(Value left, bool right) => left.As(out bool leftValue) && leftValue.Equals(right);
        public static bool operator !=(Value left, bool right) => !left.As(out bool leftValue) && leftValue.Equals(right);
        public static bool operator ==(Value left, int right) => left.As(out int leftValue) && leftValue.Equals(right);
        public static bool operator !=(Value left, int right) => !left.As(out int leftValue) && leftValue.Equals(right);
        public static bool operator ==(Value left, byte right) => left.As(out byte leftValue) && leftValue.Equals(right);
        public static bool operator !=(Value left, byte right) => !left.As(out byte leftValue) && leftValue.Equals(right);
        public static bool operator ==(Value left, Enum right) => left.As(out Enum leftValue) && leftValue.Equals(right);
        public static bool operator !=(Value left, Enum right) => !left.As(out Enum leftValue) && leftValue.Equals(right);

        public static implicit operator string(Value value) => value.Json;
        public static implicit operator bool(Value value) => value.As(out bool result) ? result : throw new InvalidCastException($"Failed not cast {value} to bool.");
        public static implicit operator double(Value value) => value.As(out double result) ? result : throw new InvalidCastException($"Failed not cast {value} to double.");
        public static implicit operator float(Value value) => value.As(out float result) ? result : throw new InvalidCastException($"Failed not cast {value} to float.");
        public static implicit operator int(Value value) => value.As(out int result) ? result : throw new InvalidCastException($"Failed not cast {value} to int.");
        public static implicit operator short(Value value) => value.As(out short result) ? result : throw new InvalidCastException($"Failed not cast {value} to short.");
        public static implicit operator byte(Value value) => value.As(out byte result) ? result : throw new InvalidCastException($"Failed not cast {value} to byte.");
        public static implicit operator Enum(Value value) => value.As(out Enum result) ? result : throw new InvalidCastException($"Failed not cast {value} to Enum.");

        public static implicit operator Value(string value) => new(value);
        public static implicit operator Value(bool value) => new(value);
        public static implicit operator Value(int value) => new(value);
        public static implicit operator Value(float value) => new(value);
        public static implicit operator Value(double value) => new(value);
        public static implicit operator Value(byte value) => new(value);
        public static implicit operator Value(short value) => new(value);
        public static implicit operator Value(Enum value) => new(value);
    }
}
