using SwiftKraft.Utils;
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

        public AnimationLayerMixerPlayable Mixer { get; private set; }

        public PlayableGraph Graph { get; set; }

        private void Awake()
        {
            Graph = PlayableGraph.Create(gameObject.name);
            Mixer = AnimationLayerMixerPlayable.Create(Graph, Layers.Count); // make addlayer methods, and apply avatarmask and weights.
            Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            Graph.Play();
        }

        private void Update()
        {
            for (int i = 0; i < Layers.Count; i++)
                Layers[i].Update();

            Graph.Evaluate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Graph.Stop();
            Graph.Destroy();
        }
    }

    [Serializable]
    public class PlayableAnimationLayer
    {
        public AvatarMask Mask;

        public PlayableAnimationState CurrentState
        {
            get => currentState;
            private set
            {
                currentState = value;

                if (currentState == null)
                {
                    Mixer.DisconnectInput(0);
                    return;
                }

                Mixer.ConnectInput(0, currentState.Mixer, 0);

                if (!currentState.Initialized)
                    currentState.Initialize(Graph);
            }
        }
        public PlayableAnimationState NextState
        {
            get => nextState;
            set
            {
                nextState = value;

                if (nextState == null)
                {
                    Mixer.DisconnectInput(1);
                    Mixer.SetInputWeight(0, 1f);
                    Mixer.SetInputWeight(1, 0f);
                    return;
                }

                Mixer.ConnectInput(1, nextState.Mixer, 0);

                if (!nextState.Initialized)
                    nextState.Initialize(Graph);
            }
        }
        private PlayableAnimationState currentState;
        private PlayableAnimationState nextState;

        public PlayableGraph Graph { get; private set; }

        public AnimationMixerPlayable Mixer { get; private set; }

        public readonly Timer Transition = new(0.1f);

        public void Initialize(PlayableGraph graph)
        {
            Graph = graph;
            Mixer = AnimationMixerPlayable.Create(graph, 2);
        }

        public void Update()
        {
            if (CurrentState != null)
                CurrentState.Update();
            else if (NextState != null)
                CurrentState = NextState;

            if (NextState == null)
                return;

            NextState.Update();

            Transition.Tick(Time.deltaTime);

            if (Transition.Ended)
            {
                CurrentState = NextState;
                NextState = null;
            }
            else
            {
                float percentage = Transition.GetPercentage();
                Mixer.SetInputWeight(0, percentage);
                Mixer.SetInputWeight(1, 1f - percentage);
            }
        }
    }

    [Serializable]
    public class PlayableAnimationState
    {
        public PlayableAnimation[] Animations;
        public float[] BlendFloat;
        public bool Initialized { get; private set; }

        public AnimationMixerPlayable Mixer { get; private set; }

        float[] weights;

        public void Initialize(PlayableGraph graph)
        {
            weights = new float[Animations.Length];
            Mixer = AnimationMixerPlayable.Create(graph, Animations.Length);

            for (int i = 0; i < Animations.Length; i++)
            {
                Animations[i].Clip = AnimationClipPlayable.Create(graph, Animations[i].OriginalClip);
                Mixer.ConnectInput(i, Animations[i].Clip, 0);
                Mixer.SetInputWeight(i, 0f);
            }

            Initialized = true;
        }

        public void Update()
        {
            BlendTreeUtility.BlendWeights(Animations, BlendFloat, ref weights);
            for (int i = 0; i < weights.Length; i++)
                Mixer.SetInputWeight(i, weights[i]);
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
        public static void BlendWeights(PlayableAnimation[] nodes, float[] input, ref float[] w)
        {
            int count = nodes.Length;

            if (w == null || w.Length != count)
                w = new float[count];

            // Fallback if input is missing
            if (input == null || input.Length == 0)
            {
                float even = 1f / count;
                for (int i = 0; i < count; i++)
                    w[i] = even;
                return;
            }

            float total = 0f;

            for (int i = 0; i < count; i++)
            {
                float distSqr = DistanceSqr(nodes[i].Position, input);

                if (distSqr < 0.000001f)
                {
                    for (int j = 0; j < count; j++)
                        w[j] = (i == j ? 1f : 0f);
                    return;
                }

                float inv = 1f / distSqr;
                w[i] = inv;
                total += inv;
            }

            float invTotal = 1f / total;
            for (int i = 0; i < count; i++)
                w[i] *= invTotal;
        }

        private static float DistanceSqr(float[] a, float[] b)
        {
            int dims = Mathf.Min(a.Length, b.Length);
            float sum = 0f;

            for (int i = 0; i < dims; i++)
            {
                float d = a[i] - b[i];
                sum += d * d;
            }

            // Handle extra dimensions
            if (b.Length > a.Length)
            {
                for (int i = dims; i < b.Length; i++)
                    sum += b[i] * b[i];
            }
            else if (a.Length > b.Length)
            {
                for (int i = dims; i < a.Length; i++)
                    sum += a[i] * a[i];
            }

            return sum;
        }
    }
}
