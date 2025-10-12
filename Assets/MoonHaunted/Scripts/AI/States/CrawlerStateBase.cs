using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Gameplay.NPCs;

public abstract class CrawlerStateBase : MoonNPCStateBase
{
    public NPCNavigator Navigator { get; protected set; }
    public NPCScannerBase Scanner { get; protected set; }
    public NPCAttackerBase Attacker { get; protected set; }
    public NPCRigidbodyMotor Motor { get; protected set; }

    public override void Begin()
    {
        Navigator = Core.GetComponent<NPCNavigator>();
        Scanner = Core.GetComponent<NPCScannerBase>();
        Attacker = Core.GetComponent<NPCAttackerBase>();

        Motor = Navigator.Motor as NPCRigidbodyMotor;
    }
}
