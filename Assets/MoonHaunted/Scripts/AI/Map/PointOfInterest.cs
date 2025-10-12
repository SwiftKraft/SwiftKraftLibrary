using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour, IWeight
{
    public static readonly List<PointOfInterest> AllPoints = new();
    public static readonly List<PointOfInterest> VacantPoints = new();

    public static readonly Comparer<PointOfInterest> Comparer = Comparer<PointOfInterest>.Create((a, b) => b.Weight.CompareTo(a.Weight));

    public bool Vacant
    {
        get => VacantPoints.Contains(this);
        set
        {
            if (Vacant == value)
                return;

            if (value)
            {
                VacantPoints.Add(this);
                VacantPoints.Sort(Comparer);
            }
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
        VacantPoints.RemoveAll(n => n == null);
        AllPoints.RemoveAll(n => n == null);
        AllPoints.Add(this);
        AllPoints.Sort(Comparer);
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
    Nest = 1 << 6
}
