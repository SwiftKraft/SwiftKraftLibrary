using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

public class GreaseGun : MonoBehaviour
{
    public SkinnedMeshRenderer rend;

    public Material original;
    public Material other;

    public GameObject flash1;
    public GameObject flash2;

    public WeaponParticle particle;

    public int index;

    public void Switch(bool state)
    {
        Material[] mats = rend.materials;
        mats[index] = state ? other : original;
        rend.materials = mats;

        particle.SetOverride("Attack", state ? flash2 : flash1);
        Debug.Log("Switch");
    }
}
