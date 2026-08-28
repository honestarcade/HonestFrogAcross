using System;

namespace FrogAcross.Levels
{
    // JSON DTOs (JsonUtility shape). docs/level-schema.md is the contract.
    [Serializable] public class LevelDto
    {
        public string id;
        public string name;
        public int columns;
        public MedalDto medal;
        public int startColumn;
        public int[] bays;
        public RowDto[] rows;
    }

    [Serializable] public class MedalDto { public float gold; public float silver; public float bronze; }

    [Serializable] public class RowDto
    {
        public string kind;
        public string dir;          // "left" | "right" | "" (moving lanes only)
        public float speed;         // cells/second (moving lanes)
        public ObjectTrainDto[] objects;
        public ObstructionDto[] obstructions;
    }

    [Serializable] public class ObjectTrainDto
    {
        public string pieceId;
        public float offset;        // cells, position of first instance at tick 0
        public float spacing;       // cells between instance starts; 0 = single instance
        public int phase;           // cycle phase ticks (turtles/gators)
    }

    [Serializable] public class ObstructionDto
    {
        public string pieceId;
        public int column;
    }
}
