using UnityEngine;

namespace SwiftKraft.Gameplay.Playables
{
    [RequireComponent(typeof(PlayableAnimationController))]
    public class PlayableAnimationStatePlayer : MonoBehaviour
    {
        public int LayerIndex = 0;
        public PlayableAnimationState State;

        public PlayableAnimationController Controller { get; private set; }

        private void Awake()
        {
            Controller = GetComponent<PlayableAnimationController>();
            if (Controller.Layers.Count <= LayerIndex)
                LayerIndex = Controller.Layers.Count - 1;
        }

        [ContextMenu("Play")]
        public void Play() => Controller.Play(State, LayerIndex);
    }
}
