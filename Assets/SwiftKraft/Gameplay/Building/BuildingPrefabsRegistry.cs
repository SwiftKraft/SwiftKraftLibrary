using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Building/Prefab Registry", fileName = "New Registry")]
    public class BuildingPrefabsRegistry : ScriptableObject
    {
        public Entry[] Entries;

        [Serializable]
        public struct Entry
        {
            public string ID;
            public GameObject Prefab;
            public GameObject OverrideBlueprint;
        }
    }
}
