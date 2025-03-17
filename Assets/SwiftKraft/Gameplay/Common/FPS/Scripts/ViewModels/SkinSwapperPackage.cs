using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Common/FPS/ViewModels/SkinSwapper Package")]
    public class SkinSwapperPackage : ScriptableObject
    {
        public MeshSwapper.Package[] Packages;
    }
}
