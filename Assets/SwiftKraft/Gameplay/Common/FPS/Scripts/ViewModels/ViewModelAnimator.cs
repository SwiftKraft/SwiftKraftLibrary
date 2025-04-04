using SwiftKraft.Utils;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static SwiftKraft.Gameplay.Common.FPS.ViewModels.ViewModelAnimator.Animation.State;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [RequireComponent(typeof(Animator))]
    public class ViewModelAnimator : RequiredDependencyComponent<Animator>
    {
        public Animation[] Animations;

        public Animator Animator => Component;

        [Header("Sounds")]
        public AudioSource SoundSource;

        protected virtual void Awake()
        {
            foreach (Animation anim in Animations)
                anim.Parent = this;
        }

        public void PlayAnimation(string id)
        {
            Animation anim = Animations.FirstOrDefault((s) => s.ID == id);
            anim?.Play(Animator);
        }

        public void PlaySound(AudioClip clip)
        {
            if (SoundSource == null)
                return;

            SoundSource.PlayOneShot(clip);
        }

        [Serializable]
        public class Animation
        {
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

                PlaySound(cur, anim);
            }

            public void PlaySound(State state, Animator anim)
            {
                AnimatorClipInfo[] infos = anim.GetNextAnimatorClipInfo(0);

                if (infos.Length <= 0)
                    infos = anim.GetCurrentAnimatorClipInfo(0);

                if (infos.Length <= 0)
                    return;

                AnimatorClipInfo info = infos.Aggregate((i1, i2) => i1.weight > i2.weight ? i1 : i2);

                foreach (SoundPair sound in state.Sounds)
                {
                    if (sound.AnimationNames.Contains(info.clip.name))
                    {
                        Parent.PlaySound(sound.Clips.GetRandom());
                        return;
                    }
                }
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
                    public string[] AnimationNames;
                    public AudioClip[] Clips;
                }
            }
        }
    }
}
