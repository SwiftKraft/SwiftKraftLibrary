using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SwiftKraft.Gameplay.Playables
{
    public class PlayableAnimationController : MonoBehaviour
    {
        public readonly List<PlayableAnimationLayer> Layers = new();
    }

    public class PlayableAnimationLayer
    {
        public PlayableAnimationState CurrentState { get; set; }
        public PlayableAnimationState NextState { get; set; }

        public void Update(float deltaTime)
        {

        }
    }

    [Serializable]
    public class PlayableAnimationState
    {
        public PlayableAnimation[] Animations;
        public AvatarMask Mask;

        public AnimationMixerPlayable Mixer { get; private set; }

        public void Initialize(PlayableGraph graph)
        {
            Mixer = AnimationMixerPlayable.Create(graph, Animations.Length);

            for (int i = 0; i < Animations.Length; i++)
            {
                Animations[i].Clip = AnimationClipPlayable.Create(graph, Animations[i].OriginalClip);
                graph.Connect(Animations[i].Clip, 0, Mixer, i);
                Mixer.SetInputWeight(i, 1f);
            }
        }

        public void Update(float deltaTime)
        {
            
        }
    }

    [Serializable]
    public class PlayableAnimation
    {
        public AnimationClip OriginalClip;
        public AnimationClipPlayable Clip;
        public float[] Position;
    }

    public static class BlendTreeUtility
    {
        public static float[] BlendWeights(PlayableAnimation[] nodes, float[] input)
        {
            int count = nodes.Length;
            float[] w = new float[count];
            float total = 0f;

            for (int i = 0; i < count; i++)
            {
                float dist = Distance(nodes[i].Position, input);

                if (dist < 0.0001f)
                {
                    w[i] = 1f;
                    return w;
                }

                float inv = 1f / dist;
                w[i] = inv;
                total += inv;
            }

            for (int i = 0; i < count; i++)
                w[i] /= total;

            return w;
        }

        private static float Distance(float[] a, float[] b)
        {
            float sum = 0f;
            int dims = Mathf.Min(a.Length, b.Length);

            for (int i = 0; i < dims; i++)
            {
                float d = a[i] - b[i];
                sum += d * d;
            }

            return Mathf.Sqrt(sum);
        }
    }
}
