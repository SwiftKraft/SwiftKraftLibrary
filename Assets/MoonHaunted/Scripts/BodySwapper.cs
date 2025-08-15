using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BodySwapper : MonoBehaviour
{
    public readonly static List<BodySwapper> Instances = new();
    public static BodySwapper PlayerInstance { get; private set; }

    public LayerMask RaycastLayers;
    public Transform RaycastPoint;

    public bool CurrentControlled => PlayerInstance == this;

    public UnityEvent OnPlayerControl;
    public UnityEvent OnBotControl;

    private void Awake()
    {
        Instances.Add(this);
        SetControl(Instances[0] == this);
    }

    private void OnDestroy() => Instances.Remove(this);

    BodySwapper queued;

    private void Update()
    {
        if (!CurrentControlled)
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
        {
            OnBotControl?.Invoke();
            PlayerInstance = null;
        }
    }
}
