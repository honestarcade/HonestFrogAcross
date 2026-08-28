using UnityEngine;

namespace FrogAcross.Pieces
{
    public sealed class LaneObjectDef : PieceDef
    {
        public ObjectRole role;
        [Tooltip("Length in cells along the lane.")]
        public float sizeCells = 1f;

        [Header("Rideable")]
        [Tooltip("Rideable span as fractions of the object length (e.g. gator back).")]
        public float rideableZoneStart;
        public float rideableZoneEnd = 1f;
        [Tooltip("Ticks rideable / ticks not (0 = always rideable). Gator: closed/open. Turtle: surfaced/submerged.")]
        public int cycleActiveTicks;
        public int cycleInactiveTicks;
        [Tooltip("When inactive (open mouth / submerged), does the WHOLE object kill on contact (gator) or just stop being rideable (turtle → water rules apply)?")]
        public bool inactiveKills;

        [Header("Crashable")]
        [Tooltip("Player stun duration on collision, in ticks (2s = 120).")]
        public int stunTicks;
        [Tooltip("Crash animation length in ticks before the resting crashed state.")]
        public int crashSequenceTicks;

        [Header("Kill")]
        [Tooltip("Warning lead shown before this object enters the row (trains), in ticks. 0 = none.")]
        public int warnLeadTicks;

        public bool IsRideableAtTick(long tick, int cyclePhaseTicks)
        {
            if (role != ObjectRole.Rideable) return false;
            int active = cycleActiveTicks;
            int inactive = cycleInactiveTicks;
            if (active <= 0 || inactive <= 0) return true; // no cycle: always rideable
            long period = active + inactive;
            long pos = ((tick + cyclePhaseTicks) % period + period) % period;
            return pos < active;
        }
    }
}
