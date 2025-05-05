using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors.Miscellaneous
{
    [RequireComponent(typeof(MotorBase))]
    public class Footsteps : MonoBehaviour
    {
        public MotorBase MotorBase { get; private set; }

        public AudioSource Source;

        public float Rate = 1f;
        public Timer RateLimit = new(0.2f);

        public Transform RayPoint;
        public float RayDistance = 0.25f;
        public LayerMask RayMask;

        public List<int> BannedStates;


        public FootstepCollection Profiles;
        public FootstepProfile[] StepProfiles => Profiles == null ? empty : Profiles.Profiles;

        readonly FootstepProfile[] empty = new FootstepProfile[0];

        float factor;
        float prevFactor;

        bool prevGrounded;

        private void Awake()
        {
            MotorBase = GetComponent<MotorBase>();
            factor = MotorBase.MoveFactor % (1f / Rate);
            prevFactor = factor;
            if (MotorBase is IGroundable groundable)
                prevGrounded = groundable.IsGrounded;

            if (Source == null)
                enabled = false;
        }

        private void FixedUpdate()
        {
            if (BannedStates.Contains(MotorBase.State))
                return;

            RateLimit.Tick(Time.fixedDeltaTime);

            factor = MotorBase.MoveFactor % (1f / Rate);

            if (factor < prevFactor)
                Trigger();

            prevFactor = factor;

            if (MotorBase is IGroundable groundable)
            {
                if (groundable.IsGrounded && !prevGrounded)
                    Trigger();

                prevGrounded = groundable.IsGrounded;
            }
        }

        public void Trigger()
        {
            if (!RateLimit.Ended)
                return;

            if (Physics.Raycast(RayPoint.position, -RayPoint.up, out RaycastHit _hit, RayDistance, RayMask, QueryTriggerInteraction.Ignore))
            {
                Material mat = FindFaceMaterial(_hit);

                if (mat == null)
                    return;

                FootstepProfile profile = GetProfile(mat);
                if (profile == null)
                {
                    if (StepProfiles.Length > 0)
                    {
                        Source.PlayOneShot(StepProfiles[0].GetClip(MotorBase.State));
                        RateLimit.Reset();
                    }
                    return;
                }

                Source.PlayOneShot(profile.GetClip(MotorBase.State));
                RateLimit.Reset();
            }
        }

        public FootstepProfile GetProfile(Material mat)
        {
            foreach (FootstepProfile prof in StepProfiles)
                if (prof.ValidMaterial(mat))
                    return prof;
            return null;
        }

        public static Material FindFaceMaterial(RaycastHit hit)
        {
            if (hit.collider is MeshCollider collider)
                return FindFaceMaterial(collider, hit.triangleIndex);
            else if (hit.collider.TryGetComponentInChildren(out Renderer rend2))
                return rend2.material;
            else
                return null;
        }

        private static Material FindFaceMaterial(MeshCollider collider, int triangleIndex)
        {
            Mesh mesh = collider.sharedMesh;

            int limit = triangleIndex * 3;
            int submesh;

            if (mesh.isReadable)
            {
                for (submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    int numIndices = mesh.GetTriangles(submesh).Length;
                    if (numIndices > limit)
                        break;

                    limit -= numIndices;
                }

                return collider.GetComponentInChildren<MeshRenderer>().sharedMaterials[submesh];
            }

            return collider.GetComponentInChildren<MeshRenderer>().material;
        }

        [Serializable]
        public class FootstepProfile
        {
            public string[] MaterialKeywords;
            public State[] States;

            public AudioClip GetClip(int state)
            {
                State st = States.FirstOrDefault(s => s.MotorState == state);
                st ??= States.FirstOrDefault();
                return st?.GetClip();
            }

            public bool ValidMaterial(Material mat)
            {
                if (MaterialKeywords.Length <= 0)
                    return true;

                foreach (string s in MaterialKeywords)
                    if (mat.name.ToLower().Contains(s.ToLower()))
                        return true;

                return false;
            }

            [Serializable]
            public class State
            {
                public int MotorState = 1;
                public AudioClip[] Clips;

                public AudioClip GetClip() => Clips.Length > 0 ? Clips.GetRandom() : null;
            }
        }
    }
}
