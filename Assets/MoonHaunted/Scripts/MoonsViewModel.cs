using SwiftKraft.Gameplay.Common.FPS.ViewModels;
using UnityEngine;

public class MoonsViewModel : ViewModel
{
    public BodySwapper Swapper { get; private set; }

    public Transform FP;
    public Transform TP;

    protected override void Awake()
    {
        base.Awake();

        Swapper = GetComponentInParent<BodySwapper>();

        FP.parent = Swapper.FPWorkspace;
        TP.parent = Swapper.TPWorkspace;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        FP.parent = Swapper.FPWorkspace;
        TP.parent = Swapper.TPWorkspace;
    }

    protected virtual void OnDisable()
    {
        FP.parent = transform;
        TP.parent = transform;
    }
}
