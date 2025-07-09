// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;

    public class Server
    {
        internal static bool UpdateOnlineServer()
        {
            var ok = true;
            ok |= ProjectSettings.instance.RehashActiveSchemas();
            ok |= Portal.Schemas.UploadActive();
            ok |= Portal.ReplicationServer.Deploy();
            Portal.Schemas.UpdateSyncState();
            if (!ok)
            {
                _ = EditorUtility.DisplayDialog("Online Server", "Uploading schemas failed!", "OK");
            }

            return ok;
        }

        internal static bool RestartOnlineServer()
        {
            bool ok = Portal.ReplicationServer.Restart();
            if (!ok)
            {
                _ = EditorUtility.DisplayDialog("Online Server", "The replication server couldn't be restarted!", "OK");
            }

            return ok;
        }
    }
}
