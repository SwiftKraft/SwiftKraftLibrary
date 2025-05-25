using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class Crosshair : MonoBehaviour
    {
        public static Crosshair Instance { get; private set; }

        public RectTransform Transform { get; private set; }

        public Canvas Canvas { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            Transform = (RectTransform)transform;
            Canvas = GetComponentInParent<Canvas>();
        }

        public virtual void SetSize(float size) => Transform.sizeDelta = Vector2.one * size;

        public virtual void SetDegrees(float degrees)
        {
            if (Camera.main == null || Canvas == null)
                return;

            float ratio = Camera.main.pixelWidth / Camera.main.fieldOfView;
            float pixel = ratio * degrees / Canvas.scaleFactor;
            SetSize(pixel);
        }
    }
}
