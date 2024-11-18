using SwiftKraft.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [RequireComponent(typeof(Animator))]
    public class ViewModelAnimator : RequiredDependencyComponent<Animator>
    {
        public Animation[] Animations;

        public Animator Animator => Component;

        public void PlayAnimation(string id)
        {
            Animation anim = Animations.FirstOrDefault((s) => s.ID == id);
            anim?.Play(Animator);
        }

        [Serializable]
        public class Animation
        {
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
            }

            [Serializable]
            public class State : IWeight
            {
                public string StateName;
                public float CrossFade;
                public int Weight;
                public UnityEvent OnPlay;

                int IWeight.Weight => Weight;
            }
        }
    }
}
