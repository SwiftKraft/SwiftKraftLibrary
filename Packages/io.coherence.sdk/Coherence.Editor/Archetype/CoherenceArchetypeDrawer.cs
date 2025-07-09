// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using Toolkit;

    internal static class CoherenceArchetypeDrawer
    {
        // Values to tweak visual appearance
        private const int PreviewBarHeight = 20;
        private static readonly Color BarLeftColor = Color.cyan;
        private static readonly Color BarRightColor = Color.blue;
        private static readonly Color NoValueColor = Color.grey;

        // PreviewChart
        private const int PreviewChartHeight = 40;
        private const int PreviewChartDataHeight = 16;
        private const float DistanceLabelMinWidth = 35;
        //private static float NameLabelWidth = 60;

        // Bit costs
        private static readonly Color BitCostHigh = new(.95f, .3f, .0f);
        private static readonly Color BitCostLow = new(.00f, .45f, .50f);
        private const float LongestDistanceMultiplier = 1.15f;

        // Color fading values for headers and text boxes
        private const float BarHeaderColorMultiplier = .6f;
        private const float BitCostColorMultiplier = .65f;
        private const float BarValueColorMultiplier = .5f;

        #region DistancePreview

        // Distance previewing, the entire bar
        internal static void DrawDistancePreview(SerializedArchetype archetype)
        {
            // Find the longest distance, also multiply the distance we draw, so we can increase it using the sliders.
            var longestDistance = GetLongestDistance(archetype) * LongestDistanceMultiplier;
            var distances = GetDistances(archetype);

            // handling if there are no distances above 0
            if (longestDistance == 0)
            {
                longestDistance = 10;
            }

            // Draw the distances on top
            var previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(PreviewBarHeight));
            DrawDistanceUnitGuide(previewRect, distances, longestDistance);

            // Draw the distance guide Bar

            var segment = new Rect(previewRect.x, previewRect.y + PreviewBarHeight, previewRect.width, PreviewBarHeight);
            DrawDataDistancePreviewBarSegment(archetype, segment, longestDistance);

            GUILayout.Space(30);

            // Draw the chart
            var chartRect = EditorGUILayout.GetControlRect(GUILayout.Height(PreviewChartHeight));
            MakePreviewChart(chartRect, archetype, distances, longestDistance, false);

            var guideRect = EditorGUILayout.GetControlRect(GUILayout.Height(PreviewChartDataHeight));
            DrawDistanceUnitGuide(guideRect, distances, longestDistance);
        }

        // Draws the multisliders and handles input
        private static void DrawDataDistancePreviewBarSegment(SerializedArchetype archetype, Rect rect, float longestDistance)
        {
            var sliderRange = new MinMaxFloat(0, longestDistance);
            var sliderColors = new MinMaxColor(BarLeftColor * BarValueColorMultiplier, BarRightColor * BarValueColorMultiplier);

            var sliderSettings = new MultiSlider.SliderSettings(sliderRange, sliderColors, true, EditorStyles.miniBoldLabel);
            var sliderValues = new MultiSlider.SliderValue[archetype.LodLevels.arraySize];
            for (var i = 0; i < archetype.LodLevels.arraySize; i++)
            {
                var lodLevel = archetype.LodLevels.GetArrayElementAtIndex(i);
                using var distance = lodLevel.FindPropertyRelative("distance");
                sliderValues[i] = new MultiSlider.SliderValue(lodLevel, distance.floatValue, i != 0,
                    new GUIContent($"{archetype.GetTotalActiveBitsOfLOD(i)} Bits"));
            }

            EditorGUI.BeginChangeCheck();
            var selectedValue = MultiSlider.Draw(rect, sliderSettings, sliderValues, false);

            // Undo state
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedValue != null)
                {
                    var lodLevel = (SerializedProperty)selectedValue.Data;
                    using var distance = lodLevel.FindPropertyRelative("distance");
                    distance.floatValue = selectedValue.Value;
                    // read functions to set lower/upper accordingly
                }

                Undo.RecordObject(archetype.Sync, "Changed Distance of LOD");
            }
        }

        // Draws the units on top and below the bars
        private static void DrawDistanceUnitGuide(Rect rect, List<float> distances, float longestDistance,
            bool drawFinalValue = false)
        {
            var labelSkin = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };

            // Draw the 0
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 10, PreviewBarHeight), "0", EditorStyles.centeredGreyMiniLabel);

            // Draw the max number
            if (drawFinalValue)
            {
                labelSkin.alignment = TextAnchor.MiddleRight;
                var longestDistanceSize = GUI.skin.label.CalcSize(new GUIContent(longestDistance.ToString("G5")));
                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width - longestDistanceSize.x, rect.y, longestDistanceSize.x, PreviewBarHeight),
                    longestDistance.ToString("G5"), EditorStyles.centeredGreyMiniLabel);
            }

            // Draw the other numbers, only if they fit.
            float lastLabel = 0;
            foreach (var distance in distances)
            {
                // calculate sizes
                var relativeWidth = rect.width * distance / longestDistance;

                // Draw the numbers if they fit
                if (lastLabel + DistanceLabelMinWidth <= relativeWidth)
                {
                    var labelRect = new Rect(rect.x + relativeWidth - DistanceLabelMinWidth, rect.y,
                        DistanceLabelMinWidth, PreviewBarHeight);

                    var text = $"{distance:G4}";
                    GUI.Label(labelRect, text, labelSkin);
                    lastLabel = relativeWidth;
                }
            }
        }


        private static void MakePreviewChart(Rect rect, SerializedArchetype archetype, List<float> distances, float longestDistance, bool fillFromLeft)
        {
            // Get the sizes of the LODsteps at the distance points
            var largestValue = 0;
            var bits = new List<float>();
            for (var i = 0; i < distances.Count; i++)
            {
                var bitsAtDistance = archetype.GetTotalActiveBitsOfLOD(i);
                bits.Add(bitsAtDistance);
                largestValue = Mathf.Max(bitsAtDistance, largestValue);
            }

            // Loop through and draw vertical bars
            var offsetX = 0f;
            var stepDistance = rect.width / distances.Count;

            for (var i = 0; i < distances.Count; i++)
            {
                // calculate sizes
                var relativeWidth = Mathf.Ceil(rect.width * distances[i] / longestDistance);
                var relativeHeight = rect.height * bits[i] / largestValue;
                var leftToRightWidth = relativeWidth - offsetX - 1;

                if (!fillFromLeft)
                {
                    offsetX = relativeWidth;

                    var nextDistance = i < distances.Count - 1 ? distances[i + 1] : longestDistance;
                    relativeWidth = Mathf.Ceil(rect.width * ((nextDistance - distances[i]) / longestDistance));
                }

                var rightToLeftWidth = relativeWidth - 1;

                // Format and draw bars
                var segment = new Rect(rect.x + offsetX, rect.y + rect.height - relativeHeight,
                    fillFromLeft ? leftToRightWidth : rightToLeftWidth, relativeHeight);
                var color = UIHelpers.HSVLerp(BitCostLow, BitCostHigh, bits[i] / largestValue);
                EditorGUI.DrawRect(segment, color * BitCostColorMultiplier);
                offsetX = relativeWidth;
            }
        }

        internal static void DrawDataWeightMiniBar(Rect rect, int[] values, int steps, bool showAsEditable)
        {
            var maxvalue = values.Max();

            EditorGUI.DrawRect(rect, Color.black * .2f);
            var stepDistance = rect.width / steps;
            for (var i = 0; i < steps; i++)
            {
                var hasLOD = values.Length > i;
                if (hasLOD && values[i] > 0)
                {
                    var color = showAsEditable
                        ? UIHelpers.HSVLerp(BarLeftColor, BarRightColor, (float)i / steps)
                        : NoValueColor * .5f;
                    var height = (float)values[i] / maxvalue;
                    var segmentRect = new Rect(rect.x + stepDistance * i, rect.y, stepDistance, rect.height);
                    var segment = new Rect(rect.x + stepDistance * i, rect.yMax - rect.height * height,
                        stepDistance - 1, rect.height * height);
                    EditorGUI.DrawRect(segment, color * BarValueColorMultiplier);
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.LowerCenter,
                    };
                    GUI.color = showAsEditable ? Color.white : Color.white * .7f;
                    EditorGUI.LabelField(segmentRect, values[i].ToString(), style);
                    GUI.color = Color.white;
                }
            }
        }

        internal static int DrawEditableDataWeightMiniBar(Rect rect, ref int[] values, int steps, int maxValue,
            int multiplier)
        {
            EditorGUI.DrawRect(rect, Color.black * .2f);
            var stepDistance = rect.width / steps;
            var editedStep = -1;
            for (var i = 0; i < steps; i++)
            {
                var hasLOD = values.Length > i;
                if (hasLOD)
                {
                    var color = UIHelpers.HSVLerp(BarLeftColor, BarRightColor, (float)i / steps);
                    var height = (float)values[i] / maxValue;
                    var segmentRect = new Rect(rect.x + stepDistance * i, rect.y, stepDistance, rect.height);
                    var segment = new Rect(rect.x + stepDistance * i, rect.yMax - rect.height * height,
                        stepDistance - 1, rect.height * height);
                    EditorGUI.DrawRect(segment, color * BarValueColorMultiplier);

                    // Draw input slider
                    var bar = new GUIStyle(GUI.skin.button);
                    var guiColor = GUI.color;
                    GUI.color = new Color(1, 1, 1, .1f);
                    var newValue = Mathf.RoundToInt(GUI.VerticalSlider(segmentRect, values[i], maxValue, 0, bar, GUI.skin.label));
                    if (newValue != values[i])
                    {
                        newValue = Mathf.Max(newValue, 1);
                        editedStep = i;
                        values[i] = newValue;
                    }

                    GUI.color = guiColor;

                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.LowerCenter,
                    };
                    EditorGUI.LabelField(segmentRect, (values[i] * multiplier).ToString(), style);
                }
            }

            return editedStep;
        }

        #endregion

        #region WeightPreview

        // DataWeight Bar

        // Draws a part of the bar
        internal static void DrawDataWeightPreviewBar(SerializedArchetype archetype, int levels)
        {
            var previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(PreviewBarHeight * 2));
            var headerRect = new Rect(previewRect.x, previewRect.y, previewRect.width, PreviewBarHeight);
            DrawDataWeightPreviewBarHeader(headerRect, levels);
            var segmentRect = new Rect(previewRect.x, previewRect.y + PreviewBarHeight, previewRect.width, PreviewBarHeight);
            DrawDataWeightPreviewBarSegment(archetype, segmentRect, levels);
        }

        // Draws the LOD values on the bar
        private static void DrawDataWeightPreviewBarSegment(SerializedArchetype archetype, Rect rect, int maxLevel)
        {
            var lodLevels = archetype.LodLevels;
            for (var i = 0; i < lodLevels.arraySize; i++)
            {
                var text = $"{archetype.GetTotalActiveBitsOfLOD(i)} Bits";
                var color = UIHelpers.HSVLerp(BarLeftColor, BarRightColor, (float)i / lodLevels.arraySize);

                DrawBarSegmentShared(rect, i, maxLevel, color * BarValueColorMultiplier, text);
            }
        }

        // Draws the header of the bar
        private static void DrawDataWeightPreviewBarHeader(Rect rect, int steps)
        {
            for (var i = 0; i < steps; i++)
            {
                var color = UIHelpers.HSVLerp(BarLeftColor, BarRightColor, (float)i / steps);
                DrawBarSegmentShared(rect, i, steps, color * BarHeaderColorMultiplier, $"LOD {i}");
            }
        }

        // Draws a part of the bar
        private static void DrawBarSegmentShared(Rect rect, int step, int steps, Color color, string text)
        {
            var stepDistance = rect.width / steps;
            var segment = new Rect(rect.x + stepDistance * step, rect.y, stepDistance - 1, rect.height - 1);
            EditorGUI.DrawRect(segment, color);
            if (!string.IsNullOrEmpty(text))
            {
                EditorGUI.LabelField(segment, text, EditorStyles.miniBoldLabel);
            }
        }

        // Get the relevant distances and sort them
        private static List<float> GetDistances(SerializedArchetype archetype)
        {
            var distances = new List<float>();
            for (var i = 0; i < archetype.LodLevels.arraySize; i++)
            {
                using var distance = archetype.LodLevels.GetArrayElementAtIndex(i).FindPropertyRelative("distance");
                distances.Add(distance.floatValue);
            }

            return distances;
        }

        // Helper functions
        private static float GetLongestDistance(SerializedArchetype archetype)
        {
            float longest = 0;
            for (var i = 0; i < archetype.LodLevels.arraySize; i++)
            {
                using var distance = archetype.LodLevels.GetArrayElementAtIndex(i).FindPropertyRelative("distance");
                longest = Mathf.Max(longest, distance.floatValue);
            }

            return longest;
        }

        internal static Color GetLODColor(float amount) => UIHelpers.HSVLerp(BarLeftColor, BarRightColor, amount);
    }

    #endregion
}
