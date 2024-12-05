using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Particles
{
    public static class ParticleManager
    {
        public static readonly Dictionary<string, List<ParticleSystem>> CachedParticles = new();

        public static bool CheckParticleRegistered(string id) => CachedParticles.ContainsKey(id);

        public static void RegisterParticle(this ParticleSystem particle, string id)
        {
            if (!CheckParticleRegistered(id))
                CachedParticles.Add(id, new List<ParticleSystem>());
            CachedParticles[id].Add(particle);
            ParticleSystem.MainModule m = particle.main;
            m.playOnAwake = false;
            m.stopAction = ParticleSystemStopAction.Disable;
            particle.gameObject.SetActive(false);
            CachedParticles[id].RemoveAll((p) => p == null);
        }

        public static ParticleSystem GetAvailableParticle(string id)
        {
            if (!CheckParticleRegistered(id))
            {
                Debug.LogWarning($"Particle with ID \"{id}\" is not found in registry! Make sure to register it.");
                return null;
            }

            ParticleSystem particle = CachedParticles[id].First((p) => !p.gameObject.activeSelf);

            if (particle == null) {
                particle = Object.Instantiate(CachedParticles[id][0]);
                particle.RegisterParticle(id);
            }

            return particle;
        }

        public static bool TryGetAvailableParticle(string id, out ParticleSystem particle)
        {
            particle = GetAvailableParticle(id);
            return particle != null;
        }

        public static ParticleSystem PlayParticle(string id, Transform parent, Vector3 pos, Quaternion rot)
        {
            if (!TryGetAvailableParticle(id, out ParticleSystem par))
                return null;

            par.transform.parent = parent;
            par.transform.SetLocalPositionAndRotation(pos, rot);
            par.gameObject.SetActive(true);
            par.Play();
            return par;
        }
        public static ParticleSystem PlayParticle(string id, Transform parent) => PlayParticle(id, parent, Vector3.zero, Quaternion.identity);
        public static ParticleSystem PlayParticle(string id, Vector3 pos, Quaternion rot) => PlayParticle(id, null, pos, rot);
        public static ParticleSystem PlayParticle(string id, Vector3 pos) => PlayParticle(id, null, pos, Quaternion.identity);
    }
}
