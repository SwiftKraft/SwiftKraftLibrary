using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwiftKraft.Gameplay.Scenes
{
    public class SceneLoader : MonoBehaviour
    {
        public static AsyncOperation CurrentLoadOperation { get; private set; }

        public static bool Loading => CurrentLoadOperation != null && !CurrentLoadOperation.isDone;

        public virtual void Load(int sceneIndex) => SceneManager.LoadScene(sceneIndex);

        public virtual void LoadAsync(int sceneIndex)
        {
            if (Loading)
                return;

            CurrentLoadOperation = SceneManager.LoadSceneAsync(sceneIndex);
        }
    }
}
