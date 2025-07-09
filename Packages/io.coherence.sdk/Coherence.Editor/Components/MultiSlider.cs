// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEditor;

    internal static class MultiSlider
    {
        // Data used for to update the currently grabbed value of the slider
        private static object grabbedData;
        private static float grabbedSliderDragValue;

        // Class holding the values for the slider
        public class SliderValue
        {
            public float Value;
            public object Data;
            public GUIContent SegmentLabelText;

            public bool CanBeGrabbed;
            public bool IsGrabbed => (grabbedData != null && grabbedData == Data);

            public SliderValue(object data, float value, bool canBeGrabbed, GUIContent segmentText)
            {
                Data = data;
                Value = value;
                CanBeGrabbed = canBeGrabbed;
                SegmentLabelText = segmentText;
            }

            public void SetValue(float value) => Value = value;
        }

        // Used to declare how the slider looks
        public class SliderSettings
        {
            public bool DrawValueIfGrabbing { get; }
            public GUIStyle SegmentLabelStyle { get; }

            public MinMaxFloat Range { get; }
            private MinMaxColor colors;

            // These are shared for now
            public float BrightenIfSelected = 1.3f;
            public float SliderHightlight = 0.2f;
            public float InputKnobSize = 25;

            public SliderSettings(MinMaxFloat sliderRange, MinMaxColor sliderColors, bool drawValueIfGrabbing, GUIStyle segmentLabelStyle)
            {
                Range = sliderRange;
                colors = sliderColors;
                DrawValueIfGrabbing = drawValueIfGrabbing;
                SegmentLabelStyle = new GUIStyle(segmentLabelStyle);
            }

            public float GetSliderProgress(float value) => Range.InverseLerp(value);

            public Color GetColorProgress(float progress) => colors.Lerp(progress, true);
        }

        // Draws a multislider, returns the value that is currently grabbed if any
        public static SliderValue Draw(Rect rect, SliderSettings settings, SliderValue[] values, bool fillFromLeft)
        {
            SliderValue grabbedValue = null;
            float offset = 0;

            for (var i = 0; i < values.Length; i++)
            {
                var sliderValue = values[i];
                var isGrabbed = sliderValue.IsGrabbed;
                if (isGrabbed)
                {
                    grabbedValue = sliderValue;
                    grabbedValue.SetValue(grabbedSliderDragValue);
                }

                var normalizedValue = settings.GetSliderProgress(sliderValue.Value);
                var nextNormalizedValue = i < values.Length - 1 ? settings.GetSliderProgress(values[i + 1].Value) : 1;

                // Calculate size
                var relativeSize = Mathf.Ceil(rect.width * normalizedValue);

                var leftToRightWidth = relativeSize - offset;
                var rightToLeftWidth = Mathf.Ceil(rect.width * (nextNormalizedValue - normalizedValue));
                if (!fillFromLeft)
                {
                    offset = relativeSize;
                }

                var segmentRect = new Rect(rect.x + offset, rect.y, fillFromLeft ? leftToRightWidth : rightToLeftWidth,
                    rect.height - 1);
                offset = relativeSize;

                // Format main bar, coloring by segment.
                var color = settings.GetColorProgress((float)i / values.Length);
                EditorGUI.DrawRect(segmentRect, color * (isGrabbed ? settings.BrightenIfSelected : 1));

                // Draw current value if grabbing
                if (isGrabbed && settings.DrawValueIfGrabbing)
                {
                    EditorGUI.LabelField(segmentRect, sliderValue.Value.ToString("G4"), settings.SegmentLabelStyle);
                }
                else
                {
                    EditorGUI.LabelField(segmentRect, sliderValue.SegmentLabelText, settings.SegmentLabelStyle);
                }

                // Invisible knob to handle input
                if (sliderValue.CanBeGrabbed)
                {
                    var knobRect = new Rect(rect.x + offset - settings.InputKnobSize * .5f, rect.y, settings.InputKnobSize, segmentRect.height);
                    GUI.Label(knobRect, GUIContent.none);
                    CheckForClick(knobRect, sliderValue);
                }
            }

            // Add an almost invisible bar to perform dragging input
            EditorGUI.BeginChangeCheck();
            var bar = new GUIStyle(GUI.skin.button);
            var guiColor = GUI.color;
            GUI.color = new Color(1, 1, 1, settings.SliderHightlight);
            grabbedSliderDragValue = GUI.HorizontalSlider(rect, grabbedSliderDragValue, settings.Range.MinValue, settings.Range.MaxValue, bar, GUI.skin.label);
            GUI.color = guiColor;

            // return function
            if (EditorGUI.EndChangeCheck())
            {
                if (grabbedValue != null)
                {
                    grabbedValue.Value = grabbedSliderDragValue;
                    return grabbedValue;
                }
            }

            return null;
        }


        // Checking if we are grabbing a knob or not - if we do store the data else drop any currently stored data.
        private static void CheckForClick(Rect rect, SliderValue sliderValue)
        {
            var evt = Event.current;
            if (Event.current.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                grabbedData = sliderValue.Data;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                grabbedData = null;
            }
        }
    }
}
