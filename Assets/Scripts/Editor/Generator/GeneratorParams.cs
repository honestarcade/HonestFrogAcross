using UnityEngine;

namespace FrogAcross.Editor.Generator
{
    /// <summary>
    /// Parameter envelope for level generation. M5's difficulty curve (#61)
    /// drives sequences of these; the defaults make sane mid-game boards.
    /// </summary>
    public sealed class GeneratorParams : ScriptableObject
    {
        public int baseSeed = 1000;
        public int count = 3;

        public Vector2Int columns = new(9, 13);
        public Vector2Int middleRows = new(6, 9);
        public Vector2Int bayCount = new(2, 4);

        [Tooltip("Bays must land within this many columns of the start (0 = anywhere). "
                 + "Keeps a teaching level a near-straight line on a wide board.")]
        public int bayWindow;

        [Tooltip("Lane kind ids drawn for middle rows (weights by repetition).")]
        public string[] laneKindPool =
        {
            "road", "road", "grass", "river", "concrete", "tracks", "bike", "swamp", "walkway",
        };

        public Vector2 deadlySpeed = new(1.6f, 3.2f);
        public Vector2 waterSpeed = new(0.9f, 1.9f);
        public Vector2 crashSpeed = new(1.8f, 3.0f);
        public Vector2 conveyorSpeed = new(1.0f, 2.0f);
        [Tooltip("Extra spacing beyond size+1.2, in cells.")]
        public Vector2 spacingSlack = new(0.8f, 3.5f);
        [Range(0f, 0.5f)] public float obstructionChance = 0.18f;

        [Tooltip("Reject candidates whose optimal solve runs longer than this many seconds "
                 + "(0 = unlimited). A wide board makes lateral distance expensive — without "
                 + "this, a water level can need three minutes of perfect play.")]
        public float maxSolverSeconds;

        public int solverNodeBudget = 250_000;
        public long solverTickBudget = 10_800;
    }
}
