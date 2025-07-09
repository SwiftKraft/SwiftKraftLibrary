// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;

    internal class SplittableRect
    {
        internal float Size { private set; get; }

        private bool isResizing = false;
        private float barSize;
        private bool horizontal;
        private float min;
        private float max;
        private Rect barRect;

        internal SplittableRect(float startSize, bool horizontal, float barSize, float min = 0, float max = 0)
        {
            this.horizontal = horizontal;
            this.barSize = barSize;

            SetSize(startSize);
            SetLimits(min, max);

            if (horizontal)
            {
                barRect = new Rect(startSize, 0, barSize, 1);
            }
            else
            {
                barRect = new Rect(0, startSize, 1, barSize);
            }
        }

        internal void SetSize(float size)
        {
            Size = size;
        }

        internal void SetLimits(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        internal void Draw(Rect rect, float shift)
        {
            SetSize(rect, shift);


            EditorGUIUtility.AddCursorRect(barRect, horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);
            //GUI.Box(barRect, GUIContent.none, EditorStyles.foldout);
            //GUI.Button(barRect, GUIContent.none);
            //EditorGUI.DrawRect(barRect, new Color(.2f, .2f,.2f));

            if (Event.current.type == EventType.MouseDown && barRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                isResizing = true;
            }
            if (isResizing)
            {
                if (horizontal)
                {
                    Size = Event.current.mousePosition.x;
                }
                else
                {
                    Size = Event.current.mousePosition.y;
                }

                SetSize(rect, shift);
            }
            if (Event.current.rawType == EventType.MouseUp)
            {
                isResizing = false;
            }
        }

        internal void DrawCoverBox()
        {
            Color color = EditorGUIUtility.isProSkin ? new Color(.25f, .25f, .25f) : new Color(.65f, .65f, .65f);
            EditorGUI.DrawRect(barRect, color);
        }

        private void SetSize(Rect rect, float shift) {
            if (horizontal)
            {
                Size = Mathf.Clamp(Size, min, max);
                barRect.Set(rect.x+Size+shift, rect.y, barSize, rect.height);
            }
            else
            {
                Size = Mathf.Clamp(Size, min, max);
                barRect.Set(rect.x, rect.y+Size +shift, rect.width, barSize);
            }

        }

        internal void MouseLeftWindow()
        {
            isResizing = false;
        }
    }
}
