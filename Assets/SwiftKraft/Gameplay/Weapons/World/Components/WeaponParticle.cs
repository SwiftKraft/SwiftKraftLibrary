using SwiftKraft.Utils;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponParticle : WeaponComponent
    {
        public Particle[] Particles;

        public void SetOverride(string action, GameObject prefab)
        {
            Particle par = Particles.FirstOrDefault(p => p.Action == action);

            if (par == null)
                return;
            
            if (prefab == null || !prefab.TryGetComponent(out ParticleSystem sys))
                par.SetOverride(null);
            else
                par.SetOverride(sys);
        }

        protected virtual void Awake()
        {
            foreach (Particle par in Particles)
                par.Initialize(this);
        }

        protected virtual void Update()
        {
            foreach (Particle par in Particles)
                par.Update();
        }

        protected virtual void OnDestroy()
        {
            foreach (Particle par in Particles)
                par.Destroy();
        }

        [Serializable]
        public class Particle
        {
            public WeaponParticle Parent { get; private set; }

            public string Action;
            public ParticleSystem ParticleSystem;
            public ParticleSystem Override { get; private set; }
            public int Repeat;
            public float RepeatDelay;

            public bool UseEvent;
            readonly Timer repeatTimer = new();
            int repeatCount;
            bool playing;

            public void Initialize(WeaponParticle parent)
            {
                Parent = parent;
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

            public void Play(string state)
            {
                if (ParticleSystem == null)
                {
                    Debug.LogError("Particle reference not set!", Parent);
                    return;
                }

                if (state != Action)
                    return;

                Play();
            }

            public void Play() => playing = true;

            public void SetOverride(ParticleSystem prefab)
            {
                if (Override != null)
                    Object.Destroy(Override.gameObject);

                if (prefab != null)
                    Override = Instantiate(prefab, ParticleSystem.transform.parent);
            }

            public void Update()
            {
                if (!playing)
                    return;

                repeatTimer.Tick(Time.deltaTime);
                if (repeatTimer.Ended)
                {
                    ParticleSystem sys = Override != null ? Override : ParticleSystem;
                    sys.Play();
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
        }
    }
}
