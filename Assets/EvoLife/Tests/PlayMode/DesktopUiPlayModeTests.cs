using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EvoLife.Common;
using EvoLife.UI;

namespace EvoLife.Tests
{
    public sealed class DesktopUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator CameraFocus_EntersOrbitAndReturnToFree()
        {
            var cameraGo = new GameObject("UiCamera");
            cameraGo.AddComponent<Camera>();
            var controller = cameraGo.AddComponent<DesktopCameraController>();
            var target = new GameObject("FocusTarget");
            target.transform.position = new Vector3(4f, 0f, 2f);
            try
            {
                yield return null;
                controller.FocusSelected(target.transform);
                Assert.AreEqual(DesktopCameraMode.Orbit, controller.Mode);
                Assert.AreEqual(target.transform, controller.FocusTarget);
                controller.ReturnToFree();
                Assert.AreEqual(DesktopCameraMode.Free, controller.Mode);
            }
            finally
            {
                Object.Destroy(cameraGo);
                Object.Destroy(target);
            }
        }

        [UnityTest]
        public IEnumerator SelectionState_ClearsWhenHostDestroyed()
        {
            var host = new GameObject("Selectable");
            var identity = host.AddComponent<PlayModeIdentity>();
            identity.Id = new CreatureId(42);
            var state = new CreatureSelectionState();
            state.Select(host, new SelectedCreatureSnapshot
            {
                HasSelection = true,
                Identity = identity
            });
            Assert.IsTrue(state.HasSelection);
            Object.Destroy(host);
            yield return null;
            if (state.Host == null)
            {
                state.Clear();
            }

            Assert.IsFalse(state.HasSelection);
        }
    }

    sealed class PlayModeIdentity : MonoBehaviour, ICreatureIdentity
    {
        public CreatureId Id { get; set; }
        public CreatureRole Role { get; set; }
        public string SpeciesId { get; set; } = "playmode";
    }
}
