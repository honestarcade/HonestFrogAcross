using UnityEngine;

namespace FrogAcross.Pieces
{
    /// <summary>How a character animates between cells. Same speed either way (design rule).</summary>
    public enum MoveStyle { Hop, Step }

    /// <summary>
    /// Engine semantics a lane kind can have. A NEW lane kind (e.g. post-v1
    /// runway) is a new LaneKindDef asset choosing one of these — data, not code.
    /// </summary>
    public enum LaneSemantics
    {
        SafeGround,      // grass, concrete: stand anywhere not obstructed
        DeadlyTraffic,   // road, tracks: ground safe, objects kill
        Water,           // river, swamp: ground kills unless riding an object
        CrashTraffic,    // bike lane: objects crash themselves and stun the player
        Conveyor,        // moving walkway: ground carries the player; board edge kills
        Goal,            // bay row: bays accept, everything else obstructed
        Bank,            // start row: safe
    }

    /// <summary>What a lane object does when the player meets it.</summary>
    public enum ObjectRole
    {
        Kill,        // vehicles, trains
        Rideable,    // logs, rafts, turtle-logs, gators (zone/cycle rules below)
        Crashable,   // riders: they crash, player is stunned
        StaticSafe,  // lily pads
    }

    public abstract class PieceDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite[] sprites; // populated by M3; greybox placeholder until then
    }




}
