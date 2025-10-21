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
            Transform trans = Workspaces.FirstOrDefault(n => n.Name == model.WorkspaceName).Reference;

            if (trans == null)
            {
                model.gameObject.SetActive(false);
                return;
            }

            model.transform.parent = trans;
            model.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
        }
    }
}
