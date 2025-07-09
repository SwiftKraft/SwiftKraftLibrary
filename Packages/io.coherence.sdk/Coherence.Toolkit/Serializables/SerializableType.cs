// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;
    
    [Serializable]
    public class SerializableType : IEquatable<SerializableType>
    {
        public SerializableType(Type type)
        {
            assemblyQualifiedName = type.AssemblyQualifiedName;
        }
        
        public SerializableType(string assemblyQualifiedName)
        {
            this.assemblyQualifiedName = assemblyQualifiedName;
        }
        
        public static implicit operator Type(SerializableType type) => type.ToType;
        public static explicit operator SerializableType(Type type) => new SerializableType(type);

        public Type ToType => cachedType ??= Type.GetType(assemblyQualifiedName);

        public string AssemblyQualifiedName => assemblyQualifiedName;
  
        [SerializeField] 
        private string assemblyQualifiedName;

        private Type cachedType;
        
        public static bool operator ==(SerializableType obj1, SerializableType obj2)
        {
            if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
            {
                return false;
            }

            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            
            return obj1.Equals(obj2);
        }
        
        public static bool operator !=(SerializableType obj1, SerializableType obj2) => !(obj1 == obj2);
        
        public override bool Equals(object obj) => obj is SerializableType o && Equals(o);

        public bool Equals(SerializableType other) => other != null && GetHashCode() == other.GetHashCode();

        public override int GetHashCode()
        {
            var hashCode = 0;
            hashCode ^= 13 * (!string.IsNullOrEmpty(assemblyQualifiedName) ? assemblyQualifiedName.GetHashCode() : 0);
            return hashCode;
        }
    }
}
