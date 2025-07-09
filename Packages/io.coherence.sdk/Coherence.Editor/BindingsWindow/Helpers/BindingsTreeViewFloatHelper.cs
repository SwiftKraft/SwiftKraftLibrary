// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using System;

    internal class BindingsTreeViewFloatHelper
    {
        // Input drawers
        internal static int DrawBindingBitsInput(Rect secondFieldRect, BindingLODStepData field, int step, bool selected)
        {
            int newBits = 0;

            int multiplier = ArchetypeMath.GetBitsMultiplier(field.SchemaType);

            BindingsTreeViewInput.SetColorBeforeInputControl(BindingsTreeViewInput.Type.Bits, selected, false, step, Color.white);

            newBits = EditorGUI.DelayedIntField(secondFieldRect, field.Bits);
            BindingsTreeViewInput.EvaluateFocus(BindingsTreeViewInput.Type.Bits, step, selected);

            newBits = Mathf.Max(newBits, 0);

            GUIStyle miniText = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            miniText.alignment = TextAnchor.MiddleRight;
            miniText.fontSize = 9;
            if (multiplier > 1)
            {
                Rect multiplierrect = new Rect(secondFieldRect.x + secondFieldRect.width - 40, secondFieldRect.y, 40, secondFieldRect.height);
                GUI.Label(multiplierrect, $"*{multiplier}", miniText);
            }

            return newBits;
        }

        // Setters

        internal static void SetStepBits(BindingArchetypeData archetypeData, BindingLODStepData field,
            bool editedStep, bool isLowerThanEditedStep, int bits)
        {
            if (!editedStep)
            {
                bits = isLowerThanEditedStep ? Mathf.Max(bits, field.Bits) : Mathf.Min(bits, field.Bits);
            }

            field.SetBits(bits);
            field.Verify(archetypeData.MinRange, archetypeData.MaxRange);
        }

        internal static void SetStepPrecision(BindingArchetypeData archetypeData, BindingLODStepData field,
            bool editedStep, bool isLowerThanEditedStep, double precision)
        {
            if (!editedStep)
            {
                precision = isLowerThanEditedStep ? Math.Min(precision, field.Precision) : Math.Max(precision, field.Precision);
            }

            field.SetPrecision(precision);
            field.Verify(archetypeData.MinRange, archetypeData.MaxRange);
        }
    }
}
