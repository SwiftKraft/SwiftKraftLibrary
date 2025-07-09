namespace Coherence.Toolkit
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Log;
    using Logger = Log.Logger;


    public class CoherenceSceneManager
    {
        private CoherenceClientConnectionManager clientConnections;
        private IClient client;
        private static Logger logger;

        /// <summary>
        /// Stores the new scene while waiting for the connection client (which is
        /// created on the server and is not available immediately after connecting).
        /// </summary>
        private uint? pendingNewScene;

        private uint? lastSetScene;

        public CoherenceSceneManager(CoherenceClientConnectionManager clientConnections, IClient client)
        {
            Debug.Assert(client != null);

            this.clientConnections = clientConnections;
            this.client = client;
            logger = Log.GetLogger<CoherenceSceneManager>();
        }

        public void SetClientScene(int newSceneIndex)
        {
            if (newSceneIndex < 0)
            {
                logger.Warning(Warning.ToolkitSceneNegativeIndex,
                    ("index", newSceneIndex));

                return;
            }

            SetClientScene((uint)newSceneIndex);
        }

        public void SetClientScene(uint newSceneIndex)
        {
            lastSetScene = newSceneIndex;

            var myClientConnection = clientConnections.GetMine();

            if (myClientConnection == null)
            {
                pendingNewScene = newSceneIndex;
                return;
            }

            myClientConnection.SendConnectionSceneUpdate(newSceneIndex);
            pendingNewScene = null;
        }

        public uint GetClientScene()
        {
            return lastSetScene.HasValue ? lastSetScene.Value : client.InitialScene;
        }

        internal void GotMyClientConnection(CoherenceClientConnection myClientConnection)
        {
            if (pendingNewScene.HasValue)
            {
                myClientConnection.SendConnectionSceneUpdate(pendingNewScene.Value);
                pendingNewScene = null;
            }
        }

        /// <summary>
        ///     <inheritdoc cref="LoadScene"/>
        ///     <paramref name="scenePath"/>Must be a complete path to the scene, e.g. "Assets/Scenes/Scene1.scene"</paramref>
        /// </summary>
        public static IEnumerator LoadScene(CoherenceBridge bridge, string scenePath, IEnumerable<CoherenceSync> bringAlong = null, IEnumerable<CoherenceSync> leaveBehind = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            var sceneBuildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            if (sceneBuildIndex < 0)
            {
                logger.Error(Error.ToolkitSceneInvalidScene,
                    ("scenePath", scenePath));

                return null;
            }

            return LoadScene(bridge, sceneBuildIndex, bringAlong, leaveBehind, loadSceneMode);
        }

        /// <summary>
        /// Helper method for loading a new Unity scene in a way that also works with coherence scenes.
        /// Before making the scene switch, it will make sure authorization is correct for entities that
        /// should be moved to the new scene and for entities that should be left behind in the old one.
        /// It will also make the necessary calls to set the scene on your <see cref="Coherence.Toolkit.CoherenceClientConnection" />,
        /// and the instantiation scene on the <see cref="Coherence.Toolkit.CoherenceBridge" />.
        ///
        /// </summary>
        ///
        /// <param name="loadSceneMode">Load scene mode for a scene to be loaded with. Default value isLoadSceneMode.Single.</param>
        /// <param name="bringAlong">Entities that should be transferred to the next scene. These must be root
        /// objects in the scene since they are temporarily marked as DontDestroyOnLoad.
        /// It is necessary that authority can be transferred or stolen for the entities that should be
        /// brought along, or otherwise this co-routine will yield indefinitely (waiting for authority).</param>
        /// <param name="leaveBehind">Entities that should be left in the scene. This method will abandon
        /// those entities (which might take a few frames) and then perform the scene load.</param>
        public static IEnumerator LoadScene(CoherenceBridge bridge, int sceneBuildIndex, IEnumerable<CoherenceSync> bringAlong = null, IEnumerable<CoherenceSync> leaveBehind = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (bringAlong != null)
            {
                foreach (var sync in bringAlong)
                {
                    if (sync.transform.parent != null)
                    {
                        logger.Warning(Warning.ToolkitSceneLoadSceneMoveNonRootEntity,
                            ("entity", sync));

                        continue;
                    }

                    if (sync.EntityState.IsOrphaned)
                    {
                        sync.Adopt();
                    }
                    else if (!sync.HasStateAuthority)
                    {
                        sync.RequestAuthority(AuthorityType.State);
                    }
                }
            }

            if (leaveBehind != null)
            {
                foreach (var sync in leaveBehind)
                {
                    if (sync.HasStateAuthority)
                    {
                        sync.AbandonAuthority();
                    }
                }
            }

            yield return new WaitUntil(() =>
            {
                return (bringAlong == null || bringAlong.All(sync => sync.transform.parent != null || sync.HasStateAuthority))
                    && (leaveBehind == null || leaveBehind.All(sync => !sync.HasStateAuthority));
            });

            if (bringAlong != null)
            {
                foreach (var sync in bringAlong)
                {
                    if (sync.transform.parent == null)
                    {
                        GameObject.DontDestroyOnLoad(sync.gameObject);
                    }
                }
            }

            yield return null;

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneBuildIndex, loadSceneMode);

            yield return new WaitUntil(() => load.isDone);

            yield return null;

            var scene = SceneManager.GetSceneByBuildIndex(sceneBuildIndex);

            bridge.SceneManager.SetClientScene(sceneBuildIndex);
            bridge.InstantiationScene = scene;

            if (bringAlong != null)
            {
                foreach (var sync in bringAlong)
                {
                    if (sync.transform.parent == null)
                    {
                        SceneManager.MoveGameObjectToScene(sync.gameObject, scene);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method for loading scenes additively. The currently open scene will remain open.
        /// Any networked entities in the additively loaded scene will use the current bridge for networking.
        /// </summary>
        /// <param name="mergeScenes">Optionally, the additively loaded scene can be merged into the currently open scene.</param>
        public static IEnumerator LoadSceneAdditive(CoherenceBridge bridge, string scenePath, bool mergeScenes = true)
        {
            var sceneBuildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            if (sceneBuildIndex < 0)
            {
                logger.Error(Error.ToolkitSceneInvalidScene,
                    ("scenePath", scenePath));

                return null;
            }

            return LoadSceneAdditive(bridge, sceneBuildIndex, mergeScenes);
        }

        /// <summary>
        /// Helper method for loading scenes additively. The currently open scene will remain open.
        /// Any networked entities in the additively loaded scene will use the current bridge for networking.
        /// </summary>
        /// <param name="mergeScenes">Optionally, the additively loaded scene can be merged into the currently open scene.</param>
        public static IEnumerator LoadSceneAdditive(CoherenceBridge bridge, int sceneBuildIndex, bool mergeScenes = false)
        {
            SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByBuildIndex(sceneBuildIndex);
            if (!scene.IsValid())
            {
                logger.Error(Error.ToolkitSceneInvalidScene,
                    ("sceneBuildIndex", sceneBuildIndex));

                yield break;
            }

            CoherenceBridgeStore.RegisterBridge(bridge, scene, bridge.mainBridge);

            yield return new WaitUntil(() => scene.isLoaded);

            if (mergeScenes)
            {
                SceneManager.MergeScenes(scene, bridge.gameObject.scene);
            }
        }
    }
}
