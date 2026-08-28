using System.Collections;
using FrogAcross.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FrogAcross.Tests.PlayMode
{
    public class UiGuardTests
    {
        private GameObject _root;

        [TearDown]
        public void Teardown()
        {
            if (_root != null) Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator TouchesOverButtons_BelongToUi_ElsewhereToGameplay()
        {
            _root = new GameObject("ui-root");
            _root.AddComponent<EventSystem>();

            var canvasGo = new GameObject("canvas");
            canvasGo.transform.SetParent(_root.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            // A button covering the screen center — deliberately inside the
            // tap-region middle zone, the exact overlap the rule protects.
            var buttonGo = new GameObject("button");
            buttonGo.transform.SetParent(canvasGo.transform);
            var image = buttonGo.AddComponent<Image>();
            buttonGo.AddComponent<Button>();
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 100);
            rect.anchoredPosition = Vector2.zero;

            yield return null; // let layout settle

            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var corner = new Vector2(Screen.width * 0.05f, Screen.height * 0.05f);

            Assert.IsTrue(UiGuard.IsPointOverUi(center), "center touch must belong to the button");
            Assert.IsFalse(UiGuard.IsPointOverUi(corner), "corner touch must fall through to gameplay");
        }
    }
}
