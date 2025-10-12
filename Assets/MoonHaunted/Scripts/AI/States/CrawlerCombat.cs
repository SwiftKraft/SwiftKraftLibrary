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

    bool lunging;
    Vector3 lungeVel;

    public override void End() { }

    public override void Tick()
    {
        if (lunging)
        {
            Vector3 vel = Motor.Component.velocity;
            vel.x = lungeVel.x;
            vel.z = lungeVel.z;
            Motor.Component.velocity = vel;

            if (!Motor.IsGrounded)
                lunging = false;

            return;
        }

        if (!Scanner.HasTarget || !Motor.IsGrounded)
            return;

        Vector3 targetPos = Scanner.Targets[0].Value.position;

        if (!LungeCooldown.Ended)
        {
            LungeCooldown.Tick(Time.fixedDeltaTime);
            if (CurrentPoint == null)
            {
                if (TryGetVacantPointWithinDistance(RetreatFlags, RetreatDistance, out PointOfInterest poi))
                    CurrentPoint = poi;
                else if (Navigator.Stopped)
                    Navigator.Destination = RandomPoint(RetreatDistance);
            }
            else
            {
                Navigator.Destination = CurrentPoint.transform.position;
                if (Navigator.Stopped)
                    LungeCooldown.Tick(LungeCooldown.CurrentValue);
            }
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
        lunging = true;
        dir.y = 0f;
        lungeVel = dir.normalized * LungeSpeed;
        Motor.Jump();
    }
}
