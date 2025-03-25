using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.UI;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class Workbench : MonoBehaviour, IInteractable
    {
        public GameObject MenuPrefab;

        public static WeaponAttachmentMenu Menu;

        private void Start()
        {
            if (Menu == null)
            {
                Menu = MenuManager.Instance.AddMenu<WeaponAttachmentMenu>("Workbench", MenuPrefab);
                Menu.Active = false;
            }
        }

        public void Interact(InteractorBase interactor)
        {
            Menu.Active = true;
            Menu.Target = interactor.GetComponentInChildren<WeaponAttachments>(false);
        }
    }
}
