using FishNet.Connection;
using FishNet.Object;
using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Networking.Inventory.Items
{
    public class NetworkItemManager : NetworkBehaviour
    {
        public static NetworkItemManager Instance { get; private set; }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            Instance = this;
        }

        // do async

        //public ItemInstance ClientRequestItem(uint serial)
        //{
        //    if (!IsClientInitialized)
        //        return null;

        //    if (ItemManager.TryGetInstance(serial, out ItemInstance inst))
        //        return inst;

            
        //}

        public void ClientReceiveItem(ItemInstance inst)
        {
            if (!ItemManager.Instances.ContainsKey(inst.Serial))
                inst.AddInstance();
            else
                ItemManager.Instances[inst.Serial] = inst;
        }

        [TargetRpc]
        public void TargetRpcSendItem(ItemInstance inst)
        {
            ClientReceiveItem(inst);
        }

        public void ServerSendItem(uint serial)
        {
            if (ItemManager.TryGetInstance(serial, out ItemInstance inst))
                TargetRpcSendItem(inst);
        }

        [ServerRpc]
        public void ServerRpcRequestItem(uint serial)
        {
            ServerSendItem(serial);
        }
    }
}
