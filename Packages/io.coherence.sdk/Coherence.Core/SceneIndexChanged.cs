// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using Entities;

    public struct SceneIndexChanged
    {
        public Entity EntityID;
        public int SceneIndex;

        public SceneIndexChanged(Entity entityID, int sceneIndex)
        {
            EntityID = entityID;
            SceneIndex = sceneIndex;
        }
    }
}
