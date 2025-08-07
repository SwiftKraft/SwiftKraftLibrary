namespace SwiftKraft.Gameplay.Motors
{
    public class MoonsHauntedController : CharacterControllerMotor
    {
        public float SprintSpeed = 7f;

        public bool WishSprint { get; set; }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

        }
    }
}
