using SwiftKraft.Gameplay.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ODMandGunSwitch : MonoBehaviour
{
    public MonoBehaviour PL_ODM;
    public ItemEquipper ItemEquip;
    public float toggleDelay = 0.5f;

    private bool isODMActive = true;
    private bool isToggling = false;

    void Start()
    {
        if (PL_ODM != null && ItemEquip != null)
        {
           PL_ODM.enabled = true;
            ItemEquip.ForceUnequip();

        }
        else
        {
            Debug.LogError("Please assign both components in the Inspector!");
        }
    }


    private void FixedUpdate()
    {
        DifferentSwitchConditions();
    }


    public void DifferentSwitchConditions()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!isODMActive)
            {
                SwitchToODM();
            }
            else
            {
                SwitchToGun();
            }
        }
    }
    public void SwitchToODM()
    {
        if (!isToggling)
        {
            StartCoroutine(ToggleIntoODM());
        }
    }

    private IEnumerator ToggleIntoODM()
    {
        isToggling = true;

            PL_ODM.enabled = true;
        ItemEquip.ForceUnequip();
        yield return new WaitForSeconds(toggleDelay);
        


        isODMActive = true;
        isToggling = false;
    }

    public void SwitchToGun()
    {
        if (!isToggling)
        {
            StartCoroutine(ToggleIntoGun());
        }
    }

    private IEnumerator ToggleIntoGun()
    {
        isToggling = true;

        PL_ODM.enabled = false;
        yield return new WaitForSeconds(toggleDelay);



        isODMActive = !isODMActive;
        isToggling = false;
    }

}
