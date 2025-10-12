using SwiftKraft.Utils;
using UnityEngine;

[CreateAssetMenu(menuName = "MoonHaunt/Enemy/Crawler/Combat")]
public class CrawlerCombat : CrawlerStateBase
{
    public float AttackDistance = 5f;
    public float LungeSpeed = 15f;
    public Timer LungeCooldown = new(5f);
    public PointFlags RetreatFlags;
    public float RetreatDistance = 60f;

    public override void End() { }

    public override void Tick()
    {
        if (!Scanner.HasTarget || !Motor.IsGrounded)
            return;

        Vector3 targetPos = Scanner.Targets[0].Value.position;

        if (!LungeCooldown.Ended)
        {
            LungeCooldown.Tick(Time.fixedDeltaTime);
            if (CurrentPoint == null && TryGetVacantPointWithinDistance(RetreatFlags, RetreatDistance, out PointOfInterest poi))
                CurrentPoint = poi;
        }
        else
        {
            CurrentPoint = null;
            if (!WithinDistance(targetPos, AttackDistance))
                Navigator.Destination = targetPos;
            else
            {
                Navigator.Destination = Core.transform.position;
                LungeCooldown.Reset();
                Lunge(targetPos - Core.transform.position);
            }
        }
    }

    public void Lunge(Vector3 dir)
    {
        Motor.WishMoveDirection = dir.normalized;
        Motor.Jump();
    }
}
