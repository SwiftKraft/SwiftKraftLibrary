using SwiftKraft.Gameplay.NPCs;
using UnityEngine;

[CreateAssetMenu(menuName = "MoonHaunt/AI/Tactical")]
public class BotTactical : BotStateBase
{
    public float AreaRadius = 15f;
    public float FollowRadius = 3f;

    private NPCNavigator navigator;

    public override void Begin()
    {
        navigator = Core.GetComponent<NPCNavigator>();
    }

    public override void End() { }

    public override void Tick()
    {
        if (Player == null)
            return;

        if (CurrentPoint != null)
            navigator.Destination = CurrentPoint.transform.position;

        if (WithinDistance(AreaRadius))
        {
            if (CurrentPoint != null)
                return;

            if (TryGetVacantPointWithinDistance(PointFlags.Defensive, AreaRadius, out PointOfInterest poi))
                CurrentPoint = poi;
            else
            {
                CurrentPoint = null;
                if (!WithinDistance(FollowRadius))
                    navigator.Destination = Player.transform.position;
                else
                    navigator.Destination = Core.transform.position;
            }
        }
        else
        {
            CurrentPoint = null;
            if (navigator.Stopped)
                navigator.Destination = Player.transform.position;
        }
    }

    public override void Update() { }
}

// AI will pathfind to player if out of range.
// When the AI is in range, they will pick a spot to camp.
// The spot can be a random walkable coordinate within range.
// Or a point of interest within range.
// The point of interest will provide the bot with a playstyle, or an angle to hold.
