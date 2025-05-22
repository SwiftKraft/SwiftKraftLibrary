using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentParticleChangeProperty : AttachmentComponentPropertyBase<WeaponParticle>
    {
        public string Action;
        public GameObject ParticlePrefab;

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => new AttachmentParticleChangeProperty()
        {
            Action = Action,
            ParticlePrefab = ParticlePrefab,
        };

        public override void Update() => Component.SetOverride(Action, ParticlePrefab);

        public override void Uninstall()
        {
            base.Uninstall();
            Component.SetOverride(Action, null);
        }
    }
}
