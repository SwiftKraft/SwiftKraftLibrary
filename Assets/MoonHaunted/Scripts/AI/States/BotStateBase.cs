using SwiftKraft.Gameplay.Common.FPS;
using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Gameplay.NPCs;
using UnityEngine;

public abstract class BotStateBase : MoonNPCStateBase
{
    public BodySwapper Player => BodySwapper.PlayerInstance;

    public NPCNavigator Navigator { get; private set; }
    public NPCScannerBase Scanner { get; private set; }
    public SimpleFPSInventory Inventory { get; private set; }
    public MoonsHauntedMotor Motor { get; private set; }

    public float PlayerDistance => Player != null ? (Player.transform.position - Core.transform.position).magnitude : Mathf.Infinity;

    public override void Begin()
    {
        Navigator = Core.GetComponent<NPCNavigator>();
        Scanner = Core.GetComponent<NPCScannerBase>();
        Inventory = Core.GetComponent<SimpleFPSInventory>();
        Motor = Navigator.Motor as MoonsHauntedMotor;
    }

    public bool WithinDistance(float dist) => WithinDistance(Player.transform.position, dist);

    public void SetSprintState(bool state)
    {
        if (Motor != null)
            Motor.WishSprint = state;
    }

    public override void End() => CurrentPoint = null;
}
