using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class FPSViewModelSway : OptionalModifyTransformComponent
    {
        public Vector2 InputAxis => new(Mathf.Clamp(Input.GetAxisRaw("Mouse Y") * VerticalMultiplier, -VerticalMaximum, VerticalMaximum), Mathf.Clamp(Input.GetAxisRaw("Mouse X") * HorizontalMultiplier, -HorizontalMaximum, HorizontalMaximum));

        public ModifiableStatistic HorizontalMultiplier = new(1f);
        public ModifiableStatistic VerticalMultiplier = new(1f);
        public ModifiableStatistic TiltMultiplier = new(-2f);
        public float HorizontalMaximum = 3f;
        public float VerticalMaximum = 3f;
        public float TiltMaximum = 3f;
        public float SmoothTime = 0.2f;

        Vector3 vel;

        protected virtual void Update()
        {
            Vector3 rotation = InputBlocker.Blocked ? default : ((Vector3)InputAxis + Mathf.Clamp(Input.GetAxisRaw("Mouse X") * TiltMultiplier, -TiltMaximum, TiltMaximum) * Vector3.forward);

            Rotation = Rotation.SmoothDamp(Quaternion.Euler(rotation), ref vel, SmoothTime);
        }
    }
}
