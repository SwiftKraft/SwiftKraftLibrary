using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    [System.Serializable]
    public struct SerializedAgentType
    {
        [SerializeField] private int agentTypeID;

        public static implicit operator SerializedAgentType(int agentTypeID) => new() { agentTypeID = agentTypeID };
        public static implicit operator int(SerializedAgentType agentType) => agentType.agentTypeID;
    }
}