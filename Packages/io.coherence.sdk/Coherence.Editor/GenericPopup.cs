// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    internal sealed class GenericPopup : PopupWindowContent
    {
        private Action fnDraw;
        private Func<Vector2> fnSize;
        private Action fnOpen;
        private Action fnClose;

        private Vector2 scroll;

        public GenericPopup(Action fnDraw, Func<Vector2> fnSize, Action fnOpen, Action fnClose)
        {
            this.fnDraw = fnDraw;
            this.fnSize = fnSize;
            this.fnOpen = fnOpen;
            this.fnClose = fnClose;
        }

        public GenericPopup(Action fnDraw, Func<Vector2> fnSize) : this(fnDraw, fnSize, null, null)
        {
        }

        public override Vector2 GetWindowSize()
        {
            return fnSize != null ? fnSize() : new Vector2(100, 100);
        }

        public override void OnGUI(Rect rect)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            fnDraw?.Invoke();
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.MouseMove)
            {
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            if ((Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand) &&
                Event.current.commandName == "Close")
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        public void Close()
        {
            editorWindow?.Close();
        }

        public override void OnOpen()
        {
            EditorApplication.modifierKeysChanged += editorWindow.Repaint;
            fnOpen?.Invoke();
        }

        public override void OnClose()
        {
            EditorApplication.modifierKeysChanged -= editorWindow.Repaint;
            fnClose?.Invoke();
        }

        public static void SendCloseCommand()
        {
            var existingWindows = Resources.FindObjectsOfTypeAll(typeof(PopupWindow));
            if (existingWindows != null && existingWindows.Length > 0)
            {
                var popupWindow = existingWindows[0] as PopupWindow;
                var e = EditorGUIUtility.CommandEvent("Close");
                _ = popupWindow.SendEvent(e);
            }
        }

        public static void Repaint()
        {
            var existingWindows = Resources.FindObjectsOfTypeAll(typeof(PopupWindow));
            if (existingWindows != null && existingWindows.Length > 0)
            {
                var popupWindow = existingWindows[0] as PopupWindow;
                popupWindow.Repaint();
            }
        }
    }
}
