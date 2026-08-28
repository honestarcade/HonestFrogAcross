using System.Collections.Generic;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.View
{
    /// <summary>
    /// Renders the board FROM sim state — the view holds no gameplay truth.
    /// Greybox for M2 (colored quads); M3 swaps quads for sprites without
    /// touching the composition logic. World: 1 unit = 1 cell; row 0 (goal) at
    /// +Z top, bank at z = -(rows-1). X = columns. Camera rig applies the
    /// design's -8° tilt.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public GameSim Sim { get; private set; }

        private readonly List<Transform[]> _objectQuads = new(); // [row][flatIndex]
        private readonly List<int[]> _trainStart = new();        // flat index base per train
        private Transform _player;
        private Transform _playerFacing;

        private static readonly Dictionary<LaneSemantics, Color> LaneColors = new()
        {
            [LaneSemantics.SafeGround] = new Color(0.23f, 0.52f, 0.29f),
            [LaneSemantics.DeadlyTraffic] = new Color(0.22f, 0.24f, 0.27f),
            [LaneSemantics.Water] = new Color(0.10f, 0.38f, 0.60f),
            [LaneSemantics.CrashTraffic] = new Color(0.33f, 0.36f, 0.30f),
            [LaneSemantics.Conveyor] = new Color(0.55f, 0.56f, 0.58f),
            [LaneSemantics.Goal] = new Color(0.12f, 0.32f, 0.19f),
            [LaneSemantics.Bank] = new Color(0.20f, 0.45f, 0.26f),
        };

        public void Bind(GameSim sim)
        {
            Sim = sim;
            BuildStatic();
        }

        private void BuildStatic()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _objectQuads.Clear();
            _trainStart.Clear();

            var level = Sim.Level;
            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];
                var strip = Quad($"row-{r}", new Vector3((level.Columns - 1) / 2f, 0f, -r),
                    new Vector3(level.Columns, 1f, 1f), LaneColors[row.Kind.semantics], -0.02f);
                strip.name = $"row-{r}-{row.Kind.id}";

                foreach (var ob in row.Obstructions)
                    Quad($"ob-{ob.Def.id}", new Vector3(ob.Column, 0f, -r),
                        new Vector3(0.8f, 1f, 0.8f), new Color(0.35f, 0.23f, 0.12f), 0.02f);

                if (row.Kind.semantics == LaneSemantics.Goal)
                    foreach (int b in level.BayColumns)
                        Quad($"bay-{b}", new Vector3(b, 0f, -r),
                            new Vector3(0.9f, 1f, 0.7f), new Color(0.05f, 0.20f, 0.10f), 0.01f);

                var quads = new List<Transform>();
                var starts = new int[row.Trains.Count];
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    starts[t] = quads.Count;
                    var def = row.Trains[t].Def;
                    var color = def.role switch
                    {
                        ObjectRole.Kill => new Color(0.85f, 0.25f, 0.2f),
                        ObjectRole.Crashable => new Color(0.95f, 0.75f, 0.2f),
                        ObjectRole.Rideable => new Color(0.55f, 0.36f, 0.2f),
                        _ => new Color(0.3f, 0.65f, 0.3f),
                    };
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                        quads.Add(Quad($"obj-{def.id}-{k}", Vector3.zero,
                            new Vector3(def.sizeCells, 1f, 0.8f), color, 0.03f));
                }
                _objectQuads.Add(quads.ToArray());
                _trainStart.Add(starts);
            }

            _player = Quad("player", Vector3.zero, new Vector3(0.7f, 1f, 0.7f), new Color(0.0f, 0.84f, 0.71f), 0.05f);
            _playerFacing = Quad("player-facing", Vector3.zero, new Vector3(0.2f, 1f, 0.25f), Color.white, 0.06f);
            _playerFacing.SetParent(_player, false);
            _playerFacing.localPosition = new Vector3(0f, 0.3f, 0.35f);
            _playerFacing.localScale = new Vector3(0.28f, 0.25f, 0.35f);
        }

        /// <summary>Called each rendered frame with the interpolation tick (float).</summary>
        public void Render(float tickF)
        {
            var level = Sim.Level;
            long tick = Sim.State.Tick;
            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    var def = row.Trains[t].Def;
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                    {
                        float left = Sim.ObjectLeftX(r, t, k, tick);
                        var quad = _objectQuads[r][_trainStart[r][t] + k];
                        quad.position = new Vector3(left + def.sizeCells * 0.5f, quad.position.y, -r);
                        bool visible = left + def.sizeCells > -0.6f && left < level.Columns + 0.6f;
                        if (quad.gameObject.activeSelf != visible) quad.gameObject.SetActive(visible);
                    }
                }
            }

            var s = Sim.State;
            _player.SetPositionAndRotation(
                new Vector3(s.PlayerX, _player.position.y, -s.PlayerRow),
                Quaternion.Euler(0f, s.Facing switch
                {
                    Move.Left or Move.DiagForwardLeft or Move.DiagBackLeft => -90f,
                    Move.Right or Move.DiagForwardRight or Move.DiagBackRight => 90f,
                    Move.Back => 180f,
                    _ => 0f,
                }, 0f));
            _player.gameObject.SetActive(s.RespawnDelay == 0);
        }

        private Transform Quad(string name, Vector3 pos, Vector3 scale, Color color, float y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            var tr = go.transform;
            tr.SetParent(transform, false);
            tr.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), Quaternion.Euler(90f, 0f, 0f));
            tr.localScale = scale;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
            go.GetComponent<MeshRenderer>().material = mat;
            return tr;
        }
    }
}
