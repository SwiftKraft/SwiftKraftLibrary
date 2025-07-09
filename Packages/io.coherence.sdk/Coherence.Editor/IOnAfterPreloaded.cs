using UnityEditor;

namespace Coherence.Editor
{
    /// <summary>
    /// PreloadedSingletons and their custom editors implementing this interface
    /// will get notified when the asset is created and/or added to the preloaded assets.
    /// </summary>
    internal interface IOnAfterPreloaded
    {
        void OnAfterPreloaded();
    }
}
