using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwiftKraft.Saving.Settings.UI.Utilities
{
    public class SliderEvents : MonoBehaviour, IPointerUpHandler
    {
        public Action<PointerEventData> OnReleaseSlider;

        public void OnPointerUp(PointerEventData eventData) => OnReleaseSlider?.Invoke(eventData);
    }
}
