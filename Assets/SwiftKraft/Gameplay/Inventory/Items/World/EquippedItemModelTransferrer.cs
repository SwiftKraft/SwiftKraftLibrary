using SwiftKraft.Gameplay.Interfaces;
using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class EquippedItemModelTransferrer : MonoBehaviour
    {
        [Serializable]
        public struct Workspace
        {
            public string Name;
            public Transform Reference;
        }

        public Workspace[] Workspaces;

        public void Transfer(EquippedItemModel model)
        {
            model.transform.parent = Workspaces.FirstOrDefault(n => n.Name == model.WorkspaceName).Reference;
            model.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
        }
    }
}
