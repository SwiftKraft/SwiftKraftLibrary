using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Common/FPS/Demo/NPCs/Basic Attacker")]
    public class NPCBasicAttacker : NPCStateBase
    {
        public bool Attack;
        public Vector3 AttackCoordinates;

        public NPCStateBase ExitState;

        NPCScannerBase scanner;
        NPCNavigator navigator;
        NPCAttackerBase attacker;
        //WeaponAmmo ammo;

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
        readonly Timer reactionTimer = new();

        public override void Begin()
        {
            reactionTimer.Reset(Random.Range(0.2f, 1f));
            scanner = Core.Modules.Get<NPCScannerBase>();
            navigator = Core.Modules.Get<NPCNavigator>();
            attacker = Core.Modules.Get<NPCAttackerBase>();
            lastRemembered = Attack ? AttackCoordinates : Core.transform.position;
        }

        public override void End() { }

        public override void Tick()
        {
            navigator.LookAtWaypoint = !scanner.HasTarget;

            if (scanner.HasTarget)
                reactionTimer.Tick(Time.fixedDeltaTime);
            else
                reactionTimer.Reset();

            attacker.AllowAttack = reactionTimer.Ended;

            if (reactionTimer.Ended && scanner.HasTarget)
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

                if (Attack || scanner.Targets[0].Value != null)
                    lastRemembered = Attack ? AttackCoordinates : scanner.Targets[0].Value.position;
            }
            else if (navigator.Stopped)
                navigator.Destination = NavMesh.SamplePosition(lastRemembered, out NavMeshHit hit, 5f, NavMesh.AllAreas) ? hit.position : lastRemembered;

            if ((lastRemembered - Core.transform.position).sqrMagnitude <= 9f)
            {
                lastRemembered = Attack ? AttackCoordinates : Core.transform.position;
                if (lastRemembered == AttackCoordinates && (lastRemembered - Core.transform.position).sqrMagnitude <= 9f && ExitState != null)
                    Core.CurrentState = ExitState;
            }
        }
    }
}
