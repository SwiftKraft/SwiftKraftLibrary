// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using UnityEngine;
    using System.Collections;
    using Logger = Log.Logger;

    internal class SimulatorFramerate
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SimulatorFramerateLimiter.Init();
        }

        public class SimulatorFramerateLimiter : MonoBehaviour
        {
            private Logger logger = Log.Log.GetLogger<SimulatorFramerateLimiter>();

            public static void Init()
            {
                if (!SimulatorUtility.IsSimulator)
                {
                    return;
                }

                var go = new GameObject();
                _ = go.AddComponent<SimulatorFramerateLimiter>();
                go.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(go);
            }

            private static int targetFrameRate = 30;

            private Coroutine loop;
            private bool changed;

            private void Awake()
            {
                logger.Info("Forcing simulator target frame rate to " + targetFrameRate);
                Application.targetFrameRate = targetFrameRate;
            }

            private void OnEnable()
            {
                loop = StartCoroutine(ForceTargetFrameRateLoop());
            }

            private void OnDisable()
            {
                if (loop != null)
                {
                    StopCoroutine(loop);
                    loop = null;
                }
            }

            private IEnumerator ForceTargetFrameRateLoop()
            {
                while (true)
                {
                    yield return null;
                    ForceTargetFrameRate();
                }
            }

            private void ForceTargetFrameRate()
            {
                if (Application.targetFrameRate != targetFrameRate)
                {
                    if (!changed)
                    {
                        changed = true;
                        logger.Warning(Log.Warning.SimulatorFrameRateChanged,
                            $"Detected target frame rate {Application.targetFrameRate}.\n" +
                            $"Simulators target frame rate is forced to {targetFrameRate} every frame.");
                    }

                    Application.targetFrameRate = targetFrameRate;
                }
            }
        }
    }
}
