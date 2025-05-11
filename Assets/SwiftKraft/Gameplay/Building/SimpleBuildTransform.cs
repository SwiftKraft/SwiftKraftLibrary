using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Building
{
    public class SimpleBuildTransform : MonoBehaviour
    {
        public Transform Target;
        public GameObject Prefab;

        public UnityEvent<GameObject> OnBuild;
        public UnityEvent OnRemove;

        GameObject built;

        public void Build()
        {
            if (BuildingManager.Instance == null || built != null)
                return;

            built = BuildingManager.Instance.Create(Prefab, new(Target));
            OnBuild?.Invoke(built);
        }

        public void Remove()
        {
            Destroy(built);
            OnRemove?.Invoke();
        }
    }
}
