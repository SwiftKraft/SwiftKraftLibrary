// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.UI
{
    using System;
    using Cloud;
    using UnityEngine;
    using UnityEngine.UI;

    [Obsolete("ConnectDialogRoomView is part of the Legacy Sample UI. Find the new UI Samples on the menu item coherence/Explore Samples.")]
    [Deprecated("04/2023", 1, 2, 0)]
    public class ConnectDialogRoomView : MonoBehaviour
    {
        [SerializeField] private Text roomName;
        [SerializeField] private Text playersNumberText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image backgroundImage;

        public Color SelectedColor;
        public Color DefaultColor;
        public RoomData RoomData;

        public Text RoomName => roomName;
        public Text PlayersNumberText => playersNumberText;
        public Button SelectButton => selectButton;
        public Image BackgroundImage => backgroundImage;

        private void Awake()
        {
            DefaultColor = backgroundImage.color;
        }
    }
}
