// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEditor.IMGUI.Controls;
    using UnityEditor;

    internal abstract class BindingsTreeViewItem : TreeViewItem
    {
        internal float RowHeight { private set; get; }
        internal bool Expanded { private set; get; }

        protected int Lodsteps { private set; get; }

        internal BindingsTreeViewItem()
        {
            id = -1;
            depth = -1;

            Setup(rowHeight: 20, lodSteps: 0);
        }

        internal virtual void Setup(int rowHeight, int lodSteps)
        {
            RowHeight = rowHeight;
            Lodsteps = lodSteps;
        }

        // Draw selector
        internal void DrawCell(Rect rect, BindingsWindowState.ColumnContent columnContent, int lodstep)
        {
            Rect contentRect = new RectOffset(4, 4, 0, 0).Remove(rect); // Add some more padding to not be too busy close to the line

            if (columnContent == BindingsWindowState.ColumnContent.Bindings)
            {
                DrawLeftBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.Config)
            {
                DrawBindingConfigBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.CompressionType)
            {
                DrawCompressionTypeBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.ValueRange)
            {
                DrawValueRangeBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.SampleRate)
            {
                DrawSampleRateBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.Statistics)
            {
                DrawStatisticsBar(contentRect);
            }

            if (columnContent == BindingsWindowState.ColumnContent.LOD)
            {
                DrawLOD(contentRect, lodstep);
            }

            Rect verticalLineRect = new Rect(rect.xMax-1, rect.y, 1, rect.height);
            EditorGUI.DrawRect(verticalLineRect, BindingsWindowSettings.VerticalLineColor);
        }

        internal virtual bool CanChangeExpandedState()
        {
            return false;
        }

        internal virtual void UpdateExpandedState(bool expanded)
        {
            Expanded = expanded;
        }

        internal virtual void DrawRowBackground(Rect rowRect)
        {
            Rect lineRect = new Rect(rowRect.x, rowRect.yMax - 1, rowRect.width, 1);
            EditorGUI.DrawRect(lineRect, BindingsWindowSettings.HorizontalLineColor);
        }

        protected virtual void DrawLeftBar(Rect rect)
        {
        }

        protected virtual void DrawCompressionTypeBar(Rect rect)
        {
        }

        protected virtual void DrawValueRangeBar(Rect rect)
        {
        }

        protected virtual void DrawSampleRateBar(Rect rect)
        {
        }

        protected virtual void DrawBindingConfigBar(Rect rect)
        {
        }

        protected virtual void DrawStatisticsBar(Rect rect)
        {
        }

        protected virtual void DrawLOD(Rect rect, int lodstep)
        {
        }

        protected IList<int> GetAllChildrenIDs()
        {
            IList<int> ids = new List<int>();
            GetChildrenRecursive(ids, this);
            return ids;
        }

        private void GetChildrenRecursive(IList<int> ids, TreeViewItem item)
        {
            ids.Add(item.id);
            if (item.children != null)
            {
                foreach (var child in item.children)
                {
                    if (child != null)
                    {
                        GetChildrenRecursive(ids, child);
                    }
                }
            }
        }

        internal virtual bool CheckIfFilteredOut(BindingsWindowTreeFilters filters, bool bindingCanBeEdited)
        {
            return false;
        }

        protected void DrawBitPercentageCircle(Rect rect, float percentage, float size = 16)
        {
            Handles.BeginGUI();
            Handles.color = new Color(.05f, .05f, .05f, .2f);
            Handles.DrawSolidArc(rect.center, Vector3.forward, Vector3.down, 360, size * .5f);
            Handles.color = new Color(.55f, .55f, .55f, .2f);
            Handles.DrawSolidArc(rect.center, Vector3.forward, Vector3.down, percentage * 360, size * .5f);
            Handles.EndGUI();
        }
        protected void DrawBitPercentage(Rect rect, int bits, int total, string pre = "", string post = "")
        {
            rect = new RectOffset(2,2,2,2).Remove(rect);

            float percentage = (float)bits / total;

            int percentageAsInt = total == 0 ? 0 : Mathf.RoundToInt(percentage * 100f);
            string percentageAsString = $"{(percentageAsInt == 0 ? "<1" : Mathf.RoundToInt(percentage * 100f).ToString())}";
            string totalAsString = $"{pre}{percentageAsString}%{post}";

            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.fontSize = 9;
            style.wordWrap = false;
            style.alignment = TextAnchor.MiddleRight;
            style.clipping = TextClipping.Overflow;
            style.normal.textColor *= .7f;
            GUI.Label(rect, new GUIContent($"{percentageAsString}%", totalAsString), style);
        }

        // Layout structs
        internal struct LODLayoutRects
        {
            public Rect MinRange { get; }
            public Rect MaxRange { get; }
            public Rect Bits{ get; }
            public Rect Precision { get; }
            public Rect BitTotal { get; }
            public Rect Percentage { get; }

            public LODLayoutRects(Rect rect, bool useRanges, RectOffset padding = null)
            {
                float widthMinusConstants = rect.width - BindingsWindowSettings.LODConstantWidth;
                float otherFieldswidth = widthMinusConstants / (useRanges ? 3 : 1);
                float rangesWidth = useRanges ? otherFieldswidth : 0;

                MinRange = new Rect(rect.x, rect.y, rangesWidth, rect.height);
                MaxRange = new Rect(MinRange.xMax, rect.y, rangesWidth, rect.height);

                Precision = new Rect(MaxRange.xMax, rect.y, BindingsWindowSettings.LODPrecisionFieldWidth, rect.height);
                Bits = new Rect(Precision.xMax, rect.y, otherFieldswidth, rect.height);

                BitTotal = new Rect(Bits.xMax, rect.y, BindingsWindowSettings.LODBitDisplayWidth, rect.height);
                Percentage = new Rect(BitTotal.xMax, rect.y, BindingsWindowSettings.ShowBitPercentages ? BindingsWindowSettings.LODBitPercentageWidth : 0, rect.height);

                if (padding != null)
                {
                    if (useRanges)
                    {
                        MinRange = padding.Remove(MinRange);
                        MaxRange = padding.Remove(MaxRange);
                    }

                    Bits = padding.Remove(Bits);
                    Precision = padding.Remove(Precision);
                    BitTotal = padding.Remove(BitTotal);
                    Percentage = padding.Remove(Percentage);
                }
            }
        }

        // Layout structs
        internal struct SplitInThreeLayoutRects
        {
            public Rect FirstRect { get; }
            public Rect SecondRect { get; }
            public Rect ThirdRect { get; }

            public SplitInThreeLayoutRects(Rect rect, RectOffset padding = null)
            {
                float fieldswidth = rect.width / 3;
                FirstRect = new Rect(rect.x, rect.y, fieldswidth, rect.height);
                SecondRect = new Rect(FirstRect.xMax, rect.y, fieldswidth, rect.height);
                ThirdRect = new Rect(SecondRect.xMax, rect.y, fieldswidth, rect.height);

                if (padding != null)
                {
                    FirstRect = padding.Remove(FirstRect);
                    SecondRect = padding.Remove(SecondRect);
                    ThirdRect = padding.Remove(ThirdRect);
                }
            }
        }

        // Layout structs
        internal struct SplitInTwoLayoutRects
        {
            public Rect FirstRect { get; }
            public Rect SecondRect { get; }

            public SplitInTwoLayoutRects(Rect rect, int fixedSize, RectOffset padding = null)
            {
                SecondRect = new Rect(rect.xMax - fixedSize, rect.y, fixedSize, rect.height);
                FirstRect = new Rect(rect.x, rect.y, rect.width - fixedSize, rect.height);

                if (padding != null)
                {
                    FirstRect = padding.Remove(FirstRect);
                    SecondRect = padding.Remove(SecondRect);
                }
            }

            public SplitInTwoLayoutRects(Rect rect, RectOffset padding = null)
            {
                FirstRect = new Rect(rect.x, rect.y, rect.width *.5f, rect.height);
                SecondRect = new Rect(FirstRect.xMax, rect.y, rect.width * .5f, rect.height);

                if (padding != null)
                {
                    FirstRect = padding.Remove(FirstRect);
                    SecondRect = padding.Remove(SecondRect);
                }
            }
        }
    }
}

