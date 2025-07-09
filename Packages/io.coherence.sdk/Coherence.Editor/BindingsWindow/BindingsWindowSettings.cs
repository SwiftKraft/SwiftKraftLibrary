// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor {
    using UnityEditor;
	using UnityEngine;

    internal class BindingsWindowSettings
    {

        internal static readonly Vector2 MinSize = new (350, 300);
        internal static readonly GUIContent WindowTitle = Icons.GetContentWithText("EditorWindow", "Optimization");
        internal static readonly GUIContent Hint = EditorGUIUtility.TrTextContent("Compress data to reduce bandwidth load of this entity. Use LODs to compress data more when the entity is further away");

        // Layout of Window bar
        internal const int HeaderHeight = 32;
        internal const int ToolbarHeight = 20;
        internal const int LodFooterHeight = 45;
        internal const int FooterHieght = 24;

        // Tree sections

        internal static BindingsWindowTreeHeader.WidthSetting LeftBarSettings = new (200, 180, 500);
        internal static BindingsWindowTreeHeader.WidthSetting BindingConfigSettings = new (220, 220, 300);
        internal static BindingsWindowTreeHeader.WidthSetting StatisticsSettings = new (150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting ValueRangeSettings = new (150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting TypeSettings = new (150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting SampleRateSettings = new (150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting LODSettings = new (175, 175, 300);

        // Layout of LODs
        internal const float LODPrecisionFieldWidth = 65;
        internal const float LODBitDisplayWidth = 45;
        internal const float LODBitPercentageWidth = 26;

        internal static float LODConstantWidth;

        // MultiInput coloring
        internal static Color HighlightColor = new (0, .8f, .9f, 1);
        internal static Color WarningColor = new (1f, 0.76f, 0.03f);

        // User settings
        internal static readonly BoolSetting CanEditLODRanges = new(SettingsKeys.CanEditLODRanges, false);
        internal static readonly BoolSetting ShowBitPercentages = new(SettingsKeys.ShowBitPercentages, false);
        internal static readonly BoolSetting CompactView = new(SettingsKeys.CompactView, false);
        internal static readonly BoolSetting AutoSave = new(SettingsKeys.AutoSave, true);
        internal static readonly BoolSetting ShowStatistics = new(SettingsKeys.ShowStatistics, false);

        internal static Color HeaderColor => EditorGUIUtility.isProSkin ? DarkHeaderColor : LightHeaderColor;
        internal static Color RowColor => EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
        internal static Color RowSelectedColor => EditorGUIUtility.isProSkin ? DarkRowSelectedColor : LightRowSelectedColor;
        internal static Color ComponentRowColor => EditorGUIUtility.isProSkin ? DarkComponentRowColor : LightComponentRowColor;
        internal static Color HorizontalLineColor => EditorGUIUtility.isProSkin ? DarkHorizontalLineColor : LightHorizontalLineColor;
        internal static Color VerticalLineColor => EditorGUIUtility.isProSkin ? DarkVerticalLineColor : LightVerticalLineColor;
        public static BoolSetting[] All { get; } = { CanEditLODRanges, ShowBitPercentages, CompactView, AutoSave, ShowStatistics };

        // Row coloring
        private static Color LightRowColor = new (.83f, .83f, .83f, 1);
        private static Color LightRowSelectedColor = new (.89f, .89f, .89f, 1f);
        private static Color LightComponentRowColor = new (.69f, .69f, .69f, 1);
        private static Color LightHeaderColor = new (.65f, .65f, .65f, 1);

        private static Color LightHorizontalLineColor = new (0.80f, 0.80f, 0.80f, 1f);
        private static Color LightVerticalLineColor = new (0.5f, 0.5f, 0.5f, .4f);

        private static Color DarkRowColor = new (.22f, .22f, .22f, 1);
        private static Color DarkRowSelectedColor = new (.25f, .25f, .25f, 1f);
        private static Color DarkComponentRowColor = new (0.20f, 0.20f, 0.20f, 1);
        private static Color DarkHeaderColor = new (0.18f, 0.18f, 0.18f, 1);

        private static Color DarkHorizontalLineColor = new (0.2f, 0.2f, 0.2f, 1f);
        private static Color DarkVerticalLineColor = new (0.5f, 0.5f, 0.5f, .4f);

        static BindingsWindowSettings() => LODConstantWidth = LODPrecisionFieldWidth +LODBitDisplayWidth + (ShowBitPercentages ? LODBitPercentageWidth : 0);

        internal static void DrawSettings()
        {
            var titleButton = new GUIStyle(EditorStyles.toolbarButton);
            titleButton.alignment = TextAnchor.MiddleLeft;

            CanEditLODRanges.Value = false;

            var percetageContent = new GUIContent("%", "Show Percentages");
            ShowBitPercentages.DrawButton(percetageContent, EditorStyles.toolbarButton, HighlightColor);

            var compactViewContent = new GUIContent("Compact", "Show Percentages");
            CompactView.DrawButton(compactViewContent, EditorStyles.toolbarButton, HighlightColor);

            var statisticsContent = new GUIContent("Bandwidth", "Show bandwidth");
            ShowStatistics.DrawButton(statisticsContent, EditorStyles.toolbarButton, HighlightColor);
        }

        private static class SettingsKeys
        {
            private const string Prefix = "CoherenceSyncEditor";

            public const string CanEditLODRanges = Prefix + "UseAdvancedView";
            public const string ShowBitPercentages = Prefix + "ShowBitPercentages";
            public const string CompactView = Prefix + "CompactView";
            public const string AutoSave = Prefix + "AutoSave";
            public const string ShowStatistics = Prefix + "ShowStatistics";
        }
    }
}
