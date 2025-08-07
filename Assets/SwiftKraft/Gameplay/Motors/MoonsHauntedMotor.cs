namespace SwiftKraft.Gameplay.Motors
{
    public class MoonsHauntedMotor : CharacterControllerMotor
    {
        public float SprintSpeed = 7f;

        public bool WishSprint { get; set; }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            State += WishSprint ? 1 : 0;
        }
    }
}
