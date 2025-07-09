// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using UnityEngine;

    [System.Serializable]
    public sealed class SchemaAsset : ScriptableObject, System.IComparable<SchemaAsset>
    {
        public string raw = string.Empty;

        public string identifier = string.Empty;
        public SchemaDefinition SchemaDefinition;

        public int CompareTo(SchemaAsset other)
        {
            if (!other)
            {
                return 0;
            }

            return identifier.CompareTo(other.identifier);
        }
    }
}

