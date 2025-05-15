using SwiftKraft.Saving;
using SwiftKraft.Saving.Progress;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class Currency : MonoBehaviour
    {
        public const string CurrencyID = "Demo.Currency";

        public static Currency Instance { get; private set; }
        public Data CurrentData
        {
            get => ProgressManager.Current == null || _currentData == null && !ProgressManager.Current.TryProgressable(CurrencyID, out _currentData)
                    ? null
                    : _currentData;
        }
        Data _currentData;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        public class Data : Progressable
        {
            public int Money { get; set; }
        }
    }
}
