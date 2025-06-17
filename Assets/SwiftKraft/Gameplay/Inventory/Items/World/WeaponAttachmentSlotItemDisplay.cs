using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using System.Linq;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WeaponAttachmentSlotItemDisplay : MeshSwapper
    {
        public WeaponAttachmentSlotScriptable Scriptable;

        public void SetAttachment(int index)
        {
            if (Scriptable.Attachments.Length <= 0)
                return;

            // temp fix
            //SwapMesh(Scriptable.Attachments[index % Scriptable.Attachments.Length].properties.FirstOrDefault((f) => f is AttachmentSkinnedMeshSwapProperty) is not AttachmentSkinnedMeshSwapProperty prop ? default : prop.Package);
        }
    }
}
