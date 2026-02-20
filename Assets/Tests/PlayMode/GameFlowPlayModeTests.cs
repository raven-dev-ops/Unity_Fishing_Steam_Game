using System.Collections;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class GameFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameFlowManager_PauseResume_RestoresPreviousPlayableState()
        {
            var root = new GameObject("GameFlowManagerTests");
            var manager = root.AddComponent<GameFlowManager>();

            yield return null;

            manager.SetState(GameFlowState.Harbor);
            manager.TogglePause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Pause));
            Assert.That(manager.IsPaused, Is.True);

            manager.ResumeFromPause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Harbor));
            Assert.That(manager.IsPaused, Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator GameFlowManager_ReturnToHarborFromPause_TransitionsCorrectly()
        {
            var root = new GameObject("GameFlowPauseToHarborTests");
            var manager = root.AddComponent<GameFlowManager>();

            yield return null;

            manager.SetState(GameFlowState.Fishing);
            manager.TogglePause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Pause));

            manager.ReturnToHarborFromFishingPause();

            Assert.That(manager.CurrentState, Is.EqualTo(GameFlowState.Harbor));
            Assert.That(manager.IsPaused, Is.False);

            Object.Destroy(root);
        }
    }
}
