// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Archetypes;
    using Coherence.Toolkit.Bindings;
    using Interpolation;
    using Log;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;
    using Logger = Log.Logger;

    internal class BindingsTreeViewBindingItem : BindingsTreeViewItem
    {
        public Binding Binding { get; private set; }
        public ArchetypeComponent BoundComponent => boundComponent;
        public BindingsTreeViewComponentItem ComponentItem { private set; get; }

        public bool SelectedForSync { private set; get; }
        public bool IsMethod => Binding.IsMethod;
        public bool SelectedInTreeView { protected get; set; }

        private BindingsWindow bindingsWindow;
        private ArchetypeComponent boundComponent;
        private BindingArchetypeData archetypeData;
        private readonly CoherenceSync sync;
        private string cantEditString;
        private readonly Binding bindingOnEditorCache;
        private WarningSource warningSource;
        private static readonly Lazy<Logger> logger = new(() => Log.GetLogger<BindingsTreeViewBindingItem>());
        private static Logger Logger => logger.Value;

        internal bool CanOverride { private set; get; }
        internal SchemaType SchemaType { private set; get; }

        internal static Action<BindingsTreeViewInput, BindingsTreeViewBindingItem> OnInput;

        private readonly List<string> warningList = new List<string>();
        private readonly StringBuilder warningsTooltipBuilder = new StringBuilder(512);

        internal BindingsTreeViewBindingItem(CoherenceSync sync, ArchetypeComponent boundComponent, Binding binding)
        {
            this.sync = sync;
            this.boundComponent = boundComponent;

            binding.Activate();
            Binding = binding;
            bindingOnEditorCache = binding;
            displayName = binding.Name;

            SelectedForSync = IsActiveOnSync();
            sync.Archetype.SetBindingActive(binding, boundComponent, SelectedForSync);
            UpdateActiveTarget();
        }

        internal void SetTreeViewData(int id, BindingsTreeViewComponentItem componentItem, BindingsWindow bindingsWindow)
        {
            // TreeviewSpecific
            this.id = id;
            this.bindingsWindow = bindingsWindow;

            var group = IsMethod ? componentItem.Methods : componentItem.Fields;
            group.AddMember(this);
            componentItem.AddChild(this);
            depth = 1;

            ComponentItem = componentItem;

            GetCantEditText();
        }

        protected void SetArchetypeData(ArchetypeComponent boundComponent, BindingArchetypeData archetypeData)
        {
            this.boundComponent = boundComponent;
            this.archetypeData = archetypeData;

            CanOverride = archetypeData.CanOverride;
            SchemaType = archetypeData.SchemaType;
            Setup(rowHeight: 22, lodSteps: boundComponent.MaxLods);

            GetCantEditText();
        }

        private void GetCantEditText()
        {
            if (!CanOverride)
            {
                if (IsMethod)
                {
                    cantEditString = "";
                    return;
                }

                int bits = BindingLODStepData.GetDefaultBits(SchemaType);
                if (ContentUtils.TryGetSchemaTypeContent(SchemaType, out GUIContent content))
                {
                    cantEditString = $"{content.tooltip} is always {bits} {(bits == 1 ? "Bit" : "Bits")}";
                }
                else
                {
                    if (bits == 0)
                    {
                        cantEditString = $"Unknown bandwith cost";
                    }
                    else
                    {
                        cantEditString = $"This type is always {bits} {(bits == 1 ? "Bit" : "Bits")}";
                    }
                }
            }
        }

        internal void SetBindingActive(bool active)
        {
            if (!GetCanBeSelectedForSync())
            {
                active = true;
            }
            if (active != SelectedForSync || IsActiveOnSync() != active)
            {
                SelectedForSync = active;
                sync.Archetype.SetBindingActive(Binding, boundComponent, active);
            }
        }

        internal GUIContent GetNameAndIconGUIContent() => ContentUtils.GetContent(Binding.UnityComponent, Binding.Descriptor);
        protected bool GetCanBeSelectedForSync() => !Binding.Descriptor.Required;
        protected bool IsActiveOnSync() => sync.Bindings.Contains(bindingOnEditorCache);

        protected void UpdateActiveTarget()
        {
            if (IsActiveOnSync())
            {
                Binding = CoherenceSyncBindingHelper.GetSerializedBinding(sync, bindingOnEditorCache);
            }
            else
            {
                Binding = bindingOnEditorCache;
            }

            SetArchetypeData(boundComponent, Binding.archetypeData);
        }

        internal bool GetCanInterpolate() => !Binding.IsMethod;

        internal void SetInterpolation(InterpolationSettings interpolationSettings)
        {
            if (GetCanInterpolate())
            {
                Binding.interpolationSettings = interpolationSettings;
            }
        }

        protected void DrawMethod(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            MessageTarget routing = (MessageTarget)EditorGUI.EnumPopup(rect, GUIContent.none, ((CommandBinding)Binding).routing);
            if (EditorGUI.EndChangeCheck())
            {
                var edit = new BindingsTreeViewInput(-1, BindingsTreeViewInput.Type.MessageTarget, routing, "Changed Routing");
                ApplyInput(edit);
            }
        }

        internal override void DrawRowBackground(Rect rowRect)
        {
            Color color = SelectedInTreeView ? BindingsWindowSettings.RowSelectedColor : BindingsWindowSettings.RowColor;
            EditorGUI.DrawRect(rowRect, color);
            base.DrawRowBackground(rowRect);
        }

        protected override void DrawLeftBar(Rect rect)
        {
            int iconSize = 14;
            int menuSize = 16;
            int warningSize = 16;
            int inset = 25;

            // Get rects
            Rect iconRect = new Rect(rect.x + 8, rect.y + ((rect.height - iconSize) * .5f), iconSize, iconSize);
            Rect nameRect = new Rect(rect.x + inset, rect.y, rect.width - inset - menuSize, rect.height);
            Rect menuRect = new Rect(rect.xMax - menuSize, rect.y, menuSize, rect.height);

            // Draw Icon
            if (bindingsWindow.Toolbar.Filters.AnyTypeFiltersActive() || bindingsWindow.Toolbar.Filters.PopupOpen)
            {
                DrawIcon(iconRect);
            }
            else
            {
                nameRect.x -= menuSize;
                nameRect.width += menuSize;
            }

            // Draw Warnings
            if (HasWarnings())
            {
                nameRect.width -= warningSize;
                Rect warningRect = new Rect(menuRect.x - warningSize, menuRect.y, warningSize, menuRect.height);

                warningsTooltipBuilder.Length = 0;
                foreach (string warning in warningList)
                {
                    warningsTooltipBuilder.AppendLine(warning);
                }

                warningsTooltipBuilder.AppendLine();
                warningsTooltipBuilder.Append("Click on this icon to automatically fix issues.");

                string helpText = warningsTooltipBuilder.ToString();

                GUIContent icon = EditorGUIUtility.IconContent("Warning");
                GUIContent helpbox = new GUIContent(icon.image, helpText);

                if (GUI.Button(warningRect, helpbox, EditorStyles.label))
                {
                    FixAllWarnings();
                }
            }

            // Draw label/ bindings
            GUIContent content = GetNameAndIconGUIContent();

            if (BindingsWindow.EditingAllFields || ComponentItem.EditingLocalBindings)
            {
                EditorGUI.BeginDisabledGroup(!GetCanBeSelectedForSync());
                Rect toggleRect = nameRect.SplitX(true, 20);
                nameRect = nameRect.SplitX(false, 20);

                bool synced = EditorGUI.Toggle(toggleRect, GUIContent.none, SelectedForSync);
                if (SelectedForSync != synced)
                {
                    BindingsTreeViewInput input = new BindingsTreeViewInput(-1, BindingsTreeViewInput.Type.Selected, synced ? 1 : 0, "Sync selection changed");
                    ApplyInput(input);
                }
                EditorGUI.EndDisabledGroup();
            }

            GUI.Label(nameRect, content, ContentUtils.GUIStyles.richLabel);

            CreateBindingsCommandMenu(menuRect);
        }

        protected override void DrawBindingConfigBar(Rect rect)
        {
            RectOffset padding = new RectOffset(2, 2, 2, 2);
            rect = padding.Remove(rect);

            EditorGUI.BeginDisabledGroup(!SelectedForSync);
            if (IsMethod)
            {
                DrawMethod(rect);
            }
            else
            {
                DrawInterpolation(rect);
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawCompressionTypeBar(Rect rect)
        {
            if (IsMethod || !CanOverride)
            {
                return;
            }

            if (IsFloatType())
            {
                EditorGUI.BeginChangeCheck();

                FloatCompression compression = (FloatCompression)EditorGUI.EnumPopup(rect, archetypeData.FloatCompression);

                if (EditorGUI.EndChangeCheck())
                {
                    var edit = new BindingsTreeViewInput(0, BindingsTreeViewInput.Type.Compression, compression, "Changed Compression");
                    ApplyInput(edit);
                }
            }
        }

        protected override void DrawValueRangeBar(Rect rect)
        {
            if (IsMethod)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(!SelectedForSync);

            RectOffset padding = new RectOffset(2, 2, 2, 2);
            SplitInTwoLayoutRects layout = new SplitInTwoLayoutRects(rect, padding);

            if (CanOverride)
            {
                bool usesRange = SchemaType == SchemaType.Int ||
                    SchemaType == SchemaType.UInt ||
                    (IsFloatType() && archetypeData.FloatCompression == FloatCompression.FixedPoint);
                if (usesRange)
                {
                    DrawIntRangeInput(layout.FirstRect, layout.SecondRect, 0);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawSampleRateBar(Rect rect)
        {
            if (IsMethod)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(!SelectedForSync);

            RectOffset padding = new RectOffset(2, 2, 2, 2);
            SplitInTwoLayoutRects layout = new SplitInTwoLayoutRects(rect, padding);

            EditorGUI.BeginChangeCheck();

            var newSampleRate = EditorGUI.DelayedFloatField(layout.FirstRect, archetypeData.SampleRate);

            if (EditorGUI.EndChangeCheck())
            {
                var edit = new BindingsTreeViewInput(0, BindingsTreeViewInput.Type.SampleRate,
                    newSampleRate, "Changed SampleRate");
                ApplyInput(edit);
            }

            if (newSampleRate <= 0)
            {
                var edit = new BindingsTreeViewInput(0, BindingsTreeViewInput.Type.SampleRate,
                    InterpolationSettings.DefaultSampleRate, "Changed SampleRate");
                ApplyInput(edit);
            }

            GUI.Label(layout.SecondRect, new GUIContent("hz"));

            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawStatisticsBar(Rect rect)
        {
            if (IsMethod)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(!SelectedForSync);
            RectOffset padding = new RectOffset(2, 2, 2, 2);
            rect = padding.Remove(rect);

            if (boundComponent.MaxLods > 0)
            {
                int[] values = new int[boundComponent.MaxLods];

                int multiplier = ArchetypeMath.GetBitsMultiplier(archetypeData.SchemaType);
                for (int i = 0; i < boundComponent.MaxLods; i++)
                {
                    BindingLODStepData field = archetypeData.GetLODstep(i);
                    values[i] = boundComponent.LodStepsActive > i ? field.Bits : 0;
                }

                if (!IsIntType() && SelectedForSync && CanOverride)
                {
                    int maxValue = 32;
                    if (archetypeData.FloatCompression == FloatCompression.FixedPoint)
                    {
                        double maxPrecision = ArchetypeMath.GetRoundedPrecisionByBitsAndRange(32, (uint)(archetypeData.MaxRange - archetypeData.MinRange));
                        ArchetypeMath.TryGetBitsForFixedFloatValue(archetypeData.MinRange, archetypeData.MaxRange, maxPrecision, out maxValue);
                    }

                    EditorGUI.BeginChangeCheck();
                    int editedStep = CoherenceArchetypeDrawer.DrawEditableDataWeightMiniBar(rect, ref values, boundComponent.MaxLods, maxValue, multiplier);

                    if (EditorGUI.EndChangeCheck() && editedStep >= 0)
                    {
                        BindingsTreeViewInput input = new BindingsTreeViewInput(editedStep, BindingsTreeViewInput.Type.Bits, values[editedStep], "Changed Bits");
                        ApplyInput(input);
                    }
                }
                else
                {
                    CoherenceArchetypeDrawer.DrawDataWeightMiniBar(rect, values, boundComponent.MaxLods, CanOverride);
                    if (!CanOverride)
                    {
                        GUIContent toolTip = new GUIContent("", cantEditString);
                        GUI.Label(rect, toolTip);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawLOD(Rect rect, int step)
        {
            if (IsMethod)
            {
                return;
            }

            bool enabledOnComponent = boundComponent.LodStepsActive > step;
            if (enabledOnComponent)
            {
                EditorGUI.BeginDisabledGroup(!SelectedForSync || !enabledOnComponent);

                BindingLODStepData field = archetypeData.GetLODstep(step);
                if (field != null)
                {
                    RectOffset padding = new RectOffset(2, 2, 2, 2);
                    Rect rectForInput = new Rect(rect);

                    if (BindingsWindowSettings.ShowBitPercentages)
                    {
                        // Rect changes
                        float width = BindingsWindowSettings.LODBitPercentageWidth;
                        rectForInput.width -= width;

                        Rect textRect = new Rect(rect.xMax - width, rect.y, width, rect.height);

                        DrawBitPercentage(textRect, field.TotalBits, boundComponent.GetTotalBitsOfLOD(step), "", " of the bits of this component");
                    }

                    LODLayoutRects layout = new LODLayoutRects(rectForInput, BindingsWindowSettings.CanEditLODRanges, padding);

                    if (CanOverride)
                    {
                        if (IsIntType() && IsRangedType())
                        {
                            DrawIntBits(layout.Bits, field);
                            DrawIntRangeInput(layout.MinRange, layout.MaxRange, step);
                        }
                        else if (IsFloatType())
                        {
                            bool bitsInputDisabled = archetypeData.FloatCompression == FloatCompression.None || archetypeData.FloatCompression == FloatCompression.FixedPoint;
                            if (bitsInputDisabled)
                            {
                                DrawIntBits(layout.Bits, field);
                            }
                            else
                            {
                                DrawBasicBitsInput(layout.Bits, field, step);
                            }

                            if (archetypeData.FloatCompression != FloatCompression.None)
                            {
                                bool precisionInputDisabled = archetypeData.FloatCompression != FloatCompression.FixedPoint;
                                string unit = string.Empty;
                                if (archetypeData.FloatCompression == FloatCompression.Truncated)
                                {
                                    unit = "%";
                                }

                                EditorGUI.BeginDisabledGroup(precisionInputDisabled);
                                {
                                    DrawPrecisionResult(layout.Precision, field, step, unit);
                                }
                                EditorGUI.EndDisabledGroup();
                            }
                        }
                        else if (SchemaType == SchemaType.Color)
                        {
                            DrawBasicBitsInput(layout.Bits, field, step);

                            EditorGUI.BeginDisabledGroup(true);
                            {
                                DrawPrecisionResult(layout.Precision, field, step);
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        else if (SchemaType == SchemaType.Quaternion)
                        {
                            DrawBasicBitsInput(layout.Bits, field, step);

                            EditorGUI.BeginDisabledGroup(true);
                            {
                                DrawPrecisionResult(layout.Precision, field, step, "°");
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                    }

                    GUIStyle miniText = new GUIStyle(EditorStyles.miniLabel);
                    miniText.alignment = TextAnchor.MiddleRight;
                    string bits = BindingsWindowSettings.CompactView ? " b" : (field.TotalBits == 1 ? " Bit" : " Bits");
                    EditorGUI.LabelField(layout.BitTotal, $"{field.TotalBits}{bits}", miniText);

                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        protected void DrawIcon(Rect rect)
        {
            GUIContent icon = GUIContent.none;
            if (IsMethod)
            {
                GUI.DrawTexture(rect, ContentUtils.GUIContents.command.image);
            }
            else if (ContentUtils.TryGetSchemaTypeContent(archetypeData.SchemaType, out icon))
            {
                GUI.DrawTexture(rect, icon.image);
            }

            RectOffset rectOffset = new RectOffset(2, 2, 2, 4);
            GUI.Label(rectOffset.Add(rect), ContentUtils.GUIContents.binding);
        }

        private void DrawIntBits(Rect rect, BindingLODStepData field)
        {
            int multiplier = ArchetypeMath.GetBitsMultiplier(field.SchemaType);

            GUIStyle miniText = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            miniText.alignment = TextAnchor.MiddleLeft;

            if (multiplier > 1)
            {
                Rect multiplierrect = new Rect(rect.x + 35, rect.y, 20, rect.height);
                GUI.Label(multiplierrect, $"*{multiplier}", miniText);
            }

            EditorGUI.LabelField(rect, $"{field.Bits} {(field.Bits == 1 ? "Bit" : "Bits")}", miniText);
        }

        private void DrawIntRangeInput(Rect minRect, Rect maxRect, int step)
        {
            EditorGUI.BeginChangeCheck();

            var newMin = BindingsTreeViewIntHelper.DrawField(minRect, archetypeData.MinRange, BindingsTreeViewInput.Type.MinRange,
                step, SelectedInTreeView, warningSource?.Active ?? false);

            if (EditorGUI.EndChangeCheck())
            {
                var edit = new BindingsTreeViewInput(step, BindingsTreeViewInput.Type.MinRange, newMin, "Changed MinRange");
                ApplyInput(edit);
            }
            EditorGUI.BeginChangeCheck();

            var newMax = BindingsTreeViewIntHelper.DrawField(maxRect, archetypeData.MaxRange, BindingsTreeViewInput.Type.MaxRange,
                step, SelectedInTreeView, warningSource?.Active ?? false);

            if (EditorGUI.EndChangeCheck())
            {
                var edit = new BindingsTreeViewInput(step, BindingsTreeViewInput.Type.MaxRange, newMax, "Changed MaxRange");
                ApplyInput(edit);
            }
        }

        private void DrawBasicBitsInput(Rect rect, BindingLODStepData field, int step)
        {
            EditorGUI.BeginChangeCheck();
            int bits = BindingsTreeViewFloatHelper.DrawBindingBitsInput(rect, field, step, SelectedInTreeView);

            if (EditorGUI.EndChangeCheck())
            {
                var edit = new BindingsTreeViewInput(step, BindingsTreeViewInput.Type.Bits, bits, "Changed Bits");
                ApplyInput(edit);
            }
        }

        private void DrawPrecisionResult(Rect rect, BindingLODStepData field, int step, string unit = "")
        {
            double precision = PrecisionPopupDrawer.Draw(rect, archetypeData, field, bindingsWindow, unit);
            if (precision >= 0 && precision != field.Precision)
            {
                var edit = new BindingsTreeViewInput(step, BindingsTreeViewInput.Type.Precision, precision, "Changed Precision");
                ApplyInput(edit);
            }
        }

        protected virtual void DrawInterpolation(Rect rect)
        {
            if (!GetCanInterpolate())
            {
                return;
            }

            var idx = sync.Bindings.IndexOf(Binding);
            if (idx == -1)
            {
                return;
            }

            var path = $"bindings.Array.data[{idx}].{nameof(Binding.interpolationSettings)}";
            using var p = new SerializedObject(sync).FindProperty(path);
            _ = EditorGUI.PropertyField(rect, p);
        }

        protected void ApplyInput(BindingsTreeViewInput input) => OnInput?.Invoke(input, this);

        internal void ApplyInputToBinding(BindingsTreeViewInput input, bool saveToDisk)
        {
            input.ApplyToBinding(archetypeData, this);

            if (sync)
            {
                var bindingIndex = sync.Bindings.FindIndex(x => x.guid == Binding.guid);
                if (bindingIndex is not -1)
                {
                    sync.Bindings[bindingIndex].archetypeData = archetypeData;
                }
                else
                {
                    Logger.Context = sync;
                    Logger.Warning(Warning.ConfigurationWindowWarning, $"Binding {Binding.guid} was not found on '{sync}' when trying to save changes to disk.");
                }
            }
            else
            {
                Logger.Warning(Warning.ConfigurationWindowWarning, $"Sync was null when trying to save changes made to binding {Binding.guid} to disk.");
            }

            if (saveToDisk)
            {
                bindingsWindow.UpdateSerialization();
            }
        }

        internal int GetBitsOfLOD(int step) => archetypeData.GetLODstep(step).TotalBits;
        internal override bool CheckIfFilteredOut(BindingsWindowTreeFilters filters, bool bindingCanBeEdited) => filters.FilterOutBinding(SchemaType, displayName);

        private void CreateBindingsCommandMenu(Rect rect)
        {
            if (!bindingsWindow.StateController.Lods)
            {
                return;
            }

            bool canResetRanges = IsRangedType();
            bool canResetBitsAndPrecision = IsFloatType() || BindingArchetypeData.IsBitsBased(SchemaType);

            if (canResetRanges || canResetBitsAndPrecision)
            {
                if (GUI.Button(rect, EditorGUIUtility.IconContent("_Menu"), EditorStyles.label))
                {
                    var menu = new GenericMenu();

                    if (canResetRanges && canResetBitsAndPrecision)
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Reset All"), false, () => ResetToAllToDefaultValues(BindingsTreeViewInput.BindingReset.All));
                    }

                    if (canResetRanges)
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Reset Ranges"), false, () => ResetToAllToDefaultValues(BindingsTreeViewInput.BindingReset.RangesOnly));
                    }

                    if (canResetBitsAndPrecision)
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Reset Bits and Precision"), false, () => ResetToAllToDefaultValues(BindingsTreeViewInput.BindingReset.BitsAndPrecisionOnly));
                    }

                    if(HasPrefabVariants())
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Apply To All Variants"), false, ApplyToAllVariants);
                    }

                    menu.DropDown(rect);
                }
            }
        }

        private void ApplyToAllVariants()
        {
            var variantsConfigs = GetAllPrefabVariantConfigs().ToArray();
            int variantCount = variantsConfigs.Length;

            string title, message;
            if(variantCount == 1)
            {
                title = $"Apply to {variantsConfigs[0].Sync.name}?";
                message = $"Apply {ObjectNames.NicifyVariableName(Binding.Name)}'s optimizations from '{sync.name}' into '{variantsConfigs[0].Sync.name}'?";
            }
            else
            {
                title = $"Apply to {variantsConfigs.Length} variants?";
                message = $"Apply {ObjectNames.NicifyVariableName(Binding.Name)}'s optimizations from '{sync.name}' into {variantsConfigs.Length} prefab variants?\n\n";
                if(variantCount <= 10)
                {
                    message += string.Join("\n", variantsConfigs.Select(x => x.Sync.name));
                }
                else
                {
                    message += string.Join("\n", variantsConfigs.Take(10).Select(x => x.Sync.name));
                    message += "\n...";
                }
            }

            if(!EditorUtility.DisplayDialog(title, message, "Apply", "Cancel"))
            {
                return;
            }

            foreach (var variantSyncConfig in variantsConfigs)
            {
                var variantSync = variantSyncConfig.Sync;
                if (!variantSync)
                {
                    continue;
                }

                var guid = Binding.guid;
                foreach(var variantBinding in variantSync.Bindings)
                {
                    if(!string.Equals(variantBinding.guid, guid))
                    {
                        continue;
                    }

                    Undo.RecordObject(variantSync, "Apply To All Variants");
                    variantBinding.archetypeData = Binding.archetypeData;
                    break;
                }
            }

            Debug.Log($"Optimizations successfully applied to all {variantsConfigs.Length} variants.");
        }

        internal void ResetToAllToDefaultValues(BindingsTreeViewInput.BindingReset bindingReset)
        {
            var edit = new BindingsTreeViewInput(-1, BindingsTreeViewInput.Type.Reset, bindingReset, "Reset bindings");
            ApplyInput(edit);
        }

        internal void ResetValuesToDefault(bool resetRanges, bool resetBitsAndPrecision)
            => archetypeData.ResetValuesToDefault(Binding.MonoAssemblyRuntimeType, resetRanges, resetBitsAndPrecision);

        internal override bool CanChangeExpandedState() => false;

        private bool IsRangedType() => archetypeData.IsRangeType();

        internal bool IsIntType()
        {
            return SchemaType == SchemaType.Int ||
                SchemaType == SchemaType.UInt ||
                SchemaType == SchemaType.Int64 ||
                SchemaType == SchemaType.UInt64 ||
                SchemaType == SchemaType.Int8 ||
                SchemaType == SchemaType.UInt8 ||
                SchemaType == SchemaType.Int16 ||
                SchemaType == SchemaType.UInt16 ||
                SchemaType == SchemaType.Char;
        }

        private bool IsFloatType() => archetypeData.IsFloatType;

        internal void SetGlobalWarning(string message)
        {
            warningSource = new WarningSource(message);
            bindingsWindow.SetWarning(warningSource.Token);
        }

        internal void ClearGlobalWarning() => bindingsWindow.ClearWarning();

        private bool HasWarnings()
        {
            warningList.Clear();

            if (archetypeData.SchemaType == SchemaType.Quaternion)
            {
                for (int i = 0; i < boundComponent.LodStepsActive; i++)
                {
                    var lodStep = archetypeData.GetLODstep(i);
                    if (lodStep.Bits < 12)
                    {
                        warningList.Add($"LOD {i} uses less than 12 bits ({lodStep.Bits}) per component. Due to the way quaternions are serialized this " +
                                        $"may severely affect quality of the quaternion syncing and is not recommended.");
                    }
                }
            }

            return warningList.Count > 0;
        }

        private void FixAllWarnings()
        {
            if (archetypeData.SchemaType == SchemaType.Quaternion)
            {
                for (int i = 0; i < boundComponent.LodStepsActive; i++)
                {
                    var lodStep = archetypeData.GetLODstep(i);
                    if (lodStep.Bits < 12)
                    {
                        ApplyInput(new BindingsTreeViewInput(i, BindingsTreeViewInput.Type.Bits, 12, "Fixed quaternion bits"));
                    }
                }
            }
            bindingsWindow.UpdateSerialization();
            warningList.Clear();
        }

        private bool HasPrefabVariants() => GetAllPrefabVariantConfigs().Any();

        private IEnumerable<CoherenceSyncConfig> GetAllPrefabVariantConfigs()
        {
            foreach (var item in CoherenceSyncConfigRegistry.Instance)
            {
                var itemSync = item?.Sync;
                if (!itemSync || PrefabUtility.GetPrefabAssetType(itemSync) != PrefabAssetType.Variant)
                {
                    continue;
                }

                for(var basePrefabSync = PrefabUtility.GetCorrespondingObjectFromSource(itemSync); basePrefabSync; basePrefabSync = PrefabUtility.GetCorrespondingObjectFromSource(basePrefabSync))
                {
                    if(ReferenceEquals(basePrefabSync, sync))
                    {
                        yield return item;
                        break;
                    }
                }
            }
        }
    }
}
