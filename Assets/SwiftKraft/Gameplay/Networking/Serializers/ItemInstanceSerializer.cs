using FishNet.Serializing;
using SwiftKraft.Gameplay.Inventory.Items;

public static class ItemInstanceSerializer
{
    public static void WriteItemInstance(this Writer writer, ItemInstance item)
    {
        writer.Write(item.Serial);
        writer.Write(item.Type.ID);

        // Write data
    }

    public static ItemInstance ReadItemInstance(this Reader reader)
    {
        uint serial = reader.Read<uint>();
        string typeId = reader.Read<string>();
        var inst = new ItemInstance(serial, typeId);

        // Read data

        return inst;
    }
}
