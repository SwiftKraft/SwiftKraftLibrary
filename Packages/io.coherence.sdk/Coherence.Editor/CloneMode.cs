namespace Coherence.Editor
{
#if HAS_MPPM
    using System;
#endif
    using UnityEditor;

    /// <summary>
    /// Handles Clone Mode for the current Editor instance. Clone Mode disables asset post-processing and
    /// a few mechanisms like baking. It's recommended to enable Clone Mode on any additional Editor instance opened
    /// for a project, so that there's no chance for asset pipeline automations to hit on those instances (referred to
    /// as Clones).
    /// </summary>
    [InitializeOnLoad]
    public static class CloneMode
    {
        private const string AllowEditsKey = "Coherence.CloneMode.AllowEdits";

        /// <summary>
        /// Enables or disables Clone Mode.
        /// </summary>
        public static bool Enabled { get; set; } = false;

        /// <summary>
        /// Allows modification of assets through the Editor interface.
        /// </summary>
        public static bool AllowEdits
        {
            get => UserSettings.GetBool(AllowEditsKey, false);
            set => UserSettings.SetBool(AllowEditsKey, value);
        }

        static CloneMode()
        {
#if HAS_PARRELSYNC
            Enabled |= ParrelSync.ClonesManager.IsClone();
#endif

#if HAS_MPPM
            Enabled |= Array.IndexOf(Environment.GetCommandLineArgs(), "--virtual-project-clone") != -1;
#endif
        }
    }
}
