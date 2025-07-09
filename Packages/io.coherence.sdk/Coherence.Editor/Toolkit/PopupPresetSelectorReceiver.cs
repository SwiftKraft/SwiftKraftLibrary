// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if COHERENCE_OBJECT_PICKER_PRESETS
namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Presets;

    // TODO VladN 2023: if this is revived, PresetSelectorReceiver is marked deprecated in Unity 2023.1
    public class PopupPresetSelectorReceiver : PresetSelectorReceiver
    {
        private Object target;
        private Preset initialValue;

        public void Init(Object obj)
        {
            target = obj;
            initialValue = new Preset(target);
        }

        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObject(target, "Apply Preset " + selection.name);
                _ = selection.ApplyTo(target);
            }
            else
            {
                Undo.RecordObject(target, "Cancel Preset");
                _ = initialValue.ApplyTo(target);
            }

            GenericPopup.Repaint();
        }

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            DestroyImmediate(this);
        }
    }
}
#endif
