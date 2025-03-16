using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Common/FPS/ViewModels/HandSwapper Package")]
    public class HandSwapperPackage : ScriptableObject
    {
        public MeshSwapper.Package[] Packages;
    }
}
