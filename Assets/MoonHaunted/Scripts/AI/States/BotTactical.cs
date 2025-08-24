using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Gameplay.NPCs;
using UnityEngine;

[CreateAssetMenu(menuName = "MoonHaunt/AI/Tactical")]
public class BotTactical : BotStateBase
{
    public float AreaRadius = 15f;
    public float FollowRadius = 3f;

    public override void End() { }

    public override void Tick()
    {
        if (Player == null)
            return;

        if (CurrentPoint != null)
            Navigator.Destination = CurrentPoint.transform.position;

        if (WithinDistance(AreaRadius))
        {
            if (CurrentPoint != null)
                return;

            if (TryGetVacantPointWithinDistance(PointFlags.Defensive, AreaRadius, out PointOfInterest poi))
            {
                CurrentPoint = poi;
                SetSprintState(true);
            }
            else
            {
                bool outOfRadius = !WithinDistance(FollowRadius);
                CurrentPoint = null;

                SetSprintState(outOfRadius && Player.Controller.Motor.WishSprint);
                Navigator.Destination = outOfRadius ? Player.transform.position : Core.transform.position;
            }
        }
        else
        {
            CurrentPoint = null;
            if (Navigator.Stopped)
                Navigator.Destination = Player.transform.position;

            SetSprintState(true);
        }
    }

    public override void Update() { }
}

// AI will pathfind to player if out of range.
// When the AI is in range, they will pick a spot to camp.
// The spot can be a random walkable coordinate within range.
// Or a point of interest within range.
// The point of interest will provide the bot with a playstyle, or an angle to hold.
