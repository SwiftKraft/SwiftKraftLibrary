using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Motors
{
    public class AdvancedCharacterControllerMotor : CharacterControllerMotor
    {
        public float SprintSpeed = 7f;
        public SmoothDampInterpolater CrouchInterpolater;
    }
}