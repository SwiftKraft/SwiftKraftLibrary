using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Animator doorAnimator;

    public GameObject[] TargetButtons;// This will reference all buttons that need to be hit in order to open the door, if the door has buttonlocks attached to it


    public bool canBeOpened;

    private void Start()
    {
        canBeOpened = true;
    }

    private void OnTriggerEnter(Collider other)
    {


        if (other.CompareTag("Player") && canBeOpened == true)
        {
            doorAnimator.SetBool("IsOpen", true);
        }
        else
        {
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            doorAnimator.SetBool("IsOpen", false);
        }
    }
}