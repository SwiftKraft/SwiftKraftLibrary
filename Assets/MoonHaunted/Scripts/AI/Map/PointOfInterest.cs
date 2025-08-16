using SwiftKraft.Gameplay.NPCs;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    public static readonly HashSet<PointOfInterest> AllPoints = new();
    public static readonly HashSet<PointOfInterest> VacantPoints = new();

    public bool Vacant
    {
        get => VacantPoints.Contains(this);
        set
        {
            if (Vacant == value)
                return;

            if (value)
                VacantPoints.Add(this);
            else
                VacantPoints.Remove(this);
        }
    }

    [field: SerializeField]
    public PointFlags Flags { get; private set; }

    public NPCCore TakenBy
    {
        get => takenBy;
        set
        {
            takenBy = value;
            Vacant = takenBy == null;
        }
    }
    private NPCCore takenBy;

    private void Awake()
    {
        AllPoints.Add(this);
        Vacant = true;
    }

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
