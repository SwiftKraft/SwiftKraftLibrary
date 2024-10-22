namespace SwiftKraft.Saving.Progress
{
    public static class ProgressManager
    {
        /// <summary>
        /// The array of folders leading up to the stored saves.
        /// </summary>
        public readonly static string[] SavesFilePath = { "Saves" };

        public static ProgressSave Current
        {
            get;
            set;
        }
    }
}
