// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    public partial class FixedUpdateInput
    {
        internal struct MouseButton { public int Button; }
        internal struct CustomButton { public string Name; }

        internal interface IInputSource<TButton>
        {
            bool GetButton(TButton button);
            bool GetButtonDown(TButton button);
            bool GetButtonUp(TButton button);
        }

        internal interface IInput :
            IInputSource<KeyCode>,
            IInputSource<MouseButton>,
            IInputSource<CustomButton>
        {
        }

        internal class UnityInputSource : IInput
        {
            public static readonly UnityInputSource Shared = new UnityInputSource();

            public bool AnyKey => Input.anyKey;
            public bool AnyKeyDown => Input.anyKeyDown;

            public bool GetButton(KeyCode keyCode) => Input.GetKey(keyCode);
            public bool GetButtonDown(KeyCode keyCode) => Input.GetKeyDown(keyCode);
            public bool GetButtonUp(KeyCode keyCode) => Input.GetKeyUp(keyCode);

            public bool GetButton(MouseButton button) => Input.GetMouseButton(button.Button);
            public bool GetButtonDown(MouseButton button) => Input.GetMouseButtonDown(button.Button);
            public bool GetButtonUp(MouseButton button) => Input.GetMouseButtonUp(button.Button);

            public bool GetButton(CustomButton button) => Input.GetButton(button.Name);
            public bool GetButtonDown(CustomButton button) => Input.GetButtonDown(button.Name);
            public bool GetButtonUp(CustomButton button) => Input.GetButtonUp(button.Name);
        }
    }
}
