using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FrogAcross.UI
{
    /// <summary>
    /// Press-and-hold on a UI element (#123). Hand-rolled rather than
    /// EventTrigger so the cancel paths are explicit and readable.
    ///
    /// Deliberately does NOT implement IDragHandler: the nearest drag handler
    /// above a levels cell is the ScrollRect, and claiming the drag here would
    /// stop the grid scrolling. A finger that slides off cancels via
    /// IPointerExitHandler instead, which is what a scroll does anyway.
    /// </summary>
    public sealed class LongPress : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float holdSeconds = 0.4f;
        public Action OnHold;
        public Action OnRelease;

        /// <summary>True once a hold has fired, until the next press. The click
        /// handler reads this: a hold must not also launch the level.</summary>
        public bool Held { get; private set; }

        private bool _down;
        private float _elapsed;

        private void Update()
        {
            if (!_down || Held) return;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < holdSeconds) return;
            Held = true;
            OnHold?.Invoke();
        }

        public void OnPointerDown(PointerEventData _)
        {
            _down = true;
            _elapsed = 0f;
            Held = false;
        }

        public void OnPointerUp(PointerEventData _) => End();

        public void OnPointerExit(PointerEventData _)
        {
            // a scroll drag leaves the cell long before the hold completes
            if (_down && !Held) { _down = false; _elapsed = 0f; }
            else if (Held) End();
        }

        private void End()
        {
            _down = false;
            _elapsed = 0f;
            if (Held) OnRelease?.Invoke();
            // Held stays true until the next press: OnPointerUp runs BEFORE the
            // click, so the click handler can still see that a hold happened.
        }

        /// <summary>Test seam: drive the hold without a real pointer.</summary>
        public void SimulateHold()
        {
            _down = true;
            _elapsed = holdSeconds;
            Update();
        }
    }
}
