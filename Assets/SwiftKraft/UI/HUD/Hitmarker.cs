using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class Hitmarker : MonoBehaviour
    {
        public static Hitmarker Instance { get; private set; }

        public GameObject Graphic;

        public float Normalized => Duration.GetPercentage();

        public HitmarkerModule[] Modules { get; private set; }

        public Timer Duration = new();

        public event Action OnShow;
        public event Action OnHide;

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            Duration.OnTimerEnd += OnDurationEnd;
            Modules = GetComponents<HitmarkerModule>();
        }

        private void OnDurationEnd() => Hide();

        protected virtual void Update()
        {
            Duration.Tick(Time.deltaTime);

            for (int i = 0; i < Modules.Length; i++)
                Modules[i].Frame();
        }

        protected virtual void OnDestroy() => Duration.OnTimerEnd -= OnDurationEnd;

        public void Hide()
        {
            OnHide?.Invoke();
            Graphic.SetActive(false);
        }

        public void Show()
        {
            OnShow?.Invoke();
            Graphic.SetActive(true);
            Duration.Reset();
        }
    }

    public abstract class HitmarkerModule : MonoBehaviour
    {
        public abstract void Frame();
    }
}
