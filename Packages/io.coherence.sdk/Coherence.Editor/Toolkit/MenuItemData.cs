// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEngine;

    public class MenuItemData
    {
        public GUIContent content;
        public GUIContent contentHover;
        public bool enabled = true;
        public bool isOn;
        public GenericMenu.MenuFunction2 function;
    }
}

