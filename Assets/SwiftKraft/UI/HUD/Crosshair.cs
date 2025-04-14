using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class Crosshair : MonoBehaviour
    {
        public static Crosshair Instance;

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

        public void SetSize(float size) => Transform.sizeDelta = Vector2.one * size;

        public void SetDegrees(float degrees)
        {
            if (Camera.main == null || Canvas == null)
                return;

            float ratio = Camera.main.pixelWidth / Camera.main.fieldOfView;
            float pixel = ratio * degrees / Canvas.scaleFactor;
            SetSize(pixel);
        }
    }
}
