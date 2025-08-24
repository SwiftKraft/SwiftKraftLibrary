using SwiftKraft.Gameplay.Motors;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BodySwapper : MonoBehaviour
{
    public readonly static List<BodySwapper> Instances = new();
    public static BodySwapper PlayerInstance { get; private set; }

    public MoonsHauntedPlayerController Controller { get; private set; }

    public LayerMask RaycastLayers;
    public Transform RaycastPoint;

    public bool PlayerControlled => PlayerInstance == this;

    public UnityEvent OnPlayerControl;
    public UnityEvent OnBotControl;

    private void Awake()
    {
        Instances.Add(this);
        Controller = GetComponent<MoonsHauntedPlayerController>();
        SetControl(Instances[0] == this);
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
        if (PlayerControlled)
            PlayerInstance = null;
    }

    BodySwapper queued;

    private void Update()
    {
        if (!PlayerControlled)
            return;

        if (Input.GetKeyDown(KeyCode.E) && Physics.Raycast(RaycastPoint.position, RaycastPoint.forward, out RaycastHit _hit, 500f, RaycastLayers, QueryTriggerInteraction.Collide) && _hit.collider.TryGetComponent(out BodySwapper swapper))
            queued = swapper;
    }

    private void FixedUpdate()
    {
        if (queued != null)
        {
            SwitchControl(queued);
            queued = null;
        }
    }

    public void SwitchControl(BodySwapper target)
    {
        SetControl(false);
        target.SetControl(true);
    }

    public void SetControl(bool player)
    {
        if (player)
        {
            OnPlayerControl?.Invoke();
            PlayerInstance = this;
        }
        else
            OnBotControl?.Invoke();
    }
}
