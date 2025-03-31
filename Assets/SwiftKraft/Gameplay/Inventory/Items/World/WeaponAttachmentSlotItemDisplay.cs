using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WeaponAttachmentSlotItemDisplay : MeshSwapper
    {
        public WeaponAttachmentSlotScriptable Scriptable;

        public void SetAttachment(int index)
        {
            if (Scriptable.Attachments.Length <= 0)
                return;

            SwapMesh(Scriptable.Attachments[index % Scriptable.Attachments.Length].package);
        }
    }
}
