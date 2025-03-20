namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttachments : WeaponComponent
    {
        public WeaponAttachmentSlot[] Slots { get; private set; }

        protected virtual void Awake()
        {
            Slots = GetComponentsInChildren<WeaponAttachmentSlot>();
        }
    }
}
