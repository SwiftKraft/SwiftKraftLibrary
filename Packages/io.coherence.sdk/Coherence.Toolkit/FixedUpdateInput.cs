// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    ///     A layer on top of <see cref="UnityEngine.Input" /> that samples inputs at the 'Update' rate and merges them
    ///     appropriately before each <see cref="TimeSync.OnFixedNetworkUpdate" /> thus preventing key-press loss introduced by
    ///     a difference in the 'Update' and network 'FixedUpdate' rate.
    /// </summary>
    /// <example>
    ///     Given `Update` running five times for each network `FixedUpdate`, if we polled inputs from the `FixedUpdate`
    ///     there's a chance that an input was fully processed within the five 'Update's in-between network `FixedUpdate's,
    ///     i.e. a key was "down" on the first 'Update', "pressed" on the second, and "up" on a third one. To prevent this the
    ///     <see cref="FixedUpdateInput" /> samples inputs at 'Update' and prolongs their lifetime to the network 'FixedUpdate'
    ///     so they can be processed correctly there. For our example that would mean a "down" & "pressed" state in the first
    ///     network 'FixedUpdate' after the initial five updates, followed by an "up" state in the subsequent network
    ///     'FixedUpdate'.
    /// </example>
    public partial class FixedUpdateInput
    {
        private static readonly Dictionary<string, KeyCode> keyCodeByName = new Dictionary<string, KeyCode>();

        private readonly IInput input;
        
        private readonly List<InputState> inputStates = new List<InputState>(16);
        private readonly InputState[] keyStates = new InputState[Enum.GetValues(typeof(KeyCode)).Cast<int>().Max() + 1];
        private readonly InputState[] mouseStates = new InputState[3];
        private readonly Dictionary<string, InputState> buttonStates = new Dictionary<string, InputState>();

        private double lastUpdateTime;
        private double lastFixedUpdateTime;

        static FixedUpdateInput()
        {
            foreach(KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                keyCodeByName[keyCode.ToString().ToLower()] = keyCode;
            }
        }

        internal FixedUpdateInput(IInput input = null)
        {
            this.input = input ?? UnityInputSource.Shared;
        }
        
        internal void Update(bool force = false)
        {
            if (!force)
            {
                double time = Time.unscaledTimeAsDouble;
                if (time <= lastUpdateTime)
                {
                    // Prevents multiple updates in a multi-scene setup 
                    return;
                }
                lastUpdateTime = time; 
            }
            
            for (int i = 0; i < inputStates.Count; i++)
            {
                inputStates[i].Update();
            }
        }

        internal void FixedUpdate(bool force = false)
        {
            if (!force)
            {
                double time = Time.fixedUnscaledTimeAsDouble;
                if (time <= lastFixedUpdateTime)
                {
                    // Prevents multiple updates in a multi-scene setup
                    return;
                }
                lastUpdateTime = time;
            }
            
            for (int i = 0; i < inputStates.Count; i++)
            {
                inputStates[i].FixedUpdate();
            }
        }
        
        /// <inheritdoc cref="Input.GetKey(UnityEngine.KeyCode)"/>
        public bool GetKey(KeyCode key) => GetOrCreateKeyState(key).IsPressed;
        /// <inheritdoc cref="Input.GetKeyDown(UnityEngine.KeyCode)"/>
        public bool GetKeyDown(KeyCode key) => GetOrCreateKeyState(key).IsDown;
        /// <inheritdoc cref="Input.GetKeyUp(UnityEngine.KeyCode)"/>
        public bool GetKeyUp(KeyCode key) => GetOrCreateKeyState(key).IsUp;

        /// <inheritdoc cref="Input.GetMouseButton"/>
        public bool GetMouseButton(int button) => GetOrCreateMouseState(button).IsPressed;
        /// <inheritdoc cref="Input.GetMouseButtonDown"/>
        public bool GetMouseButtonDown(int button) => GetOrCreateMouseState(button).IsDown;
        /// <inheritdoc cref="Input.GetMouseButtonUp"/>
        public bool GetMouseButtonUp(int button) => GetOrCreateMouseState(button).IsUp;

        /// <inheritdoc cref="Input.GetKey(string)"/>
        public bool GetKey(string name) => GetOrCreateKeyState(name).IsPressed;
        /// <inheritdoc cref="Input.GetKeyDown(string)"/>
        public bool GetKeyDown(string name) => GetOrCreateKeyState(name).IsDown;
        /// <inheritdoc cref="Input.GetKeyUp(string)"/>
        public bool GetKeyUp(string name) => GetOrCreateKeyState(name).IsUp;

        /// <inheritdoc cref="Input.GetButton"/>
        public bool GetButton(string buttonName) => GetOrCreateButtonState(buttonName).IsPressed;
        /// <inheritdoc cref="Input.GetButtonDown"/>
        public bool GetButtonDown(string buttonName) => GetOrCreateButtonState(buttonName).IsDown;
        /// <inheritdoc cref="Input.GetButtonUp"/>
        public bool GetButtonUp(string buttonName) => GetOrCreateButtonState(buttonName).IsUp;

        private InputState GetOrCreateKeyState(string keyName)
        {
            if (!keyCodeByName.TryGetValue(keyName.ToLowerInvariant(), out KeyCode keyCode))
            {
                throw new ArgumentOutOfRangeException($"Unsupported key: {keyName}");
            }

            return GetOrCreateKeyState(keyCode);
        }
        
        private InputState GetOrCreateKeyState(KeyCode key)
        {
            int keyIndex = (int)key;
            if (keyIndex < 0 || keyIndex > keyStates.Length)
            {
                throw new ArgumentOutOfRangeException($"Unsupported key: {key}");
            }
            
            InputState state = keyStates[keyIndex];
            if (state != null)
            {
                return state;
            }

            state = new ButtonState<KeyCode>(key, input);
            state.Update();
            keyStates[keyIndex] = state;
            inputStates.Add(state);
            return state;
        }
        
        private InputState GetOrCreateMouseState(int mouseButton)
        {
            if (mouseButton < 0 || mouseButton > mouseStates.Length)
            {
                throw new ArgumentOutOfRangeException($"Unsupported button: {mouseButton}");
            }
            
            InputState state = mouseStates[mouseButton];
            if (state != null)
            {
                return state;
            }

            state = new ButtonState<MouseButton>(new MouseButton{Button = mouseButton}, input);
            state.Update();
            mouseStates[mouseButton] = state;
            inputStates.Add(state);
            return state;
        }

        private InputState GetOrCreateButtonState(string buttonName)
        {
            if (buttonStates.TryGetValue(buttonName, out InputState state))
            {
                return state;
            }

            state = new ButtonState<CustomButton>(new CustomButton{Name = buttonName}, input);
            state.Update();
            buttonStates[buttonName] = state;
            inputStates.Add(state);
            return state;
        }
    }
}
