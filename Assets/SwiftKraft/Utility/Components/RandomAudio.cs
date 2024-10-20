using UnityEngine;

namespace SwiftKraft.Utils
{
    [RequireComponent(typeof(AudioSource))]
    public class RandomAudio : MonoBehaviour
    {
        public AudioSource Audio { get; private set; }

        public AudioClip[] Clips;

        public float Chance = 1f;

        int lastRandom = -1;

        private void Awake() => Audio = GetComponent<AudioSource>();

        public void Play(float chanceOverride = -1f)
        {
            if (chanceOverride < 0f)
                chanceOverride = Chance;

            if (chanceOverride == 1f || Random.Range(0f, 1f) <= chanceOverride)
                Audio.PlayOneShot(Clips.GetRandom(ref lastRandom));
            else
                Audio.Stop();
        }
    }
}