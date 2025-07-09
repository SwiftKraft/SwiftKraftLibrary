// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Interpolation;
    using System;
    using Toolkit;
    using Object = System.Object;

    internal struct BindingsTreeViewInput
    {
        enum Range { Min, Max }

        internal static bool HasFocusThisDraw;
        internal static bool InputFocus;
        internal static Type FocusedType;
        internal static int FocusedStep;

        internal enum Type { Selected, MinRange, MaxRange, SampleRate, Bits, Precision, Interpolation, MessageTarget, Reset, Compression }
        internal enum BindingReset { All, RangesOnly, BitsAndPrecisionOnly }

        internal int Step { get; }
        private Type EditedType { get; }

        private double Value { get; }
        private Object Object { get; }
        internal string UndoMessage { get; private set; }

        internal BindingsTreeViewInput(int step, Type type, double value, string undoMessage)
        {
            Step = step;
            EditedType = type;
            Value = value;
            Object = null;
            UndoMessage = undoMessage;
        }

        internal BindingsTreeViewInput(int step, Type type, Object obj, string undoMessage)
        {
            Step = step;
            EditedType = type;
            Value = -1;
            Object = obj;
            UndoMessage = undoMessage;
        }

        internal void ApplyToBinding(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            if (!CanApplyInput(binding))
            {
                return;
            }

            switch (EditedType)
            {
                case Type.Selected: ApplySelectedForSync(binding); break;
                case Type.MinRange: ApplyMinRange(archetypeData, binding); break;
                case Type.MaxRange: ApplyMaxRange(archetypeData, binding); break;
                case Type.SampleRate: ApplySampleRate(archetypeData, binding); break;
                case Type.Bits: ApplyBits(archetypeData, binding); break;
                case Type.Precision: ApplyPrecision(archetypeData, binding); break;
                case Type.Interpolation: ApplyInterpolation(archetypeData, binding); break;
                case Type.MessageTarget: ApplyMessageTarget(archetypeData, binding); break;
                case Type.Reset: ApplyReset(binding); break;
                case Type.Compression: ApplyCompressionType(archetypeData, binding); break;
                default: break;
            }
        }

        private void ApplyCompressionType(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            FloatCompression compression = (FloatCompression)Object;
            archetypeData.SetFloatCompression(compression);
            archetypeData.ResetRanges(binding.Binding.MonoAssemblyRuntimeType);

            for (int i = 0; i < binding.BoundComponent.MaxLods; i++)
            {
                BindingLODStepData lod = archetypeData.GetLODstep(i);
                lod.SetFloatCompression(archetypeData.FloatCompression);
                lod.SetDefaultOverrides(archetypeData.SchemaType);
                lod.Verify(archetypeData.MinRange, archetypeData.MaxRange);
            }

            if (compression != FloatCompression.FixedPoint)
            {
                archetypeData.SetRange(0, 0);
            }
        }

        private void ApplyMinRange(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            long modelMinRange = (long) Math.Round(Value);
            long modelMaxRange = archetypeData.MaxRange;
            bool hasWarnings = false;

            if (!VerifyRangeMinLowerThanMax(binding, modelMinRange, modelMaxRange))
            {
                return;
            }

            System.Type bindingValueBaseType = GetBindingValueBaseType(binding);
            modelMinRange = ClampRangeSideWithinTypeLimits(binding, bindingValueBaseType, ref hasWarnings);

            if (!VerifyFixedPointRangeForAllLODs(archetypeData, binding, modelMinRange, modelMaxRange))
            {
                return;
            }

            ApplyRangesToArchetype(archetypeData, binding, modelMinRange, modelMaxRange);

            if (!hasWarnings)
            {
                binding.ClearGlobalWarning();
            }
        }

        private void ApplyMaxRange(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            long modelMinRange = archetypeData.MinRange;
            long modelMaxRange = (long) Math.Round(Value);
            bool hasWarnings = false;

            if (!VerifyRangeMinLowerThanMax(binding, modelMinRange, modelMaxRange))
            {
                return;
            }

            System.Type bindingValueBaseType = GetBindingValueBaseType(binding);
            modelMaxRange = ClampRangeSideWithinTypeLimits(binding, bindingValueBaseType, ref hasWarnings);

            if (!VerifyFixedPointRangeForAllLODs(archetypeData, binding, modelMinRange, modelMaxRange))
            {
                return;
            }

            ApplyRangesToArchetype(archetypeData, binding, modelMinRange, modelMaxRange);

            if (!hasWarnings)
            {
                binding.ClearGlobalWarning();
            }
        }

        private void ApplySampleRate(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            archetypeData.SetSampleRate((float)Value);
        }

        private void ApplyBits(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            for (int i = 0; i < binding.BoundComponent.MaxLods; i++)
            {
                BindingLODStepData target = archetypeData.GetLODstep(i);
                BindingsTreeViewFloatHelper.SetStepBits(archetypeData, target, i == Step,
                    i < Step, Mathf.RoundToInt((float)Value));
            }
        }

        private void ApplyPrecision(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            for (int i = 0; i < binding.BoundComponent.MaxLods; i++)
            {
                BindingLODStepData target = archetypeData.GetLODstep(i);
                BindingsTreeViewFloatHelper.SetStepPrecision(archetypeData, target, i == Step,
                    i < Step, Value);
            }
        }

        private void ApplyInterpolation(BindingArchetypeData archetypeData, BindingsTreeViewBindingItem binding)
        {
            binding.SetInterpolation((InterpolationSettings)Object);
        }

        private void ApplySelectedForSync(BindingsTreeViewBindingItem bindingItem)
        {
            bindingItem.SetBindingActive(Value > 0);
        }

        private void ApplyReset(BindingsTreeViewBindingItem bindingItem)
        {
            BindingReset bindingReset = (BindingReset)Object;
            bool resetRanges = bindingReset == BindingReset.All || bindingReset == BindingReset.RangesOnly;
            bool resetBitsAndPrecision = bindingReset == BindingReset.All || bindingReset == BindingReset.BitsAndPrecisionOnly;
            bindingItem.ResetValuesToDefault(resetRanges, resetBitsAndPrecision);
        }

        private void ApplyMessageTarget(BindingArchetypeData bindingModel, BindingsTreeViewBindingItem bindingItem)
        {
            ((CommandBinding)bindingItem.Binding).routing = (MessageTarget)Object;
        }

        private bool CanApplyInput(BindingsTreeViewBindingItem binding)
        {
            switch (EditedType)
            {
                case Type.MinRange: return binding.CanOverride;
                case Type.MaxRange: return binding.CanOverride;
                case Type.SampleRate: break;
                case Type.Bits: return binding.CanOverride;
                case Type.Precision: return binding.CanOverride;
                case Type.Interpolation: return binding.GetCanInterpolate();
                case Type.MessageTarget: return binding.IsMethod;
                case Type.Selected: break;
                case Type.Reset: break;
                default: break;
            }
            return true;
        }

        internal static void CheckFocus()
        {
            InputFocus = HasFocusThisDraw;
            HasFocusThisDraw = false;
        }

        internal static void SetColorBeforeInputControl(Type type, bool selected, bool hasWarning, int step,
            Color unselectedColor)
        {
            if (hasWarning)
            {
                GUI.contentColor = BindingsWindowSettings.WarningColor;
            }
            else
            {
                bool highLight = selected && InputFocus && FocusedStep == step && FocusedType == type;
                GUI.contentColor = highLight ? BindingsWindowSettings.HighlightColor : unselectedColor;
            }

            GUI.SetNextControlName("FocusCheck");
        }

        internal static void EvaluateFocus(Type type, int step, bool selected)
        {
            if (!HasFocusThisDraw && selected)
            {
                if (GUI.GetNameOfFocusedControl().Equals("FocusCheck"))
                {
                    HasFocusThisDraw = true;
                    FocusedType = type;
                    FocusedStep = step;
                }
            }
            GUI.contentColor = Color.white;
        }

        private static System.Type GetBindingValueBaseType(BindingsTreeViewBindingItem bindingItem)
        {
            SchemaType schemaType = bindingItem.SchemaType;
            switch (schemaType)
            {
                case SchemaType.Vector2:
                case SchemaType.Vector3:
                case SchemaType.Quaternion:
                case SchemaType.Color:
                    return typeof(float);
                case SchemaType.Int8:
                case SchemaType.UInt8:
                case SchemaType.Int16:
                case SchemaType.UInt16:
                case SchemaType.Char:
                case SchemaType.Bool:
                case SchemaType.Float:
                case SchemaType.Int:
                case SchemaType.Int64:
                case SchemaType.UInt:
                case SchemaType.UInt64:
                case SchemaType.Float64:
                    return bindingItem.Binding.MonoAssemblyRuntimeType;
                default:
                    throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, null);
            }
        }

        private static void ApplyRangesToArchetype(
            BindingArchetypeData archetypeData,
            BindingsTreeViewBindingItem binding,
            long modelMinRange,
            long modelMaxRange)
        {
            archetypeData.SetRange(modelMinRange, modelMaxRange);

            for (int i = 0; i < binding.BoundComponent.MaxLods; i++)
            {
                BindingLODStepData target = archetypeData.GetLODstep(i);
                target.Verify(modelMinRange, modelMaxRange);
            }
        }

        private long ClampRangeSideWithinTypeLimits(BindingsTreeViewBindingItem binding, System.Type bindingValueBaseType, ref bool hasWarnings)
        {
            long clampedValue = ArchetypeMath.ClampWithinTypeLimits(Value, bindingValueBaseType, out bool wasClamped);
            if (wasClamped)
            {
                hasWarnings = true;

                var limits = ArchetypeMath.GetTypeLimits(bindingValueBaseType);
                var typeAlias = ArchetypeMath.GetSimpleTypeAlias(bindingValueBaseType);

                binding.SetGlobalWarning("Value Range input is invalid. " +
                                          $"Value {Value} can't fit in the {typeAlias} type so we clamped it " +
                                          $"to the allowed range: [{limits.minRange}, {limits.maxRange}]");
            }

            return clampedValue;
        }

        private bool VerifyRangeMinLowerThanMax(BindingsTreeViewBindingItem binding, long modelMinRange, long modelMaxRange)
        {
            if (modelMinRange >= modelMaxRange)
            {
                binding.SetGlobalWarning("Value Range input is invalid. " +
                                          $"Value {modelMinRange} is higher or equal to the current max range: {modelMaxRange}. " +
                                          "We've reverted back to your previous input.");
                return false;
            }

            return true;
        }

        private static bool VerifyFixedPointRangeForAllLODs(BindingArchetypeData archetypeData,
            BindingsTreeViewBindingItem binding, long modelMinRange, long modelMaxRange)
        {
            if (!archetypeData.IsFloatType || archetypeData.FloatCompression != FloatCompression.FixedPoint)
            {
                return true;
            }

            for (int i = 0; i < binding.BoundComponent.MaxLods; i++)
            {
                BindingLODStepData lodStep = archetypeData.GetLODstep(i);
                if (!ArchetypeMath.TryGetBitsForFixedFloatValue(modelMinRange, modelMaxRange, lodStep.Precision, out int _))
                {
                    bool canLowerPrecision = lodStep.Precision < BindingLODStepData.FLOAT_MIN_PRECISION;
                    if (canLowerPrecision)
                    {
                        binding.SetGlobalWarning("Value Range input doesn't fit within the range of this " +
                                                  $"data type for precision of {lodStep.Precision}. " +
                                                  "We reverted back to your previous input. " +
                                                  "Please try lowering the precision.");
                    }
                    else
                    {
                        binding.SetGlobalWarning("Value Range input doesn't fit within the range of this " +
                                                 $"data type for precision of {lodStep.Precision}. " +
                                                 "We reverted back to your previous input. " +
                                                 "Please try using a smaller range.");
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
