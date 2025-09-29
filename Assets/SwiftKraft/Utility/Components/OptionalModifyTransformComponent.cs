using UnityEngine;

namespace SwiftKraft.Utils
{
    public abstract class OptionalModifyTransformComponent : MonoBehaviour
    {
        public Transform ModifyTarget;

        protected MultiModifyTransform MultiModify;
        protected MultiModifyTransform.Modifier Modifier;

        public Vector3 Position
        {
            get => Modifier != null ? Modifier.Position : ModifyTarget.localPosition;
            protected set
            {
                if (Modifier != null)
                    Modifier.Position = value;
                else
                    ModifyTarget.localPosition = value;
            }
        }

        public Quaternion Rotation
        {
            get => Modifier != null ? Modifier.Rotation : ModifyTarget.localRotation;
            protected set
            {
                if (Modifier != null)
                    Modifier.Rotation = value;
                else 
                    ModifyTarget.localRotation = value;
            }
        }

        protected virtual void Awake()
        {
            if (ModifyTarget.TryGetComponent(out MultiModify))
                Modifier = MultiModify.AddModifier();
        }
    }
}
