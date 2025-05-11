using UnityEngine;
using static SwiftKraft.Gameplay.Motors.Miscellaneous.Footsteps;

namespace SwiftKraft.Gameplay.Motors.Miscellaneous 
{ 
    [CreateAssetMenu(fileName = "FootstepCollection", menuName = "SwiftKraft/Gameplay/Motors/Miscellaneous/Footstep Collection")]
    public class FootstepCollection : ScriptableObject
    {
        public FootstepProfile[] Profiles;
    }
}
