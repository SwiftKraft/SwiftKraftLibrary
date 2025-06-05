using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    [RequireComponent(typeof(ILookable))]
    public class RotateAudioPlayer : MonoBehaviour
    {
        public ILookable Component
        {
            get
            {
                _component ??= GetComponent<ILookable>();
                return _component;
            }
        }
        ILookable _component;

        public bool Rotating => Component.WishLookRotation != Component.CurrentLookRotation;

        public AudioSource Source;

        public MoveTowardsInterpolater VolumeInterp;

        float originalVolume;

        private void Awake()
        {
            originalVolume = Source.volume;
        }

        private void Update()
        {
            if (Rotating)
            {
                if (!Source.isPlaying)
                {
                    Source.Play();
                    Source.loop = true;
                }

                VolumeInterp.MaxValue = originalVolume;
            }
            else
            {
                VolumeInterp.MaxValue = 0f;

                if (VolumeInterp.CurrentValue <= 0f)
                    Source.Stop();
            }

            VolumeInterp.Tick(Time.deltaTime);
            Source.volume = VolumeInterp.CurrentValue;
        }
    }
}
