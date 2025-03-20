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

        public MenuBase AddMenu(string id, GameObject prefab)
        {
            GameObject go = Instantiate(prefab, Workspace);

            if (!go.TryGetComponent(out MenuBase menu))
            {
                DestroyImmediate(go, false);
                Debug.LogError("Prefab \"" + prefab + "\" doesn't have a MenuBase component on it!");
                return null;
            }

            Menus.Add(id, menu);
            return menu;
        }
    }
}
