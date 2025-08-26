using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    [RequireComponent(typeof(Toggle))]
    public class TimeButton : MonoBehaviour
    {
        public Toggle Toggle { get; private set; }

        private void Awake() => Toggle = GetComponent<Toggle>();

        private void Start() => Toggle.SetIsOnWithoutNotify(Time.timeScale == 0f);

        private void OnEnable() => Start();
    }
}
