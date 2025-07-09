namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;
    using Editor;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor.SceneManagement;

    [TestFixture]
    public class ActiveSceneScopeTests : CoherenceTest
    {
        private const string TestScene = Paths.packageRootPath + "/Coherence.Toolkit.Tests/ActiveSceneScopeTests.unity";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _ = EditorSceneManager.OpenScene(TestScene);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _ = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        [Test]
        public void ActiveSceneScope_HandlesActiveScene()
        {
            var oldScene = SceneManager.GetActiveScene();
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(oldScene);

            using (var scope = new ActiveSceneScope(newScene))
            {
                var gameObjectInNewScene = new GameObject();
                Assert.That(gameObjectInNewScene.scene, Is.EqualTo(newScene));
                Assert.That(scope.activeScene, Is.EqualTo(newScene));
                Assert.That(scope.currentScene, Is.EqualTo(oldScene));
            }

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(oldScene));
            var gameObjectInOldScene = new GameObject();
            Assert.That(gameObjectInOldScene.scene, Is.EqualTo(oldScene));
        }
    }
}
