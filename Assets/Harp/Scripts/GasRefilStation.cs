using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasRefilStation : MonoBehaviour
{
    public UnityEngine.UI.Image gasFillUI;
    private PL_ODM odm;
    public float refillRateMultiplier = 10f;
    public float RefillStationCapacity = 8000;
    private bool isPlayerInTrigger = false;
    // Start is called before the first frame update


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            odm = other.GetComponent<PL_ODM>();

            if (odm != null)
            {
                isPlayerInTrigger = true;
                Debug.Log("Player with ODM found! Starting gas refill...");
            }
            else
            {
                Debug.LogWarning("No PL_ODM component found on the player!");
            }
        }
    }



    // Optional: Clear the reference when player leaves
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (odm != null)
            {
                isPlayerInTrigger = false;
                odm = null;
                Debug.Log("Player left gas station. Refill stopped.");
            }
        }
    }

    void Start()
    {
        
    }

    private void UpdateGasRefillUI()
    {
        gasFillUI.fillAmount = RefillStationCapacity;
    }
        
    void FixedUpdate()
    {

        
        // Constantly refill while player is in trigger and ODM exists
        if (isPlayerInTrigger && odm != null && odm.currentGasAmount < 1500)
        {
            gasFillUI.fillAmount = RefillStationCapacity / 8000;
            RefillStationCapacity -= Time.deltaTime * refillRateMultiplier;
            if (RefillStationCapacity > 0)
            {
                odm.currentGasAmount += Time.deltaTime * refillRateMultiplier;
            }
            else
            {
                return;
            }
            
            
        }
    }
}
