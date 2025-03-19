using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class FPSViewModelSway : OptionalModifyTransformComponent
    {
        public Vector2 InputAxis => new(Mathf.Clamp(-Input.GetAxisRaw("Mouse Y"), -Maximum, Maximum), Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -Maximum, Maximum));

        public float Multiplier = 2f;
        public float Maximum = 3f;
        public float SmoothTime = 0.2f;

        Vector3 vel;

        protected virtual void Update()
        {
            Rotation = Rotation.SmoothDamp(Quaternion.Euler(InputAxis * Multiplier), ref vel, SmoothTime);
        }
    }
}
