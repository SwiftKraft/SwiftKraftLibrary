using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
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

        protected virtual void Update()
        {
            foreach (Audio au in Sounds)
                au.Update();
        }

        [Serializable]
        public class Audio
        {
            public WeaponAudio Parent { get; private set; }

            public string Action;
            public AudioSource Source;
            public List<Clip> Override { get; set; }
            public Clip[] Clips;

            public AnimationCurve VolumeMultiplier;

            public Accumulator PlayedTimes = new();

            public int Repeat;
            public float RepeatDelay;

            public bool UseEvent;

            readonly Timer repeatTimer = new();
            int repeatCount;
            bool playing;

            public void Initialize(WeaponAudio audio)
            {
                Parent = audio;
                Source.playOnAwake = false;
                if (!UseEvent)
                    Parent.Parent.OnStartAction += Play;
                else
                    Parent.Parent.OnEvent += Play;
            }

            public void Destroy()
            {
                if (!UseEvent)
                    Parent.Parent.OnStartAction -= Play;
                else
                    Parent.Parent.OnEvent -= Play;
            }

            public void Update()
            {
                PlayedTimes.Tick(Time.deltaTime);

                if (!playing)
                    return;

                repeatTimer.Tick(Time.deltaTime);
                if (repeatTimer.Ended)
                {
                    Clip cl = Override != null ? Override.GetRandom() : Clips.GetRandom();
                    cl?.Play(Source, VolumeMultiplier.keys.Length > 0 ? VolumeMultiplier.EvaluateSafe(PlayedTimes.CurrentValue, 1f) : 1f);
                    PlayedTimes.Increment(1f);
                    repeatCount++;
                    if (repeatCount < Repeat + 1)
                        repeatTimer.Reset(RepeatDelay);
                    else
                    {
                        playing = false;
                        repeatCount = 0;
                        repeatTimer.Reset(0f);
                    }
                }
            }

            public void Play(string state)
            {
                if (Source == null)
                {
                    Debug.LogError("Source is not set! ", Parent);
                    return;
                }

                if (state != Action)
                    return;

                Play();
            }

            public void Play() => playing = true;

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

                public void Play(AudioSource source, float volumeMultiplier = 1f)
                {
                    source.priority = Priority;
                    source.pitch = Pitch * Time.timeScale;
                    source.volume = Volume * volumeMultiplier;
                    source.panStereo = StereoPan;
                    source.spatialBlend = SpatialBlend;
                    source.PlayOneShot(Sound);
                }
            }
        }
    }
}
