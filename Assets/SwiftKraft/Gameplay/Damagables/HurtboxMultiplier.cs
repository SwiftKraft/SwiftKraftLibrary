namespace SwiftKraft.Gameplay.Damagables
{
    public class HurtboxMultiplier : Hurtbox
    {
        public float Multiplier = 2f;

        public override void Damage(DamageDataBase data)
        {
            DamageDataBase newDmg = data.Clone();
            newDmg.Damage *= Multiplier;
            base.Damage(newDmg);
        }
    }
}
