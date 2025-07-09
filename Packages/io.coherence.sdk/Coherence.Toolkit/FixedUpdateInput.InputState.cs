// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    public partial class FixedUpdateInput
    {
        internal enum ButtonStatus
        {
            None,
            Down,
            Pressed,
            Up
        }

        internal abstract class InputState
        {
            public ButtonStatus Status { get; protected set; }

            public bool IsDown => Status == ButtonStatus.Down;
            public bool IsPressed => IsDown || Status == ButtonStatus.Pressed;
            public bool IsUp => Status == ButtonStatus.Up;

            public abstract void Update();
            public abstract void FixedUpdate();
        }

        internal class ButtonState<TButton> : InputState
        {
            public readonly TButton Button;

            private readonly IInputSource<TButton> inputSource;

            private ButtonStatus lastUpdateStatus;
            private int downs;
            private int ups;
            private int releases;

            public ButtonState(TButton button, IInputSource<TButton> source = null)
            {
                Button = button;
                inputSource = source ?? ((IInputSource<TButton>)UnityInputSource.Shared);
            }

            public override void FixedUpdate()
            {
                switch (Status)
                {
                    case ButtonStatus.None:
                        if (downs > 0)
                        {
                            Status = ButtonStatus.Down;
                            downs--;
                        }
                        break;

                    case ButtonStatus.Down:
                    case ButtonStatus.Pressed:
                        if (ups > 0)
                        {
                            Status = ButtonStatus.Up;
                            ups--;
                        }
                        else
                        {
                            Status = ButtonStatus.Pressed;
                        }
                        break;

                    case ButtonStatus.Up:
                        if (downs > 0)
                        {
                            Status = ButtonStatus.Down;
                            downs--;
                        }
                        else if (releases > 0)
                        {
                            Status = ButtonStatus.None;
                            releases--;
                        }
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(Status), Status, $"Invalid {nameof(Status)}");
                }
            }

            public override void Update()
            {
                switch (lastUpdateStatus)
                {
                    case ButtonStatus.None:
                        if (inputSource.GetButtonDown(Button))
                        {
                            lastUpdateStatus = ButtonStatus.Down;
                            downs++;
                        }
                        break;

                    case ButtonStatus.Up:
                        releases++;

                        if (inputSource.GetButtonDown(Button))
                        {
                            lastUpdateStatus = ButtonStatus.Down;
                            downs++;
                        }
                        else
                        {
                            lastUpdateStatus = ButtonStatus.None;
                        }
                        break;

                    case ButtonStatus.Pressed:
                    case ButtonStatus.Down:
                        if (inputSource.GetButton(Button))
                        {
                            lastUpdateStatus = ButtonStatus.Pressed;
                        }
                        else
                        {
                            lastUpdateStatus = ButtonStatus.Up;
                            ups++;
                        }
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(Status), Status, $"Invalid {nameof(lastUpdateStatus)}");
                }
            }
        }
    }
}
