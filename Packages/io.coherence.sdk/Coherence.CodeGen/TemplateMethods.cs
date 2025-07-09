// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.CodeGen
{
    using System.Collections.Generic;

    internal static class TemplateMethods
    {
        public static string GetSerializeMethod(string type)
        {
            switch (type)
            {
                case "System.Byte":
                    return "Byte";
                case "System.SByte":
                    return "SByte";
                case "System.Int16":
                    return "Short";
                case "System.UInt16":
                    return "UShort";
                case "System.Char":
                    return "Char";
                case "System.Int32":
                    return "IntegerRange";
                case "System.UInt32":
                    return "UIntegerRange";
                case "System.Int64":
                    return "Long";
                case "System.UInt64":
                    return "ULong";
                case "System.Double":
                    return "Double";
                case "System.Single":
                    return "Float";
                case "Vector2":
                case "System.Numerics.Vector2":
                    return "Vector2";
                case "Vector3":
                case "System.Numerics.Vector3":
                    return "Vector3";
                case "Color":
                case "System.Numerics.Vector4":
                    return "Color";
                case "Quaternion":
                case "System.Numerics.Quaternion":
                    return "Quaternion";
                case "System.String":
                    return "ShortString";
                case "System.Boolean":
                    return "Bool";
                case "Transform":
                case "Coherence.Toolkit.CoherenceSync":
                case "GameObject":
                case "Entity":
                    return "Entity";
                case "System.Byte[]":
                    return "BytesList";
            }

            return string.Empty;
        }

        public static string GetDefaultSerializeParameters(string type, bool addComma)
        {
            var result = string.Empty;

            switch (type)
            {
                case "System.Int32":
                    result = "32, -2147483648";
                    break;
                case "System.UInt32":
                    result = "32, 0";
                    break;
                case "System.Single":
                case "Vector3":
                case "System.Numerics.Vector3":
                case "Vector2":
                case "System.Numerics.Vector2":
                case "Color":
                case "System.Numerics.Vector4":
                    result = "FloatMeta.NoCompression()";
                    break;
                case "Quaternion":
                case "System.Numerics.Quaternion":
                    result = "32";
                    break;
            }

            return !string.IsNullOrEmpty(result) ? $"{(addComma ? ", " : string.Empty)}{result}" : result;
        }

        public static string GetSerializeParametersFromOverrides(string type, Dictionary<string, string> overrides,
            bool addComma)
        {
            var result = string.Empty;

            switch (type)
            {
                case "System.Int32":
                    result =
                        $"{(overrides.TryGetValue("bits", out var over) ? over : 32)}, {(overrides.TryGetValue("range-min", out var min) ? min : -2147483648)}";
                    break;
                case "System.UInt32":
                    result =
                        $"{(overrides.TryGetValue("bits", out var overUint) ? overUint : 32)}, {(overrides.TryGetValue("range-min", out var minUi) ? minUi : 0)}";
                    break;
                case "System.Single":
                case "Vector3":
                case "System.Numerics.Vector3":
                case "Vector2":
                case "System.Numerics.Vector2":
                case "Color":
                case "System.Numerics.Vector4":

                    overrides.TryGetValue("compression", out var compression);
                    string parameters;

                    if (string.IsNullOrEmpty(compression) || compression.Equals("None"))
                    {
                        parameters = "FloatMeta.NoCompression()";
                    }
                    else if (compression.Equals("FixedPoint"))
                    {
                        parameters =
                            $"FloatMeta.ForFixedPoint({overrides["range-min"]}, {overrides["range-max"]}, {overrides["precision"]}d)";
                    }
                    else
                    {
                        parameters = $"FloatMeta.ForTruncated({overrides["bits"]})";
                    }

                    result = $"{parameters}";
                    break;
                case "Quaternion":
                case "System.Numerics.Quaternion":
                    result = $"{overrides["bits"]}";
                    break;
            }

            return !string.IsNullOrEmpty(result) ? $"{(addComma ? ", " : string.Empty)}{result}" : result;
        }

        public static string GetInteropTypeFromCSharpType(string cSharpType)
        {
            switch (cSharpType)
            {
                case "Color":
                    return "Vector4";
                case "System.Boolean":
                    return "System.Byte";
                case "System.String":
                case "System.Byte[]":
                    return "ByteArray";
                default:
                    return cSharpType;
            }
        }

        public static string GetFromInteropConversion(string cSharpType, string variableName)
        {
            switch (cSharpType)
            {
                case "System.Boolean":
                    return $"comp->{variableName} != 0";
                case "System.String":
                    return $"comp->{variableName}.Data != null ? System.Text.Encoding.UTF8.GetString((byte*)comp->{variableName}.Data, (int)comp->{variableName}.Length) : null";
                case "System.Byte[]":
                    return $"new byte[comp->{variableName}.Length]; " +
                        $"Marshal.Copy((System.IntPtr)comp->{variableName}.Data, orig.{variableName}, 0, (int)comp->{variableName}.Length)";
                default:
                    return $"comp->{variableName}";
            }
        }

        public static string GetToInteropConversionBegin(string cSharpType, string variableName)
        {
            switch (cSharpType)
            {
                case "System.Boolean":
                    return $"val.{variableName} = orig.{variableName} ? (System.Byte)1 : (System.Byte)0;";
                case "System.String":
                    return $"var pinned{variableName} = orig.{variableName} != null ? Encoding.UTF8.GetBytes(orig.{variableName}) : null; " +
                        $"fixed (void* pinnedPtr{variableName} = pinned{variableName}) {{ " +
                        $"val.{variableName} = new ByteArray {{ Data = pinnedPtr{variableName}, Length =  pinned{variableName}?.Length ?? 0 }};";
                case "System.Byte[]":
                    return $"fixed (void* pinnedPtr{variableName} = orig.{variableName}) {{ " +
                        $"val.{variableName} = new ByteArray {{ Data = pinnedPtr{variableName}, Length =  orig.{variableName}?.Length ?? 0 }};";
                default:
                    return $"val.{variableName} = orig.{variableName};";
            }
        }

        public static string GetToInteropConversionEnd(string cSharpType)
        {
            switch (cSharpType)
            {
                case "System.String":
                case "System.Byte[]":
                    return "}";
                default:
                    return "";
            }
        }
    }
}
