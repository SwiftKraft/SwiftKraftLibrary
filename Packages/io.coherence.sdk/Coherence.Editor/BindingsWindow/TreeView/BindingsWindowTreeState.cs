// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using UnityEditor.IMGUI.Controls;
    using Coherence.Editor.Toolkit;
    using System.Collections.Generic;
    using System;

    internal class BindingsWindowState
    {
        internal enum State
        {
            Bindings,
            Methods,
            Interpolation,
            Lods
        }

        internal enum ColumnContent
        {
            Bindings,
            Config,
            CompressionType,
            ValueRange,
            SampleRate,
            Statistics,
            LOD
        }

        internal State Type;

        internal List<ColumnContent> ActiveColumns = new List<ColumnContent>();
        internal List<float> ColumnWidths = new List<float>();

        internal List<ColumnContent> LodColumns = new List<ColumnContent>();
        internal List<float> LodColumnWidths = new List<float>();

        internal BindingsWindowState(State state)
        {
            Type = state;
        }

        internal void SetToNewObject(int lodsOnObject)
        {
            CreateColumnContentsByType();
            CreateLODColumns(lodsOnObject);
        }

        internal void AddLOD()
        {
            if (Type == State.Lods)
            {
                LodColumns.Insert(ActiveColumns.Count - 1, ColumnContent.LOD);
                LodColumnWidths.Insert(ColumnWidths.Count - 1, ColumnWidths[ColumnWidths.Count - 2]);
            }
        }

        internal void RemoveLOD(int lod)
        {
            if (Type == State.Lods)
            {
                LodColumns.RemoveAt(lod);
                LodColumnWidths.RemoveAt(lod);
            }
        }

        internal int GetColumnByLOD(int column)
        {
            return column - ActiveColumns.Count;
        }
        internal int GetLODByColumnIndex(int lod)
        {
            return lod + ActiveColumns.Count;
        }

        internal ColumnContent GetColumnContent(int columnIndex)
        {
            if (columnIndex >= ActiveColumns.Count)
            {
                columnIndex = Mathf.Clamp(columnIndex - ActiveColumns.Count, 0, LodColumns.Count-1);
                return LodColumns[columnIndex];
            }

            return ActiveColumns[columnIndex];
        }
        internal int GetColumnByContent(ColumnContent columnContent)
        {
            for (int i = 0; i < ActiveColumns.Count; i++)
            {
                if (ActiveColumns[i] == columnContent)
                {
                    return i;
                }
            }

            return -1;
        }

        internal float GetColumnWidth(int columnIndex)
        {
            if (columnIndex >= ActiveColumns.Count)
            {
                columnIndex = Mathf.Clamp(columnIndex - ActiveColumns.Count, 0, LodColumns.Count - 1);
                return LodColumnWidths[columnIndex];
            }

            return ColumnWidths[columnIndex];
        }

        internal void SetColumnWidth(int columnIndex, float width)
        {
            if (columnIndex >= ColumnWidths.Count)
            {
                columnIndex = Mathf.Clamp(columnIndex - ActiveColumns.Count, 0, LodColumns.Count - 1);
                LodColumnWidths[columnIndex] = width;
                return;
            }

            ColumnWidths[columnIndex] = width;
        }

        private void CreateColumnContentsByType()
        {
            ActiveColumns.Clear();
            ColumnWidths.Clear();

            AddBindingsColumn();
            AddCompressionTypeColumn();

            if (Type == State.Methods ||Type == State.Interpolation)
            {
                AddConfigColumn();
            }

            if (Type == State.Lods)
            {
                AddValueRangeColumn();
                AddSampleRateColumn();
                AddStatisticsColumn();
            }
        }

        private void CreateLODColumns(int lodsOnObject)
        {
            LodColumns.Clear();
            LodColumnWidths.Clear();

            if (Type == State.Lods)
            {
                for (int i = 0; i < lodsOnObject; i++)
                {
                    LodColumns.Add(ColumnContent.LOD);
                    LodColumnWidths.Add(BindingsWindowSettings.LODSettings.DefaultWidth);
                }
            }
        }

        private void AddBindingsColumn()
        {
            ActiveColumns.Add(ColumnContent.Bindings);
            ColumnWidths.Add(BindingsWindowSettings.LeftBarSettings.DefaultWidth);
        }

        private void AddCompressionTypeColumn()
        {
            ActiveColumns.Add(ColumnContent.CompressionType);
            ColumnWidths.Add(BindingsWindowSettings.TypeSettings.DefaultWidth);
        }

        private void AddValueRangeColumn()
        {
            ActiveColumns.Add(ColumnContent.ValueRange);
            ColumnWidths.Add(BindingsWindowSettings.ValueRangeSettings.DefaultWidth);
        }

        private void AddSampleRateColumn()
        {
            ActiveColumns.Add(ColumnContent.SampleRate);
            ColumnWidths.Add(BindingsWindowSettings.SampleRateSettings.DefaultWidth);
        }

        private void AddConfigColumn()
        {
            ActiveColumns.Add(ColumnContent.Config);
            ColumnWidths.Add(BindingsWindowSettings.BindingConfigSettings.DefaultWidth);
        }

        private void AddStatisticsColumn()
        {
            ActiveColumns.Add(ColumnContent.Statistics);
            ColumnWidths.Add(BindingsWindowSettings.StatisticsSettings.DefaultWidth);
        }
    }
}
