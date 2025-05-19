using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class HitmarkerAudio : HitmarkerModule
    {
        public AudioSource Source;
        public AudioClip[] Clips;

        int lastRand;

        public override void Frame() { }

        public override void Trigger()
        {
            base.Trigger();
            Source.Stop();
            Source.PlayOneShot(Clips.GetRandom(ref lastRand));
        }
    }
}
