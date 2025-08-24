using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Gameplay.NPCs;
using System;
using System.Linq;
using UnityEngine;

public abstract class BotStateBase : NPCStateBase
{
    public BodySwapper Player => BodySwapper.PlayerInstance;

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

    public NPCNavigator Navigator { get; private set; }
    public MoonsHauntedMotor Motor { get; private set; }

    public float PlayerDistance => Player != null ? (Player.transform.position - Core.transform.position).magnitude : Mathf.Infinity;

    public override void Begin()
    {
        Navigator = Core.GetComponent<NPCNavigator>();
        Motor = Navigator.Motor as MoonsHauntedMotor;
    }

    public bool WithinDistance(Vector3 pos, float dist) => Player != null && (Player.transform.position - pos).sqrMagnitude <= dist * dist;
    public bool WithinDistance(float dist) => WithinDistance(Core.transform.position, dist);

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

    public void SetSprintState(bool state)
    {
        if (Motor != null)
            Motor.WishSprint = state;
    }

    public override void End() => CurrentPoint = null;
}
