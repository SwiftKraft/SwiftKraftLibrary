using System;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    public static readonly HashSet<PointOfInterest> AllPoints = new();
    public PointFlags Flags;

    private void Awake() => AllPoints.Add(this);

    private void OnDestroy() => AllPoints.Remove(this);
}

[Flags]
public enum PointFlags : byte
{
    None = 0,
    FlankSecurity = 1 << 0,
    Defensive = 1 << 1,
    Offensive = 1 << 2,
    HidingSpot = 1 << 3,
    HostileAngle = 1 << 4,
    ChokePoint = 1 << 5,
}
