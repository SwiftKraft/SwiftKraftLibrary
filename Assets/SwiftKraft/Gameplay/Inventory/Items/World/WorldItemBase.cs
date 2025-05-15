using Newtonsoft.Json;
using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WorldItemBase : MonoBehaviour, IInteractable
    {
        public const string TransformDataID = "Essentials.TransformData";

        public Data CurrentData { get; set; }

        public ItemType StartType;

        public Guid Item { get; private set; }
        public ItemInstance Instance => Item;

        protected virtual void Awake()
        {
            if (StartType != null)
                Init(new ItemInstance(StartType).Guid);
        }

        public virtual void Init(Guid item)
        {
            Item = item;

            Instance.OnRefresh += OnRefresh;

            CurrentData = Instance.TryGetData(TransformDataID, out Data d) ? d : Instance.AddData<Data>(TransformDataID);
            CurrentData.Init(this);

            foreach (WorldItemAddonBase addon in GetComponents<WorldItemAddonBase>())
                addon.Init(this);
        }

        protected virtual void OnRefresh() => DestroyImmediate(gameObject, false);

        public virtual void Interact(InteractorBase interactor)
        {
            if (interactor.TryGetComponent(out InventoryBase inv))
            {
                inv.AddItem(Item);
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            Instance.RemoveData(TransformDataID);
            Instance.OnRefresh -= OnRefresh;
        }

        public class Data : ItemDataBase
        {
            public WorldItemBase WorldItem { get; private set; }

            [JsonProperty]
            public TransformData Transform { get => new(WorldItem.transform); set => WorldItem.transform.SetPositionAndRotation(value.Position, value.Rotation); }

            public void Init(WorldItemBase worldItem) => WorldItem = worldItem;
        }
    }
}
