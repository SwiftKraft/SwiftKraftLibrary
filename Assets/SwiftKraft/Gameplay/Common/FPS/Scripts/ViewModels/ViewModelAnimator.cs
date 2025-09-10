using SwiftKraft.Utils;
using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static SwiftKraft.Gameplay.Weapons.ViewModelAnimator.Animation.State;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(Animator))]
    public class ViewModelAnimator : RequiredDependencyComponent<Animator>
    {
        public string CustomClip = "Custom";
        public string CustomState = "Custom";

        public Animation[] Animations;

        public Animator Animator => Component;

        public AnimatorOverrideController OverrideController { get; private set; }

        [Header("Sounds")]
        public AudioSource SoundSource;

        protected virtual void Awake()
        {
            foreach (Animation anim in Animations)
                anim.Parent = this;

            OverrideController = new(Animator.runtimeAnimatorController);
            Animator.runtimeAnimatorController = OverrideController;
        }

        public void PlayAnimation(string id)
        {
            Animation anim = Animations.FirstOrDefault((s) => s.ID == id);
            anim?.Play(Animator);
        }

        public void PlaySound(AudioClip clip, float startTime = 0f)
        {
            if (SoundSource == null)
                return;

            SoundSource.clip = clip;
            SoundSource.pitch = Animator.speed * (Animator.IsInTransition(0) ? Animator.GetNextAnimatorStateInfo(0) : Animator.GetCurrentAnimatorStateInfo(0)).speedMultiplier * Time.timeScale;
            SoundSource.Play();
            if (startTime > 0f)
                SoundSource.time = startTime;
        }

        public void ResetAnimation(AnimationClip clip) => ResetAnimation(clip.name);

        public void SwapAnimation(AnimationClip clip, AnimationClip overrider) => SwapAnimation(clip.name, overrider);

        public void ResetAnimation(string clipName)
        {
            if (OverrideController.animationClips.Any((c) => c.name.Equals(clipName)))
                OverrideController[clipName] = null;
        }

        public void SwapAnimation(string clipName, AnimationClip overrider)
        {
            if (OverrideController.animationClips.Any((c) => c.name.Equals(clipName)))
                OverrideController[clipName] = overrider;
        }

        public void PlayCustom(AnimationClip clip, float transitionTime = 0.1f, AudioClip sound = null)
        {
            if (clip == null)
                return;

            SwapAnimation(CustomClip, clip);

            if (transitionTime > 0f)
                Animator.CrossFadeInFixedTime(CustomState, transitionTime);
            else
                Animator.Play(CustomState, 0, 0f);

            if (sound != null)
                PlaySound(sound);
        }

        [Serializable]
        public class Animation
        {
            public const int Tries = 10;

            public ViewModelAnimator Parent { get; set; }

            public string ID;
            public State[] States;

            public void Play(Animator anim)
            {
                State cur = States.GetWeightedRandom();
                cur.OnPlay?.Invoke();

                if (cur.CrossFade <= 0f)
                    anim.Play(cur.StateName, 0, 0f);
                else
                    anim.CrossFadeInFixedTime(cur.StateName, cur.CrossFade, 0, 0f);

                anim.Update(0f);

                if (Parent.isActiveAndEnabled)
                    Parent.StartCoroutine(sound());

                IEnumerator sound()
                {
                    float norm = 0f;
                    for (int i = 0; i < Tries; i++)
                    {
                        if (PlaySound(cur, anim, norm))
                            break;

                        norm += Time.deltaTime;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }

            public bool PlaySound(State state, Animator anim, float startTime = 0f)
            {
                AnimatorClipInfo[] infos = anim.IsInTransition(0) ? anim.GetNextAnimatorClipInfo(0) : anim.GetCurrentAnimatorClipInfo(0);

                if (infos.Length <= 0)
                    infos = anim.IsInTransition(0) ? anim.GetCurrentAnimatorClipInfo(0) : anim.GetNextAnimatorClipInfo(0);

                if (infos.Length <= 0)
                    return false;

                AnimatorClipInfo info = infos.Aggregate((i1, i2) => i1.weight >= i2.weight ? i1 : i2);

                foreach (SoundPair sound in state.Sounds)
                {
                    if (sound.Animations.Contains(info.clip))
                    {
                        Parent.PlaySound(sound.Clips.GetRandom(), startTime);
                        return true;
                    }
                }

                return false;
            }

            [Serializable]
            public class State : IWeight
            {
                public string StateName;
                public float CrossFade;
                public int Weight;
                public UnityEvent OnPlay;
                public SoundPair[] Sounds;

                int IWeight.Weight => Weight;

                [Serializable]
                public struct SoundPair
                {
                    public AnimationClip[] Animations;
                    public AudioClip[] Clips;
                }
            }
        }
    }
}
