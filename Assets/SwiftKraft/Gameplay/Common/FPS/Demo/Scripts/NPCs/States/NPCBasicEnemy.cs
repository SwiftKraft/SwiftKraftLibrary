using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Common/FPS/Demo/NPCs/Basic Enemy")]
    public class NPCBasicEnemy : NPCStateBase
    {
        NPCScannerBase scanner;
        NPCNavigator navigator;
        WeaponAmmo ammo;

        Vector3 lastRemembered;

        [SerializeField]
        float minRange = 3f;
        [SerializeField]
        float maxRange = 7f;

        [SerializeField]
        float minStrafeTimer = 3f;
        [SerializeField]
        float maxStrafeTimer = 6f;

        readonly Timer strafeTimer = new();

        public override void Begin()
        {
            scanner = Core.Modules.Get<NPCScannerBase>();
            navigator = Core.Modules.Get<NPCNavigator>();
            ammo = Core.Modules.Get<NPCAttackerWeapon>().Weapon.GetComponent<WeaponAmmo>();
        }

        public override void End() { }

        public override void Tick()
        {
            if (!ammo.Reloading && ammo.CurrentAmmo <= 0)
                ammo.StartReload();

            if (scanner.HasTarget)
            {
                strafeTimer.Tick(Time.fixedDeltaTime);

                if (strafeTimer.Ended)
                {
                    strafeTimer.Reset(Random.Range(minStrafeTimer, maxStrafeTimer));
                    Vector2 randDir = Random.insideUnitCircle;
                    Vector2 rand = randDir * (maxRange - minRange) + randDir * minRange;
                    Vector3 pos = Core.transform.position + new Vector3(rand.x, 0f, rand.y);
                    navigator.Destination = NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas) ? hit.position : pos;
                }

                lastRemembered = scanner.Targets[0].Value.position;
            }
            else if (navigator.Stopped)
                navigator.Destination = NavMesh.SamplePosition(lastRemembered, out NavMeshHit hit, 5f, NavMesh.AllAreas) ? hit.position : lastRemembered;
        }

        public override void Update() { }
    }
}
