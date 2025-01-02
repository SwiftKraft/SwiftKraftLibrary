using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAudio : WeaponComponent
    {
        public Audio[] Sounds;

        protected virtual void Awake()
        {
            foreach (Audio au in Sounds)
                au.Initialize(this);
        }

        protected virtual void OnDestroy()
        {
            foreach (Audio au in Sounds)
                au.Destroy();
        }

        [Serializable]
        public class Audio
        {
            public WeaponAudio Parent { get; private set; }

            public string Action;
            public AudioSource Source;
            public Clip[] Clips;

            public void Initialize(WeaponAudio audio)
            {
                Parent = audio;
                Source.playOnAwake = false;
                Parent.Parent.OnStartAction += Play;
            }

            public void Destroy() => Parent.Parent.OnStartAction -= Play;

            public void Play(string state)
            {
                if (Source == null)
                {
                    Debug.LogError("Source is not set! ", Parent);
                    return;
                }

                if (state != Action)
                    return;

                Clip cl = Clips.GetRandom();
                cl?.Play(Source);
            }

            [Serializable]
            public class Clip
            {
                public AudioClip Sound;
                [Range(0, 256)]
                public int Priority = 128;
                [Range(-3f, 3f)]
                public float Pitch = 1f;
                [Range(0f, 1f)]
                public float Volume = 1f;
                [Range(-1f, 1f)]
                public float StereoPan = 0f;
                [Range(0f, 1f)]
                public float SpatialBlend = 1f;

                public void Play(AudioSource source)
                {
                    source.clip = Sound;
                    source.priority = Priority;
                    source.pitch = Pitch;
                    source.volume = Volume;
                    source.panStereo = StereoPan;
                    source.spatialBlend = SpatialBlend;
                    source.Play();
                }
            }
        }
    }
}
