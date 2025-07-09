namespace Coherence.MatchmakingDialogSample
{
    using UnityEngine;

    public static class PlayerDataStore
    {
        private const string UsernamePrefsKey = "Coherence.MatchmakingDialogSample.Username";

        private static string name;

        public static string Name
        {
            get => name ??= PlayerPrefs.GetString(UsernamePrefsKey, "User");

            set
            {
                name = value ?? "";
                PlayerPrefs.SetString(UsernamePrefsKey, name);
            }
        }
    }
}
