using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Common/FPS/Demo/NPCs/Turret")]
    public class NPCTurretState : NPCStateBase
    {
        NPCScannerBase scanner;
        NPCAttackerBase attacker;
        ILookable lookable;

        [SerializeField]
        float maxLookTimer = 4f;
        [SerializeField]
        float minLookTimer = 1f;

        readonly Timer lookTimer = new();

        public override void Begin()
        {
            scanner = Core.Modules.Get<NPCScannerBase>();
            attacker = Core.Modules.Get<NPCAttackerBase>();
            lookable = Core.GetComponent<ILookable>();
            lookTimer.Reset(Random.Range(minLookTimer, maxLookTimer));
        }

        public override void End() { }

        public override void Tick()
        {
            attacker.AllowAttack = scanner.HasTarget;
            if (!scanner.HasTarget)
            {
                lookTimer.Tick(Time.fixedDeltaTime);

                if (lookTimer.Ended)
                {
                    lookTimer.Reset(Random.Range(minLookTimer, maxLookTimer));
                    lookable.WishLookRotation = Quaternion.LookRotation(Random.insideUnitSphere.normalized, Core.transform.up);
                }
            }
        }
    }
}
