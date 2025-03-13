using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponParticle : WeaponComponent
    {
        public Particle[] Particles;

        protected virtual void Awake()
        {
            foreach (Particle par in Particles)
                par.Initialize(this);
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

            public void Initialize(WeaponParticle parent)
            {
                Parent = parent;
                Parent.Parent.OnStartAction += Play;
            }

            public void Destroy() => Parent.Parent.OnStartAction -= Play;

            public void Play(string state)
            {
                if (ParticleSystem == null)
                {
                    Debug.LogError("Particle reference not set!", Parent);
                    return;
                }

                if (state != Action)
                    return;

                ParticleSystem.Play();
            }
        }
    }
}
