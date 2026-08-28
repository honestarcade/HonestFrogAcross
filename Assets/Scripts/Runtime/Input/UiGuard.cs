using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FrogAcross.Input
{
    /// <summary>
    /// The behind-buttons rule (#74): gameplay input claims only touches no UI
    /// element consumed. Deterministic raycast (no IsPointerOverGameObject
    /// warnings under the new Input System).
    /// </summary>
    public static class UiGuard
    {
        private static readonly List<RaycastResult> Results = new();

        public static bool IsPointOverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            Results.Clear();
            es.RaycastAll(data, Results);
            return Results.Count > 0;
        }
    }
}
