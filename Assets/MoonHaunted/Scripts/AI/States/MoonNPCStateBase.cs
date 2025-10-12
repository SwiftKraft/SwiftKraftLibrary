using SwiftKraft.Gameplay.NPCs;
using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class MoonNPCStateBase : NPCStateBase
{
    public PointOfInterest CurrentPoint
    {
        get => currentPoint;
        protected set
        {
            if (currentPoint != null)
                currentPoint.TakenBy = null;
            currentPoint = value;
            if (currentPoint != null)
                currentPoint.TakenBy = Core;
        }
    }
    private PointOfInterest currentPoint;

    public bool WithinDistance(Vector3 target, Vector3 pos, float dist) => (target - pos).sqrMagnitude <= dist * dist;
    public bool WithinDistance(Vector3 target, float dist) => WithinDistance(target, Core.transform.position, dist);

    public PointOfInterest GetVacantPoint(PointFlags flag, Func<PointOfInterest, bool> additionalFunction = null) => PointOfInterest.VacantPoints.FirstOrDefault(n => n.Flags.HasFlag(flag) && (additionalFunction == null || additionalFunction.Invoke(n)));

    public bool TryGetVacantPoint(PointFlags flag, out PointOfInterest poi, Func<PointOfInterest, bool> additionalFunction = null)
    {
        poi = GetVacantPoint(flag, additionalFunction);
        return poi != null;
    }

    public PointOfInterest GetVacantPointWithinDistance(PointFlags flag, float dist) => GetVacantPoint(flag, n => WithinDistance(n.transform.position, dist));

    public bool TryGetVacantPointWithinDistance(PointFlags flag, float dist, out PointOfInterest poi)
    {
        poi = GetVacantPointWithinDistance(flag, dist);
        return poi != null;
    }

    public Vector3 RandomPoint(float range)
    {
        Vector2 vec = Random.insideUnitCircle * range;
        return new Vector3(vec.x, 0f, vec.y) + Core.transform.position;
    }
}
