// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using UnityEditor.IMGUI.Controls;
    using System;

    internal class BindingsWindowStateController
    {
        internal bool Bindings => CurrentState == BindingsState;
        internal bool Methods => CurrentState == MethodsState;
        internal bool Lods => CurrentState == LodsState;
        internal bool Interpolation => CurrentState == InterpolationState;
        internal bool AllowUserToBind => Bindings || Methods;

        private BindingsWindowState BindingsState;
        private BindingsWindowState MethodsState;
        private BindingsWindowState LodsState;
        private BindingsWindowState InterpolationState;

        private CoherenceSync sync;

        internal BindingsWindowState CurrentState { private set; get; }

        internal BindingsWindowStateController()
        {
            BindingsState = new BindingsWindowState(BindingsWindowState.State.Bindings);
            MethodsState = new BindingsWindowState(BindingsWindowState.State.Methods);
            LodsState = new BindingsWindowState(BindingsWindowState.State.Lods);
            InterpolationState = new BindingsWindowState(BindingsWindowState.State.Interpolation);

            CurrentState = GetCurrentState(BindingsWindowState.State.Lods);
        }

        internal void SetObject(CoherenceSync sync)
        {
            this.sync = sync;
            int lodsOnObject = sync.Archetype.LODLevels.Count;
            BindingsState.SetToNewObject(lodsOnObject);
            InterpolationState.SetToNewObject(lodsOnObject);
            MethodsState.SetToNewObject(lodsOnObject);
            LodsState.SetToNewObject(lodsOnObject);
        }

        internal void UpdateWidths(MultiColumnHeaderState state)
        {
            throw new NotImplementedException();
        }

        internal void AddLOD()
        {
            LodsState.AddLOD();
        }

        internal void RemoveLOD(int lod)
        {
            LodsState.RemoveLOD(lod);
        }

        internal bool DrawStateSelection(Rect rect)
        {
            if (sync == null)
            {
                return false;
            }

            bool changed = false;
            int buttonWidth = Mathf.FloorToInt(rect.width / 4);
            /*
            if (DrawButton(new Rect(rect.x, rect.y, buttonWidth,rect.height),  BindingsWindowState.State.Bindings, new GUIContent("Variables")))
            {
                changed = true;
            }
            if (DrawButton(new Rect(rect.x + buttonWidth, rect.y, buttonWidth, rect.height), BindingsWindowState.State.Methods, new GUIContent("Methods")))
            {
                changed = true;
            }    */
            if (DrawButton(new Rect(rect.x, rect.y, buttonWidth, rect.height), BindingsWindowState.State.Interpolation, new GUIContent("Interpolation")))
            {
                changed = true;
            }
            if (DrawButton(new Rect(rect.x + buttonWidth, rect.y, buttonWidth, rect.height), BindingsWindowState.State.Lods, new GUIContent("Bandwidth")))
            {
                changed = true;
            }

            return changed;
        }

        internal void SetColumnWidth(int columnIndex, float width)
        {
            CurrentState.SetColumnWidth(columnIndex, width);
        }

        internal float GetColumnWidth(int columnIndex)
        {
            return CurrentState.GetColumnWidth(columnIndex);
        }

        internal BindingsWindowState.ColumnContent GetColumnContent(int columnIndex)
        {
            return CurrentState.GetColumnContent(columnIndex);
        }

        internal int GetLODByColumnIndex(int columnIndex)
        {
            return CurrentState.GetColumnByLOD(columnIndex);
        }
        internal int GetColumnByLOD(int lod)
        {
            return CurrentState.GetLODByColumnIndex(lod);
        }

        private bool DrawButton(Rect rect, BindingsWindowState.State state, GUIContent text)
        {
            bool pressed = false;

            GUI.backgroundColor = CurrentState.Type == state ? BindingsWindowSettings.HeaderColor : BindingsWindowSettings.RowColor;
            GUIStyle buttonStyle = UIHelpers.Button.Get("WindowTab", 4);
            if (GUI.Button(rect, text, buttonStyle) && CurrentState.Type != state)
            {
                CurrentState = GetCurrentState(state);
                pressed = true;
            }
            GUI.backgroundColor = Color.white;

            return pressed;
        }

        private BindingsWindowState GetCurrentState(BindingsWindowState.State state)
        {
            switch (state)
            {
                case BindingsWindowState.State.Bindings: return BindingsState;
                case BindingsWindowState.State.Methods: return MethodsState;
                case BindingsWindowState.State.Lods: return LodsState;
                case BindingsWindowState.State.Interpolation: return InterpolationState;
                default: return BindingsState;
            }
        }

        internal string GetHelpText()
        {
            switch (CurrentState.Type)
            {
                case BindingsWindowState.State.Methods: return "Select methods that will excute over the network";
                case BindingsWindowState.State.Lods: return "Use LODs to send less data over the network when further away from this prefab";
                case BindingsWindowState.State.Bindings: return "Select variables to sync over the network";
                case BindingsWindowState.State.Interpolation: return "Select how values are interpolated";

                default: return "";
            }
        }
    }
}
