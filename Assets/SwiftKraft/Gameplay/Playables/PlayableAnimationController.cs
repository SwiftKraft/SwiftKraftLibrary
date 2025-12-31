using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SwiftKraft.Gameplay.Playables
{
    [RequireComponent(typeof(Animator))]
    public class PlayableAnimationController : MonoBehaviour
    {
        public int AnimatorLayerIndex = 0;
        public List<PlayableAnimationLayer> Layers = new();
        public Animator Animator { get; private set; }

        public AnimatorControllerPlayable AnimatorControllerPlayable { get; private set; }
        public AnimationLayerMixerPlayable Mixer { get; private set; }
        public AnimationPlayableOutput Output { get; private set; }
        public PlayableGraph Graph { get; set; }

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            Graph = PlayableGraph.Create(gameObject.name);
            Output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
            Mixer = AnimationLayerMixerPlayable.Create(Graph, Layers.Count);

            foreach (var layer in Layers)
                InitializeLayer(layer);

            if (Layers.InRange(AnimatorLayerIndex))
                AnimatorControllerPlayable = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);

            Output.SetSourcePlayable(Mixer);

            Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            Graph.Play();
        }

        private void Update()
        {
            for (int i = 0; i < Layers.Count; i++)
                Layers[i]?.Update();

            Graph.Evaluate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Graph.Stop();
            Graph.Destroy();
        }

        public void InitializeLayer(PlayableAnimationLayer layer)
        {
            layer.Initialize(this);
            Mixer.AddInput(layer.Mixer, 0, 1f);
            if (layer.Mask != null)
                Mixer.SetLayerMaskFromAvatarMask((uint)(Layers.Count - 1), layer.Mask);
        }

        public void AddLayer(PlayableAnimationLayer layer)
        {
            if (Layers.Contains(layer))
                return;

            Layers.Add(layer);
            InitializeLayer(layer);
        }

        public void RemoveLayer(PlayableAnimationLayer layer)
        {
            int index = Layers.IndexOf(layer);

            if (index < 0)
                return;

            Mixer.DisconnectInput(index);
            Layers.RemoveAt(index);

            RebuildMixer();
        }

        public void RebuildMixer()
        {
            AnimationLayerMixerPlayable newMixer = AnimationLayerMixerPlayable.Create(Graph, Layers.Count);
            for (int i = 0; i < Layers.Count; i++)
            {
                newMixer.ConnectInput(i, Layers[i].Mixer, 0);
                newMixer.SetInputWeight(i, 1f);
                newMixer.SetLayerMaskFromAvatarMask((uint)i, Layers[i].Mask);
            }
            Mixer = newMixer;
        }

        public void Play(PlayableAnimationState state, int index)
        {
            if (!Layers.InRange(index))
                return;

            Layers[index]?.Play(state);
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

                Graph.Disconnect(Mixer, 0);

                if (currentState == null)
                    return;

                if (!currentState.Initialized)
                    currentState.Initialize(this);

                Graph.Connect(currentState.Mixer, 0, Mixer, 0);
            }
        }
        public PlayableAnimationState NextState // rework to use a list of states for more complex blending
        {
            get => nextState;
            private set
            {
                nextState = value;

                Graph.Disconnect(Mixer, 1);

                if (nextState == null)
                {
                    Mixer.SetInputWeight(0, 1f);
                    Mixer.SetInputWeight(1, 0f);
                    return;
                }

                if (!nextState.Initialized)
                    nextState.Initialize(this);

                Graph.Connect(nextState.Mixer, 0, Mixer, 1);
            }
        }
        private PlayableAnimationState currentState;
        private PlayableAnimationState nextState;

        public PlayableAnimationController Controller { get; private set; }

        public PlayableGraph Graph { get; private set; }

        public AnimationMixerPlayable Mixer { get; private set; }

        public readonly Timer Transition = new(0.1f);

        public void Initialize(PlayableAnimationController graph)
        {
            Controller = graph;
            Graph = graph.Graph;
            Mixer = AnimationMixerPlayable.Create(graph.Graph, 2);
        }

        public void Play(PlayableAnimationState state)
        {
            if (state == null)
                return;

            NextState = state;
        }

        public void Update()
        {
            if (CurrentState != null)
                CurrentState.Update();
            else if (NextState != null)
            {
                CurrentState = NextState;
                NextState = null;
            }

            if (NextState == null)
                return;

            NextState.Update();

            Transition.Tick(Time.deltaTime);

            if (Transition.Ended)
            {
                PlayableAnimationState temp = NextState;
                NextState = null;
                CurrentState = temp;
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
        public List<PlayableAnimation> Animations;
        public Vector2 BlendPosition;
        public bool Initialized { get; private set; }

        public PlayableAnimationLayer Layer { get; private set; }

        public AnimationMixerPlayable Mixer { get; private set; }

        float[] weights;

        public void Initialize(PlayableAnimationLayer graph)
        {
            Layer = graph;
            weights = new float[Animations.Count];
            Mixer = AnimationMixerPlayable.Create(Layer.Graph, Animations.Count);

            for (int i = 0; i < Animations.Count; i++)
            {
                Animations[i].Clip = AnimationClipPlayable.Create(Layer.Graph, Animations[i].OriginalClip);
                Layer.Graph.Connect(Animations[i].Clip, 0, Mixer, i);
                Mixer.SetInputWeight(i, 0f);
            }

            Initialized = true;
        }

        public void Update()
        {
            BlendTreeUtility.Blend2D(Animations, BlendPosition, ref weights);
            for (int i = 0; i < weights.Length; i++)
                Mixer.SetInputWeight(i, weights[i]);
        }
    }

    [Serializable]
    public class PlayableAnimation
    {
        public AnimationClip OriginalClip;
        [NonSerialized]
        public AnimationClipPlayable Clip;
        public Vector2 Position;
    }

    public static class BlendTreeUtility
    {
        public static void Blend2D(
            List<PlayableAnimation> animations,
            Vector2 blendPos,
            ref float[] weights)
        {
            float total = 0f;

            for (int i = 0; i < animations.Count; i++)
            {
                float dist = Vector2.Distance(blendPos, animations[i].Position);
                float w = 1f / Mathf.Max(dist, 0.0001f);
                weights[i] = w;
                total += w;
            }

            if (total <= 0f)
                return;

            for (int i = 0; i < weights.Length; i++)
                weights[i] /= total;
        }
    }
}
