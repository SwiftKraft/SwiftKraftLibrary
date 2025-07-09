// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    // WARNING!! Do not INSERT or REMOVE types, only add to the end for new ones or rename for old
    // ones being deprecated or obsoleted.
    // These are serialized as numbers so changing the order will break existing projects.
    public enum SchemaType
    {
        Unknown,
        Int,
        Int64,
        Bool,
        Float,
        String,
        Vector2,
        Vector3,
        Quaternion,
        Entity,
        Bytes,
        Color,
        UInt,
        UInt64,
        Float64,
        Int8,
        UInt8,
        Int16,
        UInt16,
        Char
    }
}
