using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class Hitmarker : MonoBehaviour
    {
        public static Hitmarker Instance { get; private set; }

        public GameObject Graphic;

        public Profile[] Profiles;

        public float Normalized => 1f - Duration.GetPercentage();

        public Profile Current { get; private set; }

        public int CurrentType
        {
            get => _currentType;
            private set
            {
                _currentType = value;
                Current = GetProfile(_currentType);
            }
        }
        private int _currentType;

        public Timer Duration = new();

        public event Action<int> OnShow;
        public event Action OnHide;

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            if (Graphic == null)
            {
                enabled = false;
                return;
            }

            Duration.OnTimerEnd += OnDurationEnd;
        }

        protected virtual void Start()
        {
            Graphic.SetActive(false);

            foreach (Profile prof in Profiles)
                prof?.Init();
        }

        protected virtual void Update()
        {
            Duration.Tick(Time.deltaTime);
            Current?.Tick();
        }

        protected virtual void OnDestroy() => Duration.OnTimerEnd -= OnDurationEnd;

        public Profile GetProfile(int type) => Profiles.FirstOrDefault(p => p.Type == type);

        private void OnDurationEnd() => Hide();

        public void Hide()
        {
            OnHide?.Invoke();
            Graphic.SetActive(false);
        }

        public void Show(int type = 0)
        {
            CurrentType = type;
            OnShow?.Invoke(type);
            Current?.Trigger();
            Graphic.SetActive(true);
            Duration.Reset();
        }

        public static void Trigger(int type = 0)
        {
            if (Instance != null)
                Instance.Show(type);
        }

        [Serializable]
        public class Profile
        {
            public int Type;
            [SerializeReference, Subclass]
            public HitmarkerModule[] Modules;

            public void Init()
            {
                for (int i = 0; i < Modules.Length; i++)
                    Modules[i].Init();
            }

            public void Trigger()
            {
                for (int i = 0; i < Modules.Length; i++)
                    Modules[i].Trigger();
            }

            public void Tick()
            {
                for (int i = 0; i < Modules.Length; i++)
                    Modules[i].Frame();
            }
        }
    }

    [Serializable]
    public abstract class HitmarkerModule
    {
        public virtual void Init() { }

        public virtual void Trigger() { }

        public abstract void Frame();
    }
}
