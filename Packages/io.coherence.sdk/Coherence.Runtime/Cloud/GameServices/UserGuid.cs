// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Replaced by " + nameof(CloudUniqueId) + ".")]
    [Deprecated("04/2025", 1, 6, 0, Reason = "Replaced by " + nameof(CloudUniqueId) + ".")]
    [Serializable]
    public record UserGuid : IFormattable
    {
        public static readonly UserGuid None = new("");

#if UNITY
        [UnityEngine.SerializeField]
#endif
        internal string value;
        private UserGuid() => value = "";
        internal UserGuid(string value) => this.value = value ?? "";
        public static implicit operator string(UserGuid userId) => userId.value;
        public static implicit operator CloudUniqueId(UserGuid userId) => new(userId.value);
        public static implicit operator UserGuid(CloudUniqueId userId) => new(userId.value);
        public override string ToString() => value;
        public string ToString(string format, IFormatProvider formatProvider) => value?.ToString(formatProvider) ?? "";
        public static string Serialize(UserGuid userId) => userId.value ?? "";
        public static SessionToken Deserialize(string serializedUserId) => new(serializedUserId);
    }
}
