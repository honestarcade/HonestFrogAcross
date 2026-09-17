using FrogAcross.Levels;
using UnityEngine;

namespace FrogAcross.View
{
    /// <summary>
    /// How the board is framed, in one place. Gameplay and the store-screenshot
    /// harness both call this: the harness used to carry its own copy, which
    /// silently stopped matching the game (no roll, its own zoom) and would have
    /// shipped listing screenshots of a board players never see (#67).
    /// </summary>
    public static class BoardCamera
    {
        /// <summary>The design rolls the board; the camera carries the tilt.</summary>
        public const float RollDegrees = -8f;

        public static void Fit(Camera cam, LevelDefinition level, float aspect)
        {
            if (cam == null || level == null) return;
            int rows = level.Rows.Count;
            cam.orthographic = true;
            cam.transform.SetPositionAndRotation(
                new Vector3((level.Columns - 1) / 2f, -(rows - 1) / 2f, -10f),
                Quaternion.Euler(0f, 0f, RollDegrees));

            // Every board corner must land inside the ROLLED frame. Rotating a
            // corner (±cols/2, ±rows/2) by -θ gives the half-extent on each axis.
            float roll = Mathf.Abs(RollDegrees) * Mathf.Deg2Rad;
            float halfHeight = level.Columns / 2f * Mathf.Sin(roll)
                + rows / 2f * Mathf.Cos(roll) + 0.35f;

            // Boards are sized to fill a 21:9 panel, so on a narrower screen the
            // width binds — fit it too, or the edge columns fall off. Zooming out
            // shows more apron, which is what the apron rows are for.
            float halfWidth = level.Columns / 2f * Mathf.Cos(roll)
                + rows / 2f * Mathf.Sin(roll) + 0.35f;

            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, aspect));
        }
    }
}
