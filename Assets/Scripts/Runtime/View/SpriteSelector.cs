using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.View
{
    /// <summary>
    /// Pure sprite-selection rules (unit-testable): which sprite index a piece
    /// shows at a given sim state. All state derives from sim tick math — the
    /// view never runs its own clocks (that would desync from determinism).
    ///
    /// Def sprite array conventions (SpriteLibraryImporter):
    /// - characters: [up, down, left, right]
    /// - liveried vehicles: [right×liveries..., left×liveries...]
    /// - trains/logs/gator: [right, left] (+ gator: [open-right, open-left] appended)
    /// - riders: [right×(liv×3frames)..., left×...]
    /// - single-sprite pieces: [0]
    /// </summary>
    public static class SpriteSelector
    {
        public const int RiderFrames = 3;
        public const int RiderFrameTicks = 9; // ~0.15s per frame at 60Hz

        public static int CharacterIndex(Move facing) => facing switch
        {
            Move.Back => 1,
            Move.Left or Move.DiagForwardLeft or Move.DiagBackLeft => 2,
            Move.Right or Move.DiagForwardRight or Move.DiagBackRight => 3,
            _ => 0,
        };

        public static int VehicleIndex(LaneObjectDef def, int dirSign, int liveryIndex, int liveryCount)
        {
            int side = dirSign < 0 ? liveryCount : 0;
            return side + Mathf.Clamp(liveryIndex, 0, liveryCount - 1);
        }

        public static int TrainOrLogIndex(int dirSign) => dirSign < 0 ? 1 : 0;

        public static int GatorIndex(LaneObjectDef def, ObjectTrain train, long tick, int dirSign)
        {
            bool open = !def.IsRideableAtTick(tick, train.PhaseTicks);
            return (open ? 2 : 0) + (dirSign < 0 ? 1 : 0);
        }

        public static int RiderIndex(long tick, int dirSign, int liveryIndex, int liveryCount)
        {
            int frame = (int)(tick / RiderFrameTicks) % RiderFrames;
            int side = dirSign < 0 ? liveryCount * RiderFrames : 0;
            return side + liveryIndex * RiderFrames + frame;
        }

        /// <summary>Turtle-log has one sprite; submerged state renders via alpha.</summary>
        public static float TurtleAlpha(LaneObjectDef def, ObjectTrain train, long tick)
        {
            if (def.cycleActiveTicks <= 0) return 1f;
            bool surfaced = def.IsRideableAtTick(tick, train.PhaseTicks);
            if (surfaced) return 1f;
            return 0.25f; // submerged: sunken ghost under the water
        }

        /// <summary>Deterministic livery assignment per instance (no RNG).</summary>
        public static int LiveryFor(int row, int trainIdx, int instance, int liveryCount)
            => liveryCount <= 0 ? 0 : (row * 7 + trainIdx * 3 + instance) % liveryCount;
    }
}
