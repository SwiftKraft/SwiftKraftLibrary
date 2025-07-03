using FishNet.Connection;
using FishNet.Object;
using SwiftKraft.Gameplay.Inventory.Items;
using System;

namespace SwiftKraft.Gameplay.Networking.Inventory.Items
{
    public class NetworkItemManager : NetworkBehaviour
    {
        public static NetworkItemManager Instance { get; private set; }

        public event Action<ItemInstance> OnReceiveItem;

        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            Instance = this;
        }

        /// <summary>
        /// Called when the client wants to access data from an item.
        /// </summary>
        /// <param name="serial"></param>
        public void ClientRequestItem(uint serial)
        {
            if (!IsClientInitialized)
                return;

            if (ItemManager.TryGetInstance(serial, out ItemInstance inst))
            {
                OnReceiveItem?.Invoke(inst);
                return;
            }

            ServerRpcRequestItem(LocalConnection, serial);
        }

        /// <summary>
        /// Called when the client receives the server's RPC.
        /// </summary>
        /// <param name="inst"></param>
        public void ClientReceiveItem(ItemInstance inst)
        {
            if (!ItemManager.Instances.ContainsKey(inst.Serial))
                inst.AddInstance();
            else
            {
                ItemManager.Instances[inst.Serial].Despawn();
                ItemManager.Instances[inst.Serial] = inst;
            }

            inst.Refresh();

            OnReceiveItem?.Invoke(inst);
        }

        /// <summary>
        /// Called on the server to send the information to the requested client.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serial"></param>
        public void ServerSendItem(NetworkConnection client, uint serial)
        {
            if (ItemManager.TryGetInstance(serial, out ItemInstance inst))
                TargetRpcSendItem(client, inst);
        }

        [TargetRpc]
        public void TargetRpcSendItem(NetworkConnection client, ItemInstance inst) => ClientReceiveItem(inst);

        [ServerRpc]
        public void ServerRpcRequestItem(NetworkConnection client, uint serial) => ServerSendItem(client, serial);
    }
}
