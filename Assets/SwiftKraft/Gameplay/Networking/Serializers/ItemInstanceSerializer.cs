using FishNet.Serializing;
using Newtonsoft.Json;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Saving.Data;
using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Networking.Serializers
{
    public static class ItemInstanceSerializer
    {
        public static void WriteItemInstance(this Writer writer, ItemInstance item)
        {
            writer.Write(item.Serial);
            writer.Write(item.Type.ID);
            string serialize = JsonConvert.SerializeObject(item.Data);

            writer.Write(serialize);
        }

        public static ItemInstance ReadItemInstance(this Reader reader)
        {
            uint serial = reader.Read<uint>();
            string typeId = reader.Read<string>();
            var inst = new ItemInstance(serial, typeId);

            Dictionary<string, SaveDataBase> data = JsonConvert.DeserializeObject<Dictionary<string, SaveDataBase>>(reader.Read<string>());

            foreach (var ent in data)
                inst.Data.Add(ent.Key, ent.Value);

            return inst;
        }
    }
}
