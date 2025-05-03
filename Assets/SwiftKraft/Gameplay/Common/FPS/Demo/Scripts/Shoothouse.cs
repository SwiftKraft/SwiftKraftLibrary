using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Gameplay.Map;
using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class Shoothouse : MonoBehaviour
    {
        Target[] targets;
        DoorBase[] doors;

        double timer;

        readonly HashSet<Target> shotTargets = new();
        int shotsFired;
        int shotsHit;

        bool ongoing;

        [SerializeField]
        TMP_Text statsText;

        private void Awake()
        {
            targets = GetComponentsInChildren<Target>();
            doors = GetComponentsInChildren<DoorBase>();

            statsText.SetText("");

            Target.OnTargetDamage += OnTargetHit;
            WeaponBase.OnWeaponSpawn += OnWeaponSpawn;
        }

        private void OnDestroy()
        {
            Target.OnTargetDamage -= OnTargetHit;
            WeaponBase.OnWeaponSpawn -= OnWeaponSpawn;
        }

        private void FixedUpdate()
        {
            if (ongoing)
                timer += Time.fixedDeltaTime;
        }

        private void OnWeaponSpawn(WeaponBase weapon, GameObject go)
        {
            if (ongoing)
                shotsFired++;
        }

        private void OnTargetHit(Target t, DamageDataBase dmg)
        {
            if (!ongoing)
                return;

            shotTargets.Add(t);
            shotsHit++;
        }

        public void StartShoothouse() => ongoing = true;

        public void ResetShoothouse()
        {
            ongoing = false;

            EndStats();

            shotTargets.Clear();

            timer = 0f;
            shotsFired = 0;
            shotsHit = 0;

            foreach (Target t in targets)
                if (t.TryGetComponent(out Animator anim))
                {
                    anim.SetBoolSafe("PeekLeft", false);
                    anim.SetBoolSafe("PeekRight", false);
                }

            foreach (DoorBase d in doors)
                d.SetState(false);
        }

        public void EndStats()
        {
            TimeSpan time = TimeSpan.FromSeconds(timer);
            statsText.SetText($"<color=#FFFF00>TARGETS</color>\n<size=85>{shotTargets.Count}/{targets.Length}</size>\n\n<color=#FFFF00>TIME</color>\n<size=85>{time.TotalSeconds.ToString("0:00.000", System.Globalization.CultureInfo.InvariantCulture)}</size>\n\n<color=#FFFF00>ACCURACY</color>\n<size=85>{shotsHit}/{shotsFired} ({shotsHit * 100f / shotsFired}%)</size>");
        }
    }
}
