using SwiftKraft.Gameplay.Common.FPS.ViewModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoonsViewModel : ViewModel
{
    public BodySwapper Swapper { get; private set; }

    public Transform FP;
    public Transform TP;

    bool initialized;

    protected override void Awake()
    {
        base.Awake();

        Swapper = GetComponentInParent<BodySwapper>();
    }

    private void Start()
    {
        initialized = true;
        FP.parent = Swapper.FPWorkspace;
        FP.SetLocalPositionAndRotation(default, Quaternion.identity);
        TP.parent = Swapper.TPWorkspace;
        TP.SetLocalPositionAndRotation(default, Quaternion.identity);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!initialized)
            return;

        FP.parent = Swapper.FPWorkspace;
        FP.SetLocalPositionAndRotation(default, Quaternion.identity);
        FP.gameObject.SetActive(true);
        TP.parent = Swapper.TPWorkspace;
        TP.SetLocalPositionAndRotation(default, Quaternion.identity);
        TP.gameObject.SetActive(true);
    }

    protected virtual void OnDisable()
    {
        FP.gameObject.SetActive(false);
        TP.gameObject.SetActive(false);
    }
}
