using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class FPSViewModelSway : OptionalModifyTransformComponent
    {
        public Vector2 InputAxis => new(Mathf.Clamp(-Input.GetAxisRaw("Mouse Y"), -Maximum, Maximum), Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -Maximum, Maximum));

        public ModifiableStatistic Multiplier = new(1f);
        public ModifiableStatistic TiltMultiplier = new(-2f);
        public float Maximum = 3f;
        public float TiltMaximum = 3f;
        public float SmoothTime = 0.2f;

        Vector3 vel;

        protected virtual void Update()
        {
            Vector3 rotation = InputBlocker.Blocked ? default : ((Vector3)InputAxis + Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -TiltMaximum, TiltMaximum) * TiltMultiplier * Vector3.forward);

            Rotation = Rotation.SmoothDamp(Quaternion.Euler(rotation * Multiplier), ref vel, SmoothTime);
        }
    }
}
