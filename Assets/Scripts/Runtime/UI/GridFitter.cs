using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// Keeps a GridLayoutGroup at an exact column count by sizing its cells to
    /// the surface it actually got — the panel width varies with the phone's
    /// aspect, so a fixed cell size gave 11 columns on one device and 12 on
    /// another (owner asked for exactly 10).
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class GridFitter : MonoBehaviour
    {
        public int columns = 10;
        public float aspect = 1.25f; // cell height / cell width

        private GridLayoutGroup _grid;
        private RectTransform _rt;
        private float _appliedWidth;

        private void OnEnable()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rt = (RectTransform)transform;
            Apply();
        }

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_grid == null || _rt == null) return;
            float width = _rt.rect.width;
            if (width <= 1f || Mathf.Approximately(width, _appliedWidth)) return;
            _appliedWidth = width;

            float usable = width - _grid.padding.left - _grid.padding.right
                - _grid.spacing.x * (columns - 1);
            float cell = Mathf.Floor(usable / columns);
            if (cell <= 1f) return;
            _grid.cellSize = new Vector2(cell, Mathf.Round(cell * aspect));
        }
    }
}
