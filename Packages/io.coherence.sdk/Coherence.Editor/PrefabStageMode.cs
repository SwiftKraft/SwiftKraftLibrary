// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    /// <summary>
    /// Specifies the different options for <see cref="GameObjectStatus.PrefabStageMode"/>.
    /// </summary>
    public enum PrefabStageMode
    {
        /// <summary>
        /// GameObject is not being edited in Prefab Mode.
        /// </summary>
        None,

        /// <summary>
        /// GameObject is being edited on its own in Prefab Mode.
        /// </summary>
        InIsolation,

        /// <summary>
        /// GameObject is being edited in Prefab Mode while retaining the context of the Prefab instance through which it was opened.
        /// </summary>
        InContext,
    }
}
