using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour, IWeight
{
    public static readonly SortedSet<PointOfInterest> AllPoints = new(Comparer<PointOfInterest>.Create((a, b) => b.Weight.CompareTo(a.Weight)));
    public static readonly SortedSet<PointOfInterest> VacantPoints = new(Comparer<PointOfInterest>.Create((a, b) => b.Weight.CompareTo(a.Weight)));

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

    [field: SerializeField]
    public int Weight { get; set; }

    private NPCCore takenBy;

    private void Awake()
    {
        VacantPoints.RemoveWhere(n => n == null);
        AllPoints.RemoveWhere(n => n == null);
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
