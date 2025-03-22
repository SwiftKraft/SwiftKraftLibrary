using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.UI
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [field: SerializeField]
        public RectTransform Workspace { get; private set; }

        public readonly Dictionary<string, MenuBase> Menus = new();

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public bool TryAddMenu<T>(string id, GameObject prefab, out T menu) where T : MenuBase
        {
            menu = AddMenu<T>(id, prefab);
            return menu != null;
        }

        public T AddMenu<T>(string id, GameObject prefab) where T : MenuBase
        {
            if (Menus.ContainsKey(id))
                return null;

            GameObject go = Instantiate(prefab, Workspace);

            if (!go.TryGetComponent(out T menu))
            {
                Debug.LogError("Prefab \"" + prefab + "\" doesn't have a MenuBase component on it!");
                return null;
            }

            Menus.Add(id, menu);
            return menu;
        }

        public void RemoveMenu(string id)
        {
            if (!Menus.ContainsKey(id))
                return;

            Destroy(Menus[id].gameObject);
            Menus.Remove(id);
        }

        public void SetMenu(string id, bool active)
        {
            if (!Menus.ContainsKey(id))
                return;

            Menus[id].Active = active;
        }
    }
}
